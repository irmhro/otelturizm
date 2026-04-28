using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;
using System.Security.Authentication;
using System.Text.Json;
using System.Net.Http;

namespace otelturizmnew.Services;

public sealed class EmailDeliveryBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailDeliveryBackgroundService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly bool _disableCertificateRevocationCheck;
    private readonly bool _smtpAllowInvalidCertificate;

    public EmailDeliveryBackgroundService(IConfiguration configuration, ILogger<EmailDeliveryBackgroundService> logger, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
        _disableCertificateRevocationCheck = _configuration.GetValue("Email:DisableCertificateRevocationCheck", true);
        _smtpAllowInvalidCertificate = _configuration.GetValue("Email:SmtpAllowInvalidCertificate", false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta kuyruğu işlenirken hata oluştu.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var defaultSmtp = await LoadSmtpConfigAsync(connection, null, null, cancellationToken);
        if (defaultSmtp is null)
        {
            // Dev ortamında DB'de email_services kaydı yoksa bile kuyruk akışını test edebilmek için
            // pickup directory moduna düşebiliriz.
            defaultSmtp = TryBuildDevPickupSmtp();
            if (defaultSmtp is null)
            {
                return;
            }
        }
        else if (_environment.IsDevelopment())
        {
            // Development'ta SMTP çalışmıyorsa (host/şifre/port vb.), yine de kuyruk akışı
            // test edilebilsin diye pickup fallback ekliyoruz.
            var devPickup = ResolveDevPickupDirectory();
            if (!string.IsNullOrWhiteSpace(devPickup) && OperatingSystem.IsWindows())
            {
                defaultSmtp = new SmtpConfig
                {
                    ServiceCode = defaultSmtp.ServiceCode,
                    SenderName = defaultSmtp.SenderName,
                    SenderEmail = defaultSmtp.SenderEmail,
                    ReplyToEmail = defaultSmtp.ReplyToEmail,
                    Host = defaultSmtp.Host,
                    Port = defaultSmtp.Port,
                    Username = defaultSmtp.Username,
                    Password = defaultSmtp.Password,
                    SecurityType = defaultSmtp.SecurityType,
                    TimeoutSeconds = defaultSmtp.TimeoutSeconds,
                    TestMode = defaultSmtp.TestMode,
                    PickupDirectory = devPickup,
                    UsePickupDirectoryOnly = defaultSmtp.UsePickupDirectoryOnly,
                    CanUsePickupDirectoryFallback = true
                };
            }
        }

        if (defaultSmtp.TestMode)
        {
            // Test modu: Development'ta e-posta teslimatını güvenli şekilde pickup directory'e yönlendirir.
            // Production ortamında "test_modu" yanlışlıkla açık bırakılırsa, 2FA gibi kritik e-postalar kuyrukta takılmasın diye
            // gönderimi tamamen durdurmuyoruz; sadece uyarı log'u basıp normal SMTP gönderimine devam ediyoruz.
            if (_environment.IsDevelopment())
            {
                var devPickup = ResolveDevPickupDirectory();
                if (!string.IsNullOrWhiteSpace(devPickup))
                {
                    defaultSmtp = new SmtpConfig
                    {
                        ServiceCode = defaultSmtp.ServiceCode,
                        SenderName = defaultSmtp.SenderName,
                        SenderEmail = defaultSmtp.SenderEmail,
                        ReplyToEmail = defaultSmtp.ReplyToEmail,
                        Host = defaultSmtp.Host,
                        Port = defaultSmtp.Port,
                        Username = defaultSmtp.Username,
                        Password = defaultSmtp.Password,
                        SecurityType = defaultSmtp.SecurityType,
                        TimeoutSeconds = defaultSmtp.TimeoutSeconds,
                        TestMode = true,
                        PickupDirectory = devPickup,
                        UsePickupDirectoryOnly = OperatingSystem.IsWindows(),
                        CanUsePickupDirectoryFallback = false
                    };
                    _logger.LogWarning("Aktif SMTP servisi test modunda. E-postalar pickup directory'e kaydedilecek. Path={Path}", devPickup);
                }
                else
                {
                    _logger.LogWarning("Aktif SMTP servisi test modunda ancak pickup directory tanimli degil. Kuyruk islenmedi. Host={Host}, Sender={Sender}", defaultSmtp.Host, defaultSmtp.SenderEmail);
                    return;
                }
            }
            else
            {
                _logger.LogWarning("Aktif SMTP servisi test modunda (PROD). Kuyruk durdurulmayacak; SMTP uzerinden gonderim denenecek. Host={Host}, Sender={Sender}", defaultSmtp.Host, defaultSmtp.SenderEmail);
                defaultSmtp = new SmtpConfig
                {
                    ServiceCode = defaultSmtp.ServiceCode,
                    SenderName = defaultSmtp.SenderName,
                    SenderEmail = defaultSmtp.SenderEmail,
                    ReplyToEmail = defaultSmtp.ReplyToEmail,
                    Host = defaultSmtp.Host,
                    Port = defaultSmtp.Port,
                    Username = defaultSmtp.Username,
                    Password = defaultSmtp.Password,
                    SecurityType = defaultSmtp.SecurityType,
                    TimeoutSeconds = defaultSmtp.TimeoutSeconds,
                    TestMode = false,
                    PickupDirectory = defaultSmtp.PickupDirectory,
                    UsePickupDirectoryOnly = defaultSmtp.UsePickupDirectoryOnly,
                    CanUsePickupDirectoryFallback = defaultSmtp.CanUsePickupDirectoryFallback
                };
            }
        }

        var hasUpdatedAtColumn = await ColumnExistsAsync(connection, "dbo.bildirim_loglari", "guncellenme_tarihi", cancellationToken);
        var hasNextAttemptColumn = await ColumnExistsAsync(connection, "dbo.bildirim_loglari", "sonraki_deneme_utc", cancellationToken);
        var pendingEmails = await ClaimPendingEmailsAsync(connection, hasUpdatedAtColumn, hasNextAttemptColumn, cancellationToken);
        foreach (var item in pendingEmails)
        {
            try
            {
                var smtp = await LoadSmtpConfigAsync(connection, item.ServiceCode, item.SenderEmailOverride, cancellationToken)
                    ?? defaultSmtp;
                var sendResult = await SendEmailAsync(smtp, item, cancellationToken);
                await MarkEmailAcceptedAsync(connection, item.Id, sendResult, cancellationToken);
                await MarkSmtpSuccessAsync(connection, smtp.ServiceCode, cancellationToken);
            }
            catch (Exception ex)
            {
                await MarkEmailFailedAsync(connection, item, ex.Message, hasNextAttemptColumn, cancellationToken);
                await MarkSmtpFailureAsync(connection, item.ServiceCode ?? defaultSmtp.ServiceCode, ex.Message, cancellationToken);
                _logger.LogError(ex, "E-posta gonderimi basarisiz. Alici={Recipient}, Konu={Subject}, KuyrukId={QueueId}", item.RecipientEmail, item.Subject, item.Id);
            }
        }
    }

    private string? ResolveDevPickupDirectory()
    {
        var configured = _configuration.GetValue<string>("Email:DevPickupDirectory");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(configured);
            return configured;
        }
        catch
        {
            return null;
        }
    }

    private SmtpConfig? TryBuildDevPickupSmtp()
    {
        var pickup = ResolveDevPickupDirectory();
        if (string.IsNullOrWhiteSpace(pickup) || !OperatingSystem.IsWindows())
        {
            return null;
        }

        var senderName = _configuration.GetValue<string>("Email:DefaultSenderName") ?? "Otelturizm";
        var senderEmail = _configuration.GetValue<string>("Email:DefaultSenderEmail") ?? "no-reply@localhost";
        var replyTo = _configuration.GetValue<string>("Email:DefaultReplyToEmail");

        return new SmtpConfig
        {
            ServiceCode = "development_pickup",
            SenderName = senderName,
            SenderEmail = senderEmail,
            ReplyToEmail = replyTo,
            Host = string.Empty,
            Port = 0,
            Username = string.Empty,
            Password = string.Empty,
            SecurityType = "PICKUP",
            TimeoutSeconds = 60,
            TestMode = true,
            PickupDirectory = pickup,
            UsePickupDirectoryOnly = true,
            CanUsePickupDirectoryFallback = false
        };
    }

    private static async Task<List<QueuedEmailItem>> ClaimPendingEmailsAsync(SqlConnection connection, bool hasUpdatedAtColumn, bool hasNextAttemptColumn, CancellationToken cancellationToken)
    {
        var hasAttachmentsColumn = await ColumnExistsAsync(connection, "dbo.bildirim_loglari", "ekler_json", cancellationToken);
        var hasServiceCodeColumn = await ColumnExistsAsync(connection, "dbo.bildirim_loglari", "email_servis_kodu", cancellationToken);
        var hasSenderOverrideColumn = await ColumnExistsAsync(connection, "dbo.bildirim_loglari", "gonderen_eposta_override", cancellationToken);
        var updateSet = hasUpdatedAtColumn
            ? "durum = 'İşleniyor', guncellenme_tarihi = SYSUTCDATETIME()"
            : "durum = 'İşleniyor'";
        if (hasNextAttemptColumn)
        {
            updateSet += ", sonraki_deneme_utc = NULL";
        }

        var nextAttemptWhere = hasNextAttemptColumn
            ? "AND (sonraki_deneme_utc IS NULL OR sonraki_deneme_utc <= SYSUTCDATETIME())"
            : string.Empty;

        var staleProcessingWhere = hasUpdatedAtColumn
            ? """
                (
                    durum = 'Beklemede'
                    OR (
                        durum = 'İşleniyor'
                        AND COALESCE(guncellenme_tarihi, olusturulma_tarihi) <= DATEADD(MINUTE, -3, SYSUTCDATETIME())
                    )
                )
              """
            : """
                (
                    durum = 'Beklemede'
                    OR (
                        durum = 'İşleniyor'
                        AND olusturulma_tarihi <= DATEADD(MINUTE, -3, SYSUTCDATETIME())
                    )
                )
              """;

        string sql;
        if (hasAttachmentsColumn && hasServiceCodeColumn && hasSenderOverrideColumn)
        {
            sql = """
                ;WITH cte AS (
                    SELECT TOP (10) id
                    FROM bildirim_loglari WITH (READPAST, ROWLOCK, UPDLOCK)
                    WHERE tur = 'E-posta'
                      AND __STALE_PROCESSING_WHERE__
                      AND alici_eposta IS NOT NULL
                      __NEXT_ATTEMPT_WHERE__
                    ORDER BY olusturulma_tarihi ASC
                )
                UPDATE b
                SET __UPDATE_SET__
                OUTPUT
                    inserted.id,
                    inserted.alici_eposta,
                    inserted.konu,
                    inserted.gonderilen_icerik,
                    COALESCE(inserted.gonderme_denemesi, 1),
                    COALESCE(inserted.maksimum_deneme, 3),
                    inserted.ekler_json,
                    inserted.email_servis_kodu,
                    inserted.gonderen_eposta_override
                FROM bildirim_loglari b
                INNER JOIN cte ON cte.id = b.id;
                """
            ;
        }
        else if (hasAttachmentsColumn)
        {
            sql = """
                ;WITH cte AS (
                    SELECT TOP (10) id
                    FROM bildirim_loglari WITH (READPAST, ROWLOCK, UPDLOCK)
                    WHERE tur = 'E-posta'
                      AND __STALE_PROCESSING_WHERE__
                      AND alici_eposta IS NOT NULL
                      __NEXT_ATTEMPT_WHERE__
                    ORDER BY olusturulma_tarihi ASC
                )
                UPDATE b
                SET __UPDATE_SET__
                OUTPUT
                    inserted.id,
                    inserted.alici_eposta,
                    inserted.konu,
                    inserted.gonderilen_icerik,
                    COALESCE(inserted.gonderme_denemesi, 1),
                    COALESCE(inserted.maksimum_deneme, 3),
                    inserted.ekler_json,
                    NULL AS email_servis_kodu,
                    NULL AS gonderen_eposta_override
                FROM bildirim_loglari b
                INNER JOIN cte ON cte.id = b.id;
                """;
        }
        else if (hasServiceCodeColumn && hasSenderOverrideColumn)
        {
            sql = """
                ;WITH cte AS (
                    SELECT TOP (10) id
                    FROM bildirim_loglari WITH (READPAST, ROWLOCK, UPDLOCK)
                    WHERE tur = 'E-posta'
                      AND __STALE_PROCESSING_WHERE__
                      AND alici_eposta IS NOT NULL
                      __NEXT_ATTEMPT_WHERE__
                    ORDER BY olusturulma_tarihi ASC
                )
                UPDATE b
                SET __UPDATE_SET__
                OUTPUT
                    inserted.id,
                    inserted.alici_eposta,
                    inserted.konu,
                    inserted.gonderilen_icerik,
                    COALESCE(inserted.gonderme_denemesi, 1),
                    COALESCE(inserted.maksimum_deneme, 3),
                    NULL AS ekler_json,
                    inserted.email_servis_kodu,
                    inserted.gonderen_eposta_override
                FROM bildirim_loglari b
                INNER JOIN cte ON cte.id = b.id;
                """;
        }
        else
        {
            sql = """
                ;WITH cte AS (
                    SELECT TOP (10) id
                    FROM bildirim_loglari WITH (READPAST, ROWLOCK, UPDLOCK)
                    WHERE tur = 'E-posta'
                      AND __STALE_PROCESSING_WHERE__
                      AND alici_eposta IS NOT NULL
                      __NEXT_ATTEMPT_WHERE__
                    ORDER BY olusturulma_tarihi ASC
                )
                UPDATE b
                SET __UPDATE_SET__
                OUTPUT
                    inserted.id,
                    inserted.alici_eposta,
                    inserted.konu,
                    inserted.gonderilen_icerik,
                    COALESCE(inserted.gonderme_denemesi, 1),
                    COALESCE(inserted.maksimum_deneme, 3),
                    NULL AS ekler_json,
                    NULL AS email_servis_kodu,
                    NULL AS gonderen_eposta_override
                FROM bildirim_loglari b
                INNER JOIN cte ON cte.id = b.id;
                """;
        }
        sql = sql.Replace("__UPDATE_SET__", updateSet, StringComparison.Ordinal);
        sql = sql.Replace("__NEXT_ATTEMPT_WHERE__", nextAttemptWhere, StringComparison.Ordinal);
        sql = sql.Replace("__STALE_PROCESSING_WHERE__", staleProcessingWhere, StringComparison.Ordinal);

        var items = new List<QueuedEmailItem>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new QueuedEmailItem
            {
                Id = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                RecipientEmail = reader.GetString(1),
                Subject = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                HtmlBody = reader.GetString(3),
                AttemptCount = reader.IsDBNull(4) ? 1 : Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                MaxAttempts = reader.IsDBNull(5) ? 3 : Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
                AttachmentsJson = reader.IsDBNull(6) ? null : reader.GetString(6),
                ServiceCode = reader.IsDBNull(7) ? null : reader.GetString(7),
                SenderEmailOverride = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return items;
    }

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COL_LENGTH(@tableName, @columnName);", connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && scalar != DBNull.Value;
    }

    private static async Task<SmtpConfig?> LoadSmtpConfigAsync(SqlConnection connection, string? preferredServiceCode, string? preferredSenderEmail, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1) servis_kodu, gonderen_ad, gonderen_eposta, yanitla_eposta, smtp_host, smtp_port, smtp_kullanici_adi, smtp_sifre, guvenlik_tipi, baglanti_zaman_asimi_saniye, test_modu, metadata
            FROM email_services
            WHERE aktif_mi = 1
            ORDER BY
                CASE
                    WHEN @serviceCode IS NOT NULL AND LOWER(servis_kodu) = LOWER(@serviceCode) THEN 3
                    WHEN @senderEmail IS NOT NULL AND LOWER(gonderen_eposta) = LOWER(@senderEmail) THEN 2
                    WHEN varsayilan_mi = 1 THEN 1
                    ELSE 0
                END DESC,
                id ASC;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@serviceCode", string.IsNullOrWhiteSpace(preferredServiceCode) ? DBNull.Value : preferredServiceCode.Trim());
        command.Parameters.AddWithValue("@senderEmail", string.IsNullOrWhiteSpace(preferredSenderEmail) ? DBNull.Value : preferredSenderEmail.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var metadataJson = reader.FieldCount > 11 && !reader.IsDBNull(11)
            ? reader.GetString(11)
            : null;
        var pickupDirectory = ReadMetadataValue(metadataJson, "pickup_directory");
        var transportMode = ReadMetadataValue(metadataJson, "transport_mode");
        var canUsePickupFallback = OperatingSystem.IsWindows()
            && !string.IsNullOrWhiteSpace(pickupDirectory);

        return new SmtpConfig
        {
            ServiceCode = reader.IsDBNull(0) ? "default_smtp" : reader.GetString(0),
            SenderName = reader.IsDBNull(1) ? "Otelturizm" : reader.GetString(1),
            SenderEmail = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            ReplyToEmail = reader.IsDBNull(3) ? null : reader.GetString(3),
            Host = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            Port = reader.IsDBNull(5) ? 465 : Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
            Username = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Password = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            SecurityType = reader.IsDBNull(8) ? "SSL" : reader.GetString(8),
            TimeoutSeconds = Math.Max(30, reader.IsDBNull(9) ? 60 : Convert.ToInt32(reader.GetValue(9), CultureInfo.InvariantCulture)),
            TestMode = !reader.IsDBNull(10) && reader.GetBoolean(10),
            PickupDirectory = pickupDirectory,
            UsePickupDirectoryOnly = string.Equals(transportMode, "pickup", StringComparison.OrdinalIgnoreCase) && canUsePickupFallback,
            CanUsePickupDirectoryFallback = !string.Equals(transportMode, "pickup", StringComparison.OrdinalIgnoreCase) && canUsePickupFallback
        };
    }

    private static string? ReadMetadataValue(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (document.RootElement.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private async Task<EmailSendResult> SendEmailAsync(SmtpConfig smtp, QueuedEmailItem item, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.SenderName, smtp.SenderEmail));
        message.To.Add(MailboxAddress.Parse(item.RecipientEmail));
        message.Subject = item.Subject;
        message.MessageId = MimeUtils.GenerateMessageId("otelturizm.com");
        if (!string.IsNullOrWhiteSpace(smtp.ReplyToEmail))
        {
            message.ReplyTo.Add(MailboxAddress.Parse(smtp.ReplyToEmail));
        }

        var builder = new BodyBuilder
        {
            HtmlBody = item.HtmlBody
        };

        await AttachFilesAsync(builder, item.AttachmentsJson, cancellationToken);
        message.Body = builder.ToMessageBody();

        if (smtp.UsePickupDirectoryOnly)
        {
            var pickupPath = await SaveToPickupDirectoryAsync(smtp, message, cancellationToken);
            return new EmailSendResult
            {
                Status = "Dosyaya Yazıldı",
                ProviderMessageId = pickupPath
            };
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var client = new SmtpClient
        {
            Timeout = smtp.TimeoutSeconds * 1000
        };
        client.CheckCertificateRevocation = !_disableCertificateRevocationCheck;
        client.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        if (_smtpAllowInvalidCertificate)
        {
            client.ServerCertificateValidationCallback = static (_, _, _, _) => true;
        }

        var socketCandidates = BuildSocketOptions(smtp);
        Exception? lastError = null;
        for (var i = 0; i < socketCandidates.Count; i++)
        {
            var socketOptions = socketCandidates[i];
            try
            {
                _logger.LogInformation(
                    "SMTP gonderimi baslatiliyor. Host={Host}, Port={Port}, Guvenlik={Security}, Alici={Recipient}, KuyrukId={QueueId}, Deneme={TryIndex}",
                    smtp.Host,
                    smtp.Port,
                    socketOptions,
                    item.RecipientEmail,
                    item.Id,
                    i + 1);

                await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(smtp.Username))
                {
                    await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
                }

                var providerResponse = await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
                return new EmailSendResult
                {
                    Status = "SMTP Kabul",
                    ProviderMessageId = NormalizeProviderMessageId(providerResponse, message.MessageId)
                };
            }
            catch (Exception ex)
            {
                lastError = ex;
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true, cancellationToken);
                }
            }
        }

        if (smtp.CanUsePickupDirectoryFallback)
        {
            var pickupPath = await SaveToPickupDirectoryAsync(smtp, message, cancellationToken);
            return new EmailSendResult
            {
                Status = "Dosyaya Yazıldı",
                ProviderMessageId = pickupPath
            };
        }

        throw lastError ?? new InvalidOperationException("SMTP gonderimi basarisiz oldu.");
    }

    private static async Task AttachFilesAsync(BodyBuilder builder, string? attachmentsJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(attachmentsJson))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(attachmentsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var fileName = item.TryGetProperty("fileName", out var fileNameProp) && fileNameProp.ValueKind == JsonValueKind.String
                    ? fileNameProp.GetString()
                    : null;
                var path = item.TryGetProperty("path", out var pathProp) && pathProp.ValueKind == JsonValueKind.String
                    ? pathProp.GetString()
                    : null;
                var contentType = item.TryGetProperty("contentType", out var ctProp) && ctProp.ValueKind == JsonValueKind.String
                    ? ctProp.GetString()
                    : "application/octet-stream";

                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    using var http = new HttpClient();
                    var bytes = await http.GetByteArrayAsync(uri, cancellationToken);
                    builder.Attachments.Add(string.IsNullOrWhiteSpace(fileName) ? "dosya" : fileName, bytes, ContentType.Parse(contentType ?? "application/octet-stream"));
                    continue;
                }

                var physicalPath = path;
                if (!Path.IsPathRooted(physicalPath))
                {
                    physicalPath = Path.Combine(Directory.GetCurrentDirectory(), path.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar));
                }

                if (File.Exists(physicalPath))
                {
                    builder.Attachments.Add(string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(physicalPath) : fileName, await File.ReadAllBytesAsync(physicalPath, cancellationToken), ContentType.Parse(contentType ?? "application/octet-stream"));
                }
            }
        }
        catch
        {
            // Ekler parse edilemezse mail yine de gitsin.
        }
    }

    private static async Task<string> SaveToPickupDirectoryAsync(SmtpConfig smtp, MimeMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(smtp.PickupDirectory))
        {
            throw new InvalidOperationException("Pickup directory tanimli degil.");
        }

        Directory.CreateDirectory(smtp.PickupDirectory);
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.eml";
        var path = Path.Combine(smtp.PickupDirectory, fileName);
        await using var stream = File.Create(path);
        await message.WriteToAsync(stream, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        return path;
    }

    private static async Task MarkSmtpSuccessAsync(SqlConnection connection, string serviceCode, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            UPDATE email_services
            SET son_basarili_test_tarihi = SYSUTCDATETIME(),
                son_hata_tarihi = NULL,
                son_hata_mesaji = NULL
            WHERE aktif_mi = 1
              AND servis_kodu = @serviceCode;
            """, connection);
        command.Parameters.AddWithValue("@serviceCode", serviceCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkSmtpFailureAsync(SqlConnection connection, string serviceCode, string errorMessage, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            UPDATE email_services
            SET son_hata_tarihi = SYSUTCDATETIME(),
                son_hata_mesaji = @error
            WHERE aktif_mi = 1
              AND servis_kodu = @serviceCode;
            """, connection);
        command.Parameters.AddWithValue("@serviceCode", serviceCode);
        command.Parameters.AddWithValue("@error", errorMessage.Length > 1000 ? errorMessage[..1000] : errorMessage);
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private static IReadOnlyList<SecureSocketOptions> BuildSocketOptions(SmtpConfig smtp)
    {
        var primary = ResolveSocketOptions(smtp);
        var options = new List<SecureSocketOptions>
        {
            primary,
            SecureSocketOptions.Auto,
            SecureSocketOptions.SslOnConnect,
            SecureSocketOptions.StartTls
        };

        return options.Distinct().ToList();
    }

    private static async Task MarkEmailAcceptedAsync(SqlConnection connection, long notificationId, EmailSendResult sendResult, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            UPDATE bildirim_loglari
            SET durum = @status,
                gonderim_tarihi = SYSUTCDATETIME(),
                saglayici_mesaj_id = @providerMessageId,
                hata_kodu = NULL,
                hata_mesaji = NULL
            WHERE id = @id;
            """, connection);
        command.Parameters.AddWithValue("@status", sendResult.Status);
        command.Parameters.AddWithValue("@providerMessageId", (object?)sendResult.ProviderMessageId ?? DBNull.Value);
        command.Parameters.AddWithValue("@id", notificationId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkEmailFailedAsync(SqlConnection connection, QueuedEmailItem item, string errorMessage, bool hasNextAttemptColumn, CancellationToken cancellationToken)
    {
        var nextAttempt = item.AttemptCount + 1;
        var finalStatus = nextAttempt > item.MaxAttempts ? "Başarısız" : "Beklemede";
        var delaySeconds = ComputeBackoffSeconds(nextAttempt);

        var sql = hasNextAttemptColumn && finalStatus == "Beklemede"
            ? """
            UPDATE bildirim_loglari
            SET durum = @status,
                gonderme_denemesi = @attemptCount,
                hata_kodu = 'SMTP',
                hata_mesaji = @error,
                sonraki_deneme_utc = DATEADD(SECOND, @delaySec, SYSUTCDATETIME())
            WHERE id = @id;
            """
            : """
            UPDATE bildirim_loglari
            SET durum = @status,
                gonderme_denemesi = @attemptCount,
                hata_kodu = 'SMTP',
                hata_mesaji = @error
            WHERE id = @id;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@status", finalStatus);
        command.Parameters.AddWithValue("@attemptCount", nextAttempt);
        command.Parameters.AddWithValue("@error", errorMessage.Length > 500 ? errorMessage[..500] : errorMessage);
        if (hasNextAttemptColumn && finalStatus == "Beklemede")
        {
            command.Parameters.AddWithValue("@delaySec", delaySeconds);
        }
        command.Parameters.AddWithValue("@id", item.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static int ComputeBackoffSeconds(int attempt)
    {
        var a = Math.Max(1, attempt);
        return a switch
        {
            1 => 10,
            2 => 30,
            3 => 90,
            4 => 180,
            _ => 300
        };
    }

    private static string? NormalizeProviderMessageId(string? providerResponse, string? messageId)
    {
        var value = string.IsNullOrWhiteSpace(providerResponse) ? messageId : providerResponse;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 250 ? trimmed : trimmed[..250];
    }

    private sealed class QueuedEmailItem
    {
        public long Id { get; init; }
        public string RecipientEmail { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string HtmlBody { get; init; } = string.Empty;
        public int AttemptCount { get; init; }
        public int MaxAttempts { get; init; }
        public string? AttachmentsJson { get; init; }
        public string? ServiceCode { get; init; }
        public string? SenderEmailOverride { get; init; }
    }

    private sealed class EmailSendResult
    {
        public string Status { get; init; } = "SMTP Kabul";
        public string? ProviderMessageId { get; init; }
    }

    private sealed class SmtpConfig
    {
        public string ServiceCode { get; init; } = "default_smtp";
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
        public string? PickupDirectory { get; init; }
        public bool UsePickupDirectoryOnly { get; init; }
        public bool CanUsePickupDirectoryFallback { get; init; }
    }
}
