using System.Globalization;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Email;
using otelturizmnew.Services.Abstractions;
using System.Text.Json;

namespace otelturizmnew.Services;

public class EmailQueueService : IEmailQueueService
{
    private readonly string _connectionString;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<EmailQueueService> _logger;

    public EmailQueueService(IConfiguration configuration, IEmailTemplateService emailTemplateService, ILogger<EmailQueueService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    public async Task QueueTemplateAsync(QueuedEmailTemplateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await QueueTemplateAsync(connection, null, request, cancellationToken);
    }

    public async Task QueueTemplateAsync(DbConnection connection, DbTransaction? transaction, QueuedEmailTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var lang = ResolveLang(request.Tokens);
        var tokensWithUtm = ApplyUtmToTokens(request.Tokens, request.TemplateCode);
        var (templateId, subject, viewPath) = await LoadTemplateAsync(connection, transaction, request.TemplateCode, lang, cancellationToken);
        var preferredSenderEmail = string.IsNullOrWhiteSpace(request.SenderEmailOverride)
            ? ResolvePreferredSenderEmail(request.TemplateCode)
            : request.SenderEmailOverride.Trim();
        var provider = await LoadProviderAsync(connection, transaction, request.ServiceCodeOverride, preferredSenderEmail, cancellationToken);
        var renderedSubject = BuildInboxFriendlySubject(
            request.TemplateCode,
            ReplaceTokens(string.IsNullOrWhiteSpace(request.SubjectOverride) ? subject : request.SubjectOverride!, tokensWithUtm),
            tokensWithUtm,
            request.RelatedRecordId);
        var renderedBody = await _emailTemplateService.RenderTemplateFileAsync(viewPath, tokensWithUtm, cancellationToken);
        var safeUserId = await ResolveSafeUserIdAsync(connection, transaction, request.UserId, request.RecipientEmail, cancellationToken);
        var attachmentsJson = BuildAttachmentsJson(request.Attachments);

        var hasAttachmentsColumn = await ColumnExistsAsync(connection, transaction, "dbo.bildirim_loglari", "ekler_json", cancellationToken);
        var hasServiceCodeColumn = await ColumnExistsAsync(connection, transaction, "dbo.bildirim_loglari", "email_servis_kodu", cancellationToken);
        var hasSenderOverrideColumn = await ColumnExistsAsync(connection, transaction, "dbo.bildirim_loglari", "gonderen_eposta_override", cancellationToken);
        var insertSql = hasAttachmentsColumn
            ? (hasServiceCodeColumn && hasSenderOverrideColumn
                ? """
                INSERT INTO bildirim_loglari
                (kullanici_id, bildirim_sablon_id, tur, alici_eposta, konu, icerik, gonderilen_icerik, durum, saglayici, email_servis_kodu, gonderen_eposta_override, ilgili_tablo, ilgili_kayit_id, ekler_json)
                VALUES
                (@userId, @templateId, 'E-posta', @email, @subject, @body, @body, 'Beklemede', @provider, @serviceCode, @senderEmail, @relatedTable, @relatedId, @attachmentsJson);
                """
                : """
                INSERT INTO bildirim_loglari
                (kullanici_id, bildirim_sablon_id, tur, alici_eposta, konu, icerik, gonderilen_icerik, durum, saglayici, ilgili_tablo, ilgili_kayit_id, ekler_json)
                VALUES
                (@userId, @templateId, 'E-posta', @email, @subject, @body, @body, 'Beklemede', @provider, @relatedTable, @relatedId, @attachmentsJson);
                """)
            : (hasServiceCodeColumn && hasSenderOverrideColumn
                ? """
                INSERT INTO bildirim_loglari
                (kullanici_id, bildirim_sablon_id, tur, alici_eposta, konu, icerik, gonderilen_icerik, durum, saglayici, email_servis_kodu, gonderen_eposta_override, ilgili_tablo, ilgili_kayit_id)
                VALUES
                (@userId, @templateId, 'E-posta', @email, @subject, @body, @body, 'Beklemede', @provider, @serviceCode, @senderEmail, @relatedTable, @relatedId);
                """
                : """
                INSERT INTO bildirim_loglari
                (kullanici_id, bildirim_sablon_id, tur, alici_eposta, konu, icerik, gonderilen_icerik, durum, saglayici, ilgili_tablo, ilgili_kayit_id)
                VALUES
                (@userId, @templateId, 'E-posta', @email, @subject, @body, @body, 'Beklemede', @provider, @relatedTable, @relatedId);
                """);

        await using var command = new SqlCommand(insertSql, (SqlConnection)connection, (SqlTransaction?)transaction);
        command.Parameters.AddWithValue("@userId", safeUserId);
        command.Parameters.AddWithValue("@templateId", templateId);
        command.Parameters.AddWithValue("@email", request.RecipientEmail.Trim());
        command.Parameters.AddWithValue("@subject", renderedSubject);
        command.Parameters.AddWithValue("@body", renderedBody);
        command.Parameters.AddWithValue("@provider", provider.Provider);
        if (hasServiceCodeColumn && hasSenderOverrideColumn)
        {
            command.Parameters.AddWithValue("@serviceCode", string.IsNullOrWhiteSpace(request.ServiceCodeOverride) ? provider.ServiceCode : request.ServiceCodeOverride!.Trim());
            command.Parameters.AddWithValue("@senderEmail", preferredSenderEmail);
        }
        command.Parameters.AddWithValue("@relatedTable", string.IsNullOrWhiteSpace(request.RelatedTable) ? DBNull.Value : request.RelatedTable);
        command.Parameters.AddWithValue("@relatedId", request.RelatedRecordId.HasValue ? request.RelatedRecordId.Value : DBNull.Value);
        if (hasAttachmentsColumn)
        {
            command.Parameters.AddWithValue("@attachmentsJson", (object?)attachmentsJson ?? DBNull.Value);
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(
        DbConnection connection,
        DbTransaction? transaction,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COL_LENGTH(@tableName, @columnName);", (SqlConnection)connection, (SqlTransaction?)transaction);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && scalar != DBNull.Value;
    }

    private static string? BuildAttachmentsJson(IReadOnlyList<QueuedEmailAttachment>? attachments)
    {
        if (attachments is null || attachments.Count == 0)
        {
            return null;
        }

        var normalized = attachments
            .Where(static a => a is not null && !string.IsNullOrWhiteSpace(a.FilePathOrUrl))
            .Select(static a => new
            {
                fileName = string.IsNullOrWhiteSpace(a.FileName) ? "dosya" : a.FileName.Trim(),
                path = a.FilePathOrUrl.Trim(),
                contentType = string.IsNullOrWhiteSpace(a.ContentType) ? "application/octet-stream" : a.ContentType.Trim()
            })
            .ToList();

        if (normalized.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(normalized);
    }

    private static async Task<long> ResolveSafeUserIdAsync(
        DbConnection connection,
        DbTransaction? transaction,
        long requestedUserId,
        string? recipientEmail,
        CancellationToken cancellationToken)
    {
        if (requestedUserId > 0)
        {
            const string userExistsSql = "SELECT COUNT(*) FROM users WHERE id = @userId;";
            await using var userExistsCommand = new SqlCommand(userExistsSql, (SqlConnection)connection, (SqlTransaction?)transaction);
            userExistsCommand.Parameters.AddWithValue("@userId", requestedUserId);
            var exists = Convert.ToInt32(await userExistsCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture) > 0;
            if (exists)
            {
                return requestedUserId;
            }
        }

        if (!string.IsNullOrWhiteSpace(recipientEmail))
        {
            const string byEmailSql = @"
                SELECT TOP (1) id
                FROM users
                WHERE LOWER(eposta) = LOWER(@email)
                ORDER BY id ASC;";
            await using var byEmailCommand = new SqlCommand(byEmailSql, (SqlConnection)connection, (SqlTransaction?)transaction);
            byEmailCommand.Parameters.AddWithValue("@email", recipientEmail.Trim());
            var byEmailScalar = await byEmailCommand.ExecuteScalarAsync(cancellationToken);
            if (byEmailScalar is not null && byEmailScalar != DBNull.Value)
            {
                return Convert.ToInt64(byEmailScalar, CultureInfo.InvariantCulture);
            }
        }

        const string fallbackSql = "SELECT MIN(id) FROM users;";
        await using var fallbackCommand = new SqlCommand(fallbackSql, (SqlConnection)connection, (SqlTransaction?)transaction);
        var fallbackScalar = await fallbackCommand.ExecuteScalarAsync(cancellationToken);
        if (fallbackScalar is not null && fallbackScalar != DBNull.Value)
        {
            return Convert.ToInt64(fallbackScalar, CultureInfo.InvariantCulture);
        }

        throw new InvalidOperationException("E-posta kuyrugu icin gecerli kullanici kaydi bulunamadi.");
    }

    private static string ReplaceTokens(string content, IReadOnlyDictionary<string, string> tokens)
    {
        var rendered = content;
        foreach (var token in tokens)
        {
            var key = token.Key.StartsWith("{{", StringComparison.Ordinal) ? token.Key : $"{{{{{token.Key}}}}}";
            rendered = rendered.Replace(key, token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return rendered;
    }

    private static string BuildInboxFriendlySubject(string templateCode, string subject, IReadOnlyDictionary<string, string> tokens, long? relatedRecordId)
    {
        var title = ResolveSubjectTitle(templateCode, subject);
        var reservationNo = ResolveReservationNumber(tokens);

        var stamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"));
        var parts = new[] { title, reservationNo, stamp }
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Select(static p => p!.Trim());

        return string.Join(" | ", parts);
    }

    private static string ResolveSubjectTitle(string templateCode, string fallback)
    {
        var code = (templateCode ?? string.Empty).Trim().ToLowerInvariant();
        return code switch
        {
            "reservation_received_customer" => "Rezervasyon Talebi Alındı",
            "reservation_confirmed_customer" => "Rezervasyon Onaylandı",
            "reservation_new_partner" => "Yeni Rezervasyon Onayı",
            "reservation_rejected_customer" => "Rezervasyon Reddedildi",
            "reservation_guest_message" => "Misafir Mesajı",
            "reservation_cancelled_partner" => "Rezervasyon İptali",
            "firma_reservation_created_company" => "Kurumsal Rezervasyon Alındı",
            "firma_reservation_created_partner" => "Yeni Kurumsal Rezervasyon",
            _ => NormalizeSubjectTitle(fallback)
        };
    }

    private static string NormalizeSubjectTitle(string fallback)
    {
        var title = (fallback ?? string.Empty).Split('-', StringSplitOptions.TrimEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(title) ? "otelturizm.com Bildirimi" : title.Trim();
    }

    private static string? ResolveReservationNumber(IReadOnlyDictionary<string, string> tokens)
    {
        foreach (var key in new[] { "reservation_no", "rezervasyon_no", "reservation_number", "reservation_code", "rezervasyon_kodu", "code" })
        {
            if (tokens.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static async Task<(long TemplateId, string Subject, string ViewPath)> LoadTemplateAsync(
        DbConnection connection,
        DbTransaction? transaction,
        string templateCode,
        string lang,
        CancellationToken cancellationToken)
    {
        var safeLang = NormalizeLang(lang);
        const string sql = @"
            SELECT TOP (1) id, COALESCE(konu, ''), COALESCE(icerik, '')
            FROM bildirim_sablonlari
            WHERE sablon_kodu = @templateCode AND tur = 'E-posta' AND aktif_mi = 1
            ORDER BY
                CASE WHEN dil = @lang THEN 2
                     WHEN dil = 'tr' THEN 1
                     ELSE 0
                END DESC,
                id ASC;";
        await using var command = new SqlCommand(sql, (SqlConnection)connection, (SqlTransaction?)transaction);
        command.Parameters.AddWithValue("@templateCode", templateCode);
        command.Parameters.AddWithValue("@lang", safeLang);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"E-posta şablonu bulunamadı: {templateCode}");
        }

        return (Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture), reader.GetString(1), reader.GetString(2));
    }

    private static string ResolveLang(IReadOnlyDictionary<string, string> tokens)
    {
        if (tokens is not null)
        {
            if (tokens.TryGetValue("lang", out var lang) && !string.IsNullOrWhiteSpace(lang)) return NormalizeLang(lang);
            if (tokens.TryGetValue("culture", out var culture) && !string.IsNullOrWhiteSpace(culture)) return NormalizeLang(culture);
            if (tokens.TryGetValue("locale", out var locale) && !string.IsNullOrWhiteSpace(locale)) return NormalizeLang(locale);
        }

        return NormalizeLang(CultureInfo.CurrentUICulture?.Name ?? CultureInfo.CurrentCulture?.Name ?? "tr");
    }

    private static string NormalizeLang(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return "tr";
        }

        var first = trimmed.Split('-', '_', ' ').FirstOrDefault() ?? trimmed;
        return first.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "tr";
    }

    private Dictionary<string, string> ApplyUtmToTokens(IReadOnlyDictionary<string, string> tokens, string templateCode)
    {
        // p88: Email linkleri UTM standardı (absolute URL'lere eklenir)
        var updated = new Dictionary<string, string>(tokens ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        var campaign = (templateCode ?? "email").Trim();
        foreach (var key in updated.Keys.ToList())
        {
            if (!key.EndsWith("_link", StringComparison.OrdinalIgnoreCase) &&
                !key.EndsWith("_url", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var current = updated[key];
            if (string.IsNullOrWhiteSpace(current)) continue;
            if (!Uri.TryCreate(current, UriKind.Absolute, out var uri)) continue;

            var ub = new UriBuilder(uri);
            var qs = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(ub.Query ?? string.Empty);
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in qs)
            {
                dict[kv.Key] = kv.Value.Count > 0 ? kv.Value[0] : null;
            }
            dict.TryAdd("utm_source", "transactional");
            dict.TryAdd("utm_medium", "email");
            dict.TryAdd("utm_campaign", campaign);
            ub.Query = string.Join("&", dict.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? string.Empty)}"));
            updated[key] = ub.Uri.ToString();
        }

        return updated;
    }

    private static async Task<EmailProviderSettings> LoadProviderAsync(DbConnection connection, DbTransaction? transaction, string? preferredServiceCode, string preferredSenderEmail, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) servis_kodu, saglayici, gonderen_ad, gonderen_eposta, test_modu
            FROM email_services
            WHERE aktif_mi = 1
            ORDER BY
                CASE WHEN @serviceCode IS NOT NULL AND LOWER(servis_kodu) = LOWER(@serviceCode) THEN 3
                     WHEN LOWER(gonderen_eposta) = LOWER(@senderEmail) THEN 2
                     WHEN varsayilan_mi = 1 THEN 1
                     ELSE 0
                END DESC,
                id ASC;";
        await using var command = new SqlCommand(sql, (SqlConnection)connection, (SqlTransaction?)transaction);
        command.Parameters.AddWithValue("@serviceCode", string.IsNullOrWhiteSpace(preferredServiceCode) ? DBNull.Value : preferredServiceCode.Trim());
        command.Parameters.AddWithValue("@senderEmail", preferredSenderEmail);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new EmailProviderSettings
            {
                SenderEmail = preferredSenderEmail
            };
        }

        return new EmailProviderSettings
        {
            ServiceCode = reader.IsDBNull(0) ? "default_smtp" : reader.GetString(0),
            Provider = reader.GetString(1),
            SenderName = reader.GetString(2),
            SenderEmail = reader.GetString(3),
            TestMode = !reader.IsDBNull(4) && reader.GetBoolean(4)
        };
    }

    private static string ResolvePreferredSenderEmail(string templateCode)
    {
        var code = (templateCode ?? string.Empty).Trim().ToLowerInvariant();
        return code switch
        {
            "login_2fa_email" => "guvenlik@otelturizm.com",
            "email_verify" => "guvenlik@otelturizm.com",
            "password_reset" => "guvenlik@otelturizm.com",
            "reservation_received_customer" => "rezervasyon@otelturizm.com",
            "reservation_confirmed_customer" => "rezervasyon@otelturizm.com",
            "reservation_new_partner" => "rezervasyon@otelturizm.com",
            "reservation_rejected_customer" => "rezervasyon@otelturizm.com",
            "reservation_guest_message" => "rezervasyon@otelturizm.com",
            "reservation_cancelled_partner" => "rezervasyon@otelturizm.com",
            "firma_reservation_created_company" => "rezervasyon@otelturizm.com",
            "firma_reservation_created_partner" => "rezervasyon@otelturizm.com",
            "favorite_price_alert_match" => "bildiri@otelturizm.com",
            "contract_delivery" => "bilgi@otelturizm.com",
            "system_health_link_report" => "bildiri@otelturizm.com",
            _ => "info@otelturizm.com"
        };
    }
}
