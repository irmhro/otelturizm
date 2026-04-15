using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MySqlConnector;

namespace otelturizmnew.Services;

public sealed class EmailDeliveryBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailDeliveryBackgroundService> _logger;

    public EmailDeliveryBackgroundService(IConfiguration configuration, ILogger<EmailDeliveryBackgroundService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta kuyruğu işlenirken hata oluştu.");
            }

            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var smtp = await LoadActiveSmtpAsync(connection, cancellationToken);
        if (smtp is null || string.IsNullOrWhiteSpace(smtp.Host) || smtp.TestMode)
        {
            return;
        }

        var pendingEmails = await LoadPendingEmailsAsync(connection, cancellationToken);
        foreach (var item in pendingEmails)
        {
            try
            {
                await SendEmailAsync(smtp, item, cancellationToken);
                await MarkEmailSentAsync(connection, item.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                await MarkEmailFailedAsync(connection, item, ex.Message, cancellationToken);
            }
        }
    }

    private static async Task<List<QueuedEmailItem>> LoadPendingEmailsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, alici_eposta, konu, gonderilen_icerik, COALESCE(gonderme_denemesi, 1), COALESCE(maksimum_deneme, 3)
            FROM bildirim_loglari
            WHERE tur = 'E-posta'
              AND durum = 'Beklemede'
              AND alici_eposta IS NOT NULL
            ORDER BY olusturulma_tarihi ASC
            LIMIT 10;
            """;

        var items = new List<QueuedEmailItem>();
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new QueuedEmailItem
            {
                Id = reader.GetInt64(0),
                RecipientEmail = reader.GetString(1),
                Subject = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                HtmlBody = reader.GetString(3),
                AttemptCount = reader.IsDBNull(4) ? 1 : reader.GetInt32(4),
                MaxAttempts = reader.IsDBNull(5) ? 3 : reader.GetInt32(5)
            });
        }

        return items;
    }

    private static async Task<SmtpConfig?> LoadActiveSmtpAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT gonderen_ad, gonderen_eposta, yanitla_eposta, smtp_host, smtp_port, smtp_kullanici_adi, smtp_sifre, guvenlik_tipi, baglanti_zaman_asimi_saniye, test_modu
            FROM email_services
            WHERE aktif_mi = 1
            ORDER BY varsayilan_mi DESC, id ASC
            LIMIT 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SmtpConfig
        {
            SenderName = reader.IsDBNull(0) ? "Otelturizm" : reader.GetString(0),
            SenderEmail = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            ReplyToEmail = reader.IsDBNull(2) ? null : reader.GetString(2),
            Host = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Port = reader.IsDBNull(4) ? 465 : reader.GetInt32(4),
            Username = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            Password = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            SecurityType = reader.IsDBNull(7) ? "SSL" : reader.GetString(7),
            TimeoutSeconds = reader.IsDBNull(8) ? 30 : reader.GetInt32(8),
            TestMode = !reader.IsDBNull(9) && reader.GetBoolean(9)
        };
    }

    private async Task SendEmailAsync(SmtpConfig smtp, QueuedEmailItem item, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.SenderName, smtp.SenderEmail));
        message.To.Add(MailboxAddress.Parse(item.RecipientEmail));
        message.Subject = item.Subject;
        if (!string.IsNullOrWhiteSpace(smtp.ReplyToEmail))
        {
            message.ReplyTo.Add(MailboxAddress.Parse(smtp.ReplyToEmail));
        }

        message.Body = new BodyBuilder
        {
            HtmlBody = item.HtmlBody
        }.ToMessageBody();

        cancellationToken.ThrowIfCancellationRequested();
        using var client = new SmtpClient
        {
            Timeout = smtp.TimeoutSeconds * 1000
        };

        var socketOptions = ResolveSocketOptions(smtp);

        _logger.LogInformation(
            "SMTP gonderimi baslatiliyor. Host={Host}, Port={Port}, Guvenlik={Security}, Alici={Recipient}, KuyrukId={QueueId}",
            smtp.Host,
            smtp.Port,
            socketOptions,
            item.RecipientEmail,
            item.Id);

        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static SecureSocketOptions ResolveSocketOptions(SmtpConfig smtp)
    {
        if (smtp.Port == 465)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        return smtp.SecurityType.Trim().ToUpperInvariant() switch
        {
            "SSL" => SecureSocketOptions.SslOnConnect,
            "SSL/TLS" => SecureSocketOptions.SslOnConnect,
            "TLS" => SecureSocketOptions.StartTls,
            "STARTTLS" => SecureSocketOptions.StartTls,
            "AUTO" => SecureSocketOptions.Auto,
            "NONE" => SecureSocketOptions.None,
            _ => SecureSocketOptions.Auto
        };
    }

    private static async Task MarkEmailSentAsync(MySqlConnection connection, long notificationId, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand("""
            UPDATE bildirim_loglari
            SET durum = 'Gönderildi',
                gonderim_tarihi = UTC_TIMESTAMP(),
                hata_kodu = NULL,
                hata_mesaji = NULL
            WHERE id = @id;
            """, connection);
        command.Parameters.AddWithValue("@id", notificationId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkEmailFailedAsync(MySqlConnection connection, QueuedEmailItem item, string errorMessage, CancellationToken cancellationToken)
    {
        var nextAttempt = item.AttemptCount + 1;
        var finalStatus = nextAttempt > item.MaxAttempts ? "Başarısız" : "Beklemede";

        await using var command = new MySqlCommand("""
            UPDATE bildirim_loglari
            SET durum = @status,
                gonderme_denemesi = @attemptCount,
                hata_kodu = 'SMTP',
                hata_mesaji = @error
            WHERE id = @id;
            """, connection);
        command.Parameters.AddWithValue("@status", finalStatus);
        command.Parameters.AddWithValue("@attemptCount", nextAttempt);
        command.Parameters.AddWithValue("@error", errorMessage.Length > 500 ? errorMessage[..500] : errorMessage);
        command.Parameters.AddWithValue("@id", item.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed class QueuedEmailItem
    {
        public long Id { get; init; }
        public string RecipientEmail { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string HtmlBody { get; init; } = string.Empty;
        public int AttemptCount { get; init; }
        public int MaxAttempts { get; init; }
    }

    private sealed class SmtpConfig
    {
        public string SenderName { get; init; } = "Otelturizm";
        public string SenderEmail { get; init; } = string.Empty;
        public string? ReplyToEmail { get; init; }
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; } = 465;
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string SecurityType { get; init; } = "SSL";
        public int TimeoutSeconds { get; init; } = 30;
        public bool TestMode { get; init; }
    }
}
