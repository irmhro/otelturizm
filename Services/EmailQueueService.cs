using System.Globalization;
using MySqlConnector;
using otelturizmnew.Models.Email;
using otelturizmnew.Services.Abstractions;

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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await QueueTemplateAsync(connection, null, request, cancellationToken);
    }

    public async Task QueueTemplateAsync(MySqlConnection connection, MySqlTransaction? transaction, QueuedEmailTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var (templateId, subject, viewPath) = await LoadTemplateAsync(connection, transaction, request.TemplateCode, cancellationToken);
        var provider = await LoadProviderAsync(connection, transaction, cancellationToken);
        var renderedSubject = ReplaceTokens(string.IsNullOrWhiteSpace(request.SubjectOverride) ? subject : request.SubjectOverride!, request.Tokens);
        var renderedBody = await _emailTemplateService.RenderTemplateFileAsync(viewPath, request.Tokens, cancellationToken);

        const string insertSql = @"
            INSERT INTO bildirim_loglari
            (kullanici_id, bildirim_sablon_id, tur, alici_eposta, konu, icerik, gonderilen_icerik, durum, saglayici, ilgili_tablo, ilgili_kayit_id)
            VALUES
            (@userId, @templateId, 'E-posta', @email, @subject, @body, @body, 'Beklemede', @provider, @relatedTable, @relatedId);";

        await using var command = new MySqlCommand(insertSql, connection, transaction);
        command.Parameters.AddWithValue("@userId", request.UserId);
        command.Parameters.AddWithValue("@templateId", templateId);
        command.Parameters.AddWithValue("@email", request.RecipientEmail.Trim());
        command.Parameters.AddWithValue("@subject", renderedSubject);
        command.Parameters.AddWithValue("@body", renderedBody);
        command.Parameters.AddWithValue("@provider", provider.Provider);
        command.Parameters.AddWithValue("@relatedTable", string.IsNullOrWhiteSpace(request.RelatedTable) ? DBNull.Value : request.RelatedTable);
        command.Parameters.AddWithValue("@relatedId", request.RelatedRecordId.HasValue ? request.RelatedRecordId.Value : DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private static async Task<(long TemplateId, string Subject, string ViewPath)> LoadTemplateAsync(MySqlConnection connection, MySqlTransaction? transaction, string templateCode, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, COALESCE(konu, ''), COALESCE(icerik, '')
            FROM bildirim_sablonlari
            WHERE sablon_kodu = @templateCode AND tur = 'E-posta' AND aktif_mi = 1
            ORDER BY dil = 'tr' DESC, id ASC
            LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@templateCode", templateCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"E-posta şablonu bulunamadı: {templateCode}");
        }

        return (reader.GetInt64(0), reader.GetString(1), reader.GetString(2));
    }

    private static async Task<EmailProviderSettings> LoadProviderAsync(MySqlConnection connection, MySqlTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT saglayici, gonderen_ad, gonderen_eposta, test_modu
            FROM email_services
            WHERE aktif_mi = 1
            ORDER BY varsayilan_mi DESC, id ASC
            LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection, transaction);
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
