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

    public EmailQueueService(IConfiguration configuration, IEmailTemplateService emailTemplateService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _emailTemplateService = emailTemplateService;
    }

    public async Task QueueTemplateAsync(QueuedEmailTemplateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await QueueTemplateAsync(connection, null, request, cancellationToken);
    }

    public async Task QueueTemplateAsync(DbConnection connection, DbTransaction? transaction, QueuedEmailTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var (templateId, subject, viewPath) = await LoadTemplateAsync(connection, transaction, request.TemplateCode, cancellationToken);
        var provider = await LoadProviderAsync(connection, transaction, cancellationToken);
        var renderedSubject = ReplaceTokens(string.IsNullOrWhiteSpace(request.SubjectOverride) ? subject : request.SubjectOverride!, request.Tokens);
        var renderedBody = await _emailTemplateService.RenderTemplateFileAsync(viewPath, request.Tokens, cancellationToken);
        var safeUserId = await ResolveSafeUserIdAsync(connection, transaction, request.UserId, request.RecipientEmail, cancellationToken);
        var attachmentsJson = BuildAttachmentsJson(request.Attachments);

        const string insertSql = @"
            INSERT INTO bildirim_loglari
            (kullanici_id, bildirim_sablon_id, tur, alici_eposta, konu, icerik, gonderilen_icerik, durum, saglayici, ilgili_tablo, ilgili_kayit_id, ekler_json)
            VALUES
            (@userId, @templateId, 'E-posta', @email, @subject, @body, @body, 'Beklemede', @provider, @relatedTable, @relatedId, @attachmentsJson);";

        await using var command = new SqlCommand(insertSql, (SqlConnection)connection, (SqlTransaction?)transaction);
        command.Parameters.AddWithValue("@userId", safeUserId);
        command.Parameters.AddWithValue("@templateId", templateId);
        command.Parameters.AddWithValue("@email", request.RecipientEmail.Trim());
        command.Parameters.AddWithValue("@subject", renderedSubject);
        command.Parameters.AddWithValue("@body", renderedBody);
        command.Parameters.AddWithValue("@provider", provider.Provider);
        command.Parameters.AddWithValue("@relatedTable", string.IsNullOrWhiteSpace(request.RelatedTable) ? DBNull.Value : request.RelatedTable);
        command.Parameters.AddWithValue("@relatedId", request.RelatedRecordId.HasValue ? request.RelatedRecordId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@attachmentsJson", (object?)attachmentsJson ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private static async Task<(long TemplateId, string Subject, string ViewPath)> LoadTemplateAsync(DbConnection connection, DbTransaction? transaction, string templateCode, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) id, COALESCE(konu, ''), COALESCE(icerik, '')
            FROM bildirim_sablonlari
            WHERE sablon_kodu = @templateCode AND tur = 'E-posta' AND aktif_mi = 1
            ORDER BY CASE WHEN dil = 'tr' THEN 1 ELSE 0 END DESC, id ASC;";
        await using var command = new SqlCommand(sql, (SqlConnection)connection, (SqlTransaction?)transaction);
        command.Parameters.AddWithValue("@templateCode", templateCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"E-posta şablonu bulunamadı: {templateCode}");
        }

        return (Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture), reader.GetString(1), reader.GetString(2));
    }

    private static async Task<EmailProviderSettings> LoadProviderAsync(DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) saglayici, gonderen_ad, gonderen_eposta, test_modu
            FROM email_services
            WHERE aktif_mi = 1
            ORDER BY varsayilan_mi DESC, id ASC;";
        await using var command = new SqlCommand(sql, (SqlConnection)connection, (SqlTransaction?)transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new EmailProviderSettings();
        }

        return new EmailProviderSettings
        {
            Provider = reader.GetString(0),
            SenderName = reader.GetString(1),
            SenderEmail = reader.GetString(2),
            TestMode = !reader.IsDBNull(3) && reader.GetBoolean(3)
        };
    }
}
