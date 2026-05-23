using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.TelefonDogrulama;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class LoginTwoFactorService : ILoginTwoFactorService
{
    private readonly string _connectionString;
    private readonly IWhatsAppCloudApiService _whatsAppCloudApiService;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IDataProtector _protector;
    private readonly IWebHostEnvironment _environment;
    private readonly bool _devShowTwoFactorCode;

    public LoginTwoFactorService(IConfiguration configuration, IWhatsAppCloudApiService whatsAppCloudApiService, IEmailQueueService emailQueueService, IDataProtectionProvider dataProtectionProvider, IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _whatsAppCloudApiService = whatsAppCloudApiService;
        _emailQueueService = emailQueueService;
        _protector = dataProtectionProvider.CreateProtector("otelturizm.whatsapp-cloud-api.settings.v1");
        _environment = environment;
        _devShowTwoFactorCode = configuration.GetValue("Security:DevShowTwoFactorCode", false);
    }

    public async Task<(bool Success, string Message, string Channel)> SendCodeAsync(long userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return (false, "Doğrulama için geçersiz kullanıcı.", "email");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var delivery = await LoadTwoFactorDeliveryAsync(connection, userId, cancellationToken);
        if (!delivery.Success)
        {
            return (false, delivery.Message, delivery.Channel);
        }

        var settings = delivery.Channel == "whatsapp"
            ? await LoadActiveWhatsAppSettingsAsync(connection, cancellationToken)
            : null;
        if (delivery.Channel == "whatsapp" && settings is null)
        {
            return (false, "WhatsApp doğrulama ayarları aktif değil. Lütfen e-posta kanalını kullanın veya yönetici ile iletişime geçin.", delivery.Channel);
        }

        var resendCooldown = settings?.ResendCooldownSeconds ?? 60;
        if (await HasCooldownAsync(connection, userId, resendCooldown, cancellationToken))
        {
            return (false, $"Yeni kod istemek için {resendCooldown} saniye bekleyin.", delivery.Channel);
        }

        var codeLength = settings?.CodeLength ?? 6;
        var ttlSeconds = settings?.TtlSeconds ?? 300;
        var maxAttempt = settings?.MaxAttemptCount ?? 5;
        var code = CreateNumericCode(codeLength);
        var hash = ComputeSha256Hex(code);

        long tokenId;
        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            await using (var invalidate = new SqlCommand("""
                UPDATE [dbo].[KULLANICI_GIRIS_2FA_TOKENLARI]
                SET [KULLANILDI_MI] = 1,
                    [KULLANILMA_TARIHI] = SYSUTCDATETIME(),
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE [KULLANICI_ID] = @userId
                  AND [KULLANILDI_MI] = 0;
                """, connection, (SqlTransaction)transaction))
            {
                invalidate.Parameters.AddWithValue("@userId", userId);
                await invalidate.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insert = new SqlCommand("""
                INSERT INTO [dbo].[KULLANICI_GIRIS_2FA_TOKENLARI]
                (
                    [KULLANICI_ID], [KANAL], [TELEFON_E164], [EPOSTA], [DOGRULAMA_KODU_HASH],
                    [DENEME_SAYISI], [MAKSIMUM_DENEME], [KULLANILDI_MI],
                    [GECERLILIK_SURESI], [IP_ADRESI], [KULLANICI_ARACISI],
                    [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI]
                )
                VALUES
                (
                    @userId, @channel, @phone, @email, @hash,
                    0, @maxAttempt, 0,
                    DATEADD(SECOND, @ttlSeconds, SYSUTCDATETIME()), NULLIF(@ip, ''), NULLIF(@ua, ''),
                    SYSUTCDATETIME(), SYSUTCDATETIME()
                );
                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """, connection, (SqlTransaction)transaction))
            {
                insert.Parameters.AddWithValue("@userId", userId);
                insert.Parameters.AddWithValue("@channel", delivery.Channel);
                insert.Parameters.AddWithValue("@phone", delivery.PhoneE164 ?? string.Empty);
                insert.Parameters.AddWithValue("@email", (object?)delivery.Email ?? DBNull.Value);
                insert.Parameters.AddWithValue("@hash", hash);
                insert.Parameters.AddWithValue("@maxAttempt", maxAttempt);
                insert.Parameters.AddWithValue("@ttlSeconds", ttlSeconds);
                insert.Parameters.AddWithValue("@ip", ipAddress ?? string.Empty);
                insert.Parameters.AddWithValue("@ua", TrimToMax(userAgent, 500) ?? string.Empty);
                tokenId = Convert.ToInt64(await insert.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        if (delivery.Channel == "email")
        {
            await _emailQueueService.QueueTemplateAsync(new QueuedEmailTemplateRequest
            {
                UserId = userId,
                RecipientEmail = delivery.Email ?? string.Empty,
                TemplateCode = "login_2fa_email",
                RelatedTable = "KULLANICILAR",
                RelatedRecordId = userId,
                Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["user_first_name"] = FirstNameFromFullName(delivery.FullName),
                    ["verification_code"] = code,
                    ["verification_channel"] = "e-posta",
                    ["login_time"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                }
            }, cancellationToken);
            _ = tokenId;
            if (_environment.IsDevelopment() && _devShowTwoFactorCode)
            {
                return (true, $"DEV: Güvenlik kodu: {code}", delivery.Channel);
            }
            return (true, "Güvenlik kodu e-posta adresinize gönderildi.", delivery.Channel);
        }

        var sendResult = await _whatsAppCloudApiService.SendVerificationTemplateAsync(new WhatsAppCloudSendRequest
        {
            PhoneNumberId = settings!.PhoneNumberId,
            AccessToken = settings.AccessToken,
            RecipientPhoneE164 = delivery.PhoneE164 ?? string.Empty,
            TemplateName = settings.TemplateName,
            LanguageCode = settings.LanguageCode,
            VerificationCode = code
        }, cancellationToken);

        if (!sendResult.Success)
        {
            return (false, $"WhatsApp kodu gönderilemedi. {sendResult.ErrorMessage}".Trim(), delivery.Channel);
        }

        _ = tokenId;
        return (true, "Güvenlik kodu WhatsApp üzerinden gönderildi.", delivery.Channel);
    }

    public async Task<(bool Success, string Message)> VerifyCodeAsync(long userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        var code = (verificationCode ?? string.Empty).Trim();
        if (userId <= 0 || string.IsNullOrWhiteSpace(code))
        {
            return (false, "Doğrulama kodu zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sql = """
            SELECT TOP (1)
                id, [DOGRULAMA_KODU_HASH], [DENEME_SAYISI], [MAKSIMUM_DENEME], [KULLANILDI_MI], [GECERLILIK_SURESI]
            FROM [dbo].[KULLANICI_GIRIS_2FA_TOKENLARI]
            WHERE [KULLANICI_ID] = @userId
            ORDER BY [OLUSTURULMA_TARIHI] DESC;
            """;

        long tokenId;
        string storedHash;
        int attempts;
        int maxAttempts;
        bool used;
        DateTime expiryUtc;

        await using (var cmd = new SqlCommand(sql, connection, (SqlTransaction)transaction))
        {
            cmd.Parameters.AddWithValue("@userId", userId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, "Doğrulama kodu bulunamadı. Lütfen yeniden kod isteyin.");
            }

            tokenId = reader.GetInt64(0);
            storedHash = reader.GetString(1);
            attempts = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
            maxAttempts = reader.IsDBNull(3) ? 5 : Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture);
            used = !reader.IsDBNull(4) && reader.GetBoolean(4);
            expiryUtc = reader.GetDateTime(5);
        }

        if (used)
        {
            return (false, "Bu doğrulama kodu daha önce kullanılmış.");
        }

        if (expiryUtc <= DateTime.UtcNow)
        {
            return (false, "Doğrulama kodunun süresi dolmuş. Lütfen yeniden kod isteyin.");
        }

        if (attempts >= maxAttempts)
        {
            return (false, "Bu doğrulama kodu çok fazla denendiği için geçersiz hale geldi.");
        }

        var candidateHash = ComputeSha256Hex(code);
        if (!string.Equals(candidateHash, storedHash, StringComparison.OrdinalIgnoreCase))
        {
            await using var inc = new SqlCommand("""
                UPDATE [dbo].[KULLANICI_GIRIS_2FA_TOKENLARI]
                SET [DENEME_SAYISI] = COALESCE([DENEME_SAYISI], 0) + 1,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id = @tokenId;
                """, connection, (SqlTransaction)transaction);
            inc.Parameters.AddWithValue("@tokenId", tokenId);
            await inc.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (false, "Doğrulama kodu hatalı.");
        }

        await using (var mark = new SqlCommand("""
            UPDATE [dbo].[KULLANICI_GIRIS_2FA_TOKENLARI]
            SET [KULLANILDI_MI] = 1,
                [KULLANILMA_TARIHI] = SYSUTCDATETIME(),
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @tokenId;
            """, connection, (SqlTransaction)transaction))
        {
            mark.Parameters.AddWithValue("@tokenId", tokenId);
            await mark.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, "Doğrulama başarılı.");
    }

    private static string CreateNumericCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var min = (int)Math.Pow(10, length - 1);
        return RandomNumberGenerator.GetInt32(min, max).ToString(CultureInfo.InvariantCulture);
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? TrimToMax(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static async Task<bool> HasCooldownAsync(SqlConnection connection, long userId, int cooldownSeconds, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("""
            SELECT TOP (1) [OLUSTURULMA_TARIHI]
            FROM [dbo].[KULLANICI_GIRIS_2FA_TOKENLARI]
            WHERE [KULLANICI_ID] = @userId
            ORDER BY [OLUSTURULMA_TARIHI] DESC;
            """, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        if (scalar is DateTime lastCreated)
        {
            return lastCreated.ToUniversalTime() > DateTime.UtcNow.AddSeconds(-cooldownSeconds);
        }
        return false;
    }

    private async Task<(bool Success, string Message, string Channel, string? Email, string? PhoneE164, string FullName)> LoadTwoFactorDeliveryAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            SELECT TOP (1)
                COALESCE([AD_SOYAD], ''),
                COALESCE([EPOSTA], ''),
                [EPOSTA_DOGRULAMA_TARIHI],
                COALESCE([TELEFON_E164], ''),
                [TELEFON_DOGRULAMA_TARIHI],
                COALESCE([IKI_ASAMALI_DOGRULAMA_KANALI], 'email')
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (false, "Kullanıcı bulunamadı.", "email", null, null, string.Empty);
        }

        var fullName = reader.GetString(0);
        var email = reader.GetString(1);
        var emailVerified = !reader.IsDBNull(2);
        var phone = reader.GetString(3);
        var phoneVerified = !reader.IsDBNull(4);
        var channel = NormalizeChannel(reader.GetString(5));

        if (channel == "whatsapp")
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return (false, "WhatsApp ile 2FA için hesabınıza telefon eklemelisiniz.", channel, email, null, fullName);
            }

            if (!phoneVerified)
            {
                return (false, "WhatsApp ile 2FA için telefon numaranızın doğrulanmış olması gerekir.", channel, email, phone, fullName);
            }

            return (true, "Telefon doğrulandı.", channel, email, phone, fullName);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "E-posta ile 2FA için hesabınıza geçerli bir e-posta tanımlı olmalıdır.", "email", null, phone, fullName);
        }

        if (!emailVerified)
        {
            return (false, "E-posta ile 2FA için e-posta adresinizin onaylanmış olması gerekir.", "email", email, phone, fullName);
        }

        return (true, "E-posta doğrulandı.", "email", email, phone, fullName);
    }

    private static string NormalizeChannel(string? channel)
    {
        var normalized = (channel ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "telefon" => "whatsapp",
            "phone" => "whatsapp",
            "whatsapp" => "whatsapp",
            _ => "email"
        };
    }

    private static string FirstNameFromFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Misafir";
        }

        var first = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? "Misafir" : first;
    }

    private sealed record WhatsAppSettings(
        string PhoneNumberId,
        string AccessToken,
        string TemplateName,
        string LanguageCode,
        int CodeLength,
        int TtlSeconds,
        int ResendCooldownSeconds,
        int MaxAttemptCount);

    private async Task<WhatsAppSettings?> LoadActiveWhatsAppSettingsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("""
            SELECT TOP (1)
                COALESCE([TELEFON_NUMARASI_ID], ''),
                COALESCE([PERMANENT_ACCESS_TOKEN_ENCRYPTED], ''),
                COALESCE([DOGRULAMA_SABLON_ADI], ''),
                COALESCE([VARSAYILAN_DIL_KODU], 'tr'),
                COALESCE([OTP_KOD_LENGTH], 6),
                COALESCE([OTP_TTL_SECONDS], 300),
                COALESCE([RESEND_COOLDOWN_SECONDS], 60),
                COALESCE([MAX_ATTEMPT_COUNT], 5),
                COALESCE([AKTIF_MI], 0)
            FROM [dbo].[WHATSAPP_CLOUD_API_AYARLARI]
            ORDER BY id DESC;
            """, connection);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var phoneNumberId = reader.GetString(0);
        var accessTokenEncrypted = reader.GetString(1);
        var template = reader.GetString(2);
        var lang = reader.GetString(3);
        var codeLen = Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture);
        var ttl = Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture);
        var cooldown = Convert.ToInt32(reader.GetValue(6), CultureInfo.InvariantCulture);
        var max = Convert.ToInt32(reader.GetValue(7), CultureInfo.InvariantCulture);
        var active = Convert.ToInt32(reader.GetValue(8), CultureInfo.InvariantCulture) == 1;

        var accessToken = Unprotect(accessTokenEncrypted);

        if (!active || string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        return new WhatsAppSettings(phoneNumberId, accessToken, template, lang, Math.Clamp(codeLen, 4, 8), Math.Clamp(ttl, 60, 1800), Math.Clamp(cooldown, 10, 600), Math.Clamp(max, 3, 10));
    }

    private string Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            return _protector.Unprotect(value);
        }
        catch
        {
            return string.Empty;
        }
    }
}

