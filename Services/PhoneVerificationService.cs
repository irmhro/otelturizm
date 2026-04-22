using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Models.TelefonDogrulama;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class PhoneVerificationService : IPhoneVerificationService
{
    private const string VerificationChannel = "whatsapp";
    private readonly string _connectionString;
    private readonly IWhatsAppCloudApiService _whatsAppCloudApiService;
    private readonly IDataProtector _protector;

    public PhoneVerificationService(
        IConfiguration configuration,
        IWhatsAppCloudApiService whatsAppCloudApiService,
        IDataProtectionProvider dataProtectionProvider)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _whatsAppCloudApiService = whatsAppCloudApiService;
        _protector = dataProtectionProvider.CreateProtector("otelturizm.whatsapp-cloud-api.settings.v1");
    }

    public async Task<UserPhoneVerificationStatusViewModel> GetUserStatusAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var settings = await GetActiveSettingsAsync(connection, cancellationToken);
        var snapshot = await LoadUserPhoneSnapshotAsync(connection, userId, cancellationToken);
        return BuildStatus(snapshot, settings);
    }

    public async Task<PhoneVerificationReservationRequirementResult> GetReservationRequirementAsync(long userId, string returnUrl, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var settings = await GetActiveSettingsAsync(connection, cancellationToken);
        if (settings is null || !settings.IsActive || !settings.ReservationPhoneVerificationRequired)
        {
            return new PhoneVerificationReservationRequirementResult
            {
                IsAllowed = true,
                Message = "Telefon doğrulaması bu akış için zorunlu değil."
            };
        }

        var snapshot = await LoadUserPhoneSnapshotAsync(connection, userId, cancellationToken);
        var status = BuildStatus(snapshot, settings);
        if (status.IsVerified && !status.NeedsReverification)
        {
            return new PhoneVerificationReservationRequirementResult
            {
                IsAllowed = true,
                Message = "Telefon doğrulaması tamam."
            };
        }

        return new PhoneVerificationReservationRequirementResult
        {
            IsAllowed = false,
            Message = status.HasPhoneNumber
                ? "Rezervasyon oluşturmadan önce telefon numaranızı WhatsApp kodu ile doğrulamanız gerekiyor."
                : "Rezervasyon oluşturmadan önce telefon numaranızı ekleyip WhatsApp kodu ile doğrulamanız gerekiyor.",
            RedirectUrl = await ResolvePhoneVerificationRedirectUrlAsync(connection, userId, returnUrl, cancellationToken)
        };
    }

    public async Task<(bool Success, string Message)> SendVerificationCodeAsync(long userId, string? phoneNumber, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var settings = await GetActiveSettingsAsync(connection, cancellationToken);
        if (settings is null || !settings.IsActive)
        {
            return (false, "WhatsApp Cloud API ayarları henüz aktif değil. Lütfen yönetici ile iletişime geçin.");
        }

        if (string.IsNullOrWhiteSpace(settings.PhoneNumberId)
            || string.IsNullOrWhiteSpace(settings.AccessToken)
            || string.IsNullOrWhiteSpace(settings.TemplateName))
        {
            return (false, "WhatsApp Cloud API yapılandırması eksik. Lütfen yönetici ayarlarını tamamlayın.");
        }

        var snapshot = await LoadUserPhoneSnapshotAsync(connection, userId, cancellationToken);
        var rawPhone = string.IsNullOrWhiteSpace(phoneNumber) ? snapshot.PhoneNumber : phoneNumber.Trim();
        var normalizedPhone = NormalizePhoneNumber(rawPhone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return (false, "Geçerli bir telefon numarası giriniz. Örnek: 05xx xxx xx xx");
        }

        if (await HasActiveCooldownAsync(connection, userId, settings.ResendCooldownSeconds, cancellationToken))
        {
            return (false, $"Yeni kod istemek için {settings.ResendCooldownSeconds} saniye bekleyin.");
        }

        var verificationCode = CreateNumericCode(settings.CodeLength);
        var verificationHash = ComputeSha256Hex(verificationCode);
        var tokenId = 0L;
        var messageLogId = 0L;
        var phoneChanged = !string.Equals(snapshot.PhoneNumberE164, normalizedPhone, StringComparison.OrdinalIgnoreCase);

        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            if (phoneChanged && (!string.IsNullOrWhiteSpace(snapshot.PhoneNumber) || !string.IsNullOrWhiteSpace(snapshot.PhoneNumberE164)))
            {
                const string historySql = """
                    INSERT INTO kullanici_telefon_gecmisi
                    (
                        kullanici_id, onceki_telefon_raw, onceki_telefon_e164, yeni_telefon_raw, yeni_telefon_e164,
                        dogrulama_durumu, degisim_nedeni, olusturulma_tarihi
                    )
                    VALUES
                    (
                        @userId, NULLIF(@oldPhoneRaw, ''), NULLIF(@oldPhoneE164, ''), NULLIF(@newPhoneRaw, ''), @newPhoneE164,
                        @verificationStatus, 'Telefon doğrulama akışında güncellendi', SYSUTCDATETIME()
                    );
                    """;
                await using var historyCommand = new SqlCommand(historySql, connection, (SqlTransaction)transaction);
                historyCommand.Parameters.AddWithValue("@userId", userId);
                historyCommand.Parameters.AddWithValue("@oldPhoneRaw", snapshot.PhoneNumber ?? string.Empty);
                historyCommand.Parameters.AddWithValue("@oldPhoneE164", snapshot.PhoneNumberE164 ?? string.Empty);
                historyCommand.Parameters.AddWithValue("@newPhoneRaw", rawPhone);
                historyCommand.Parameters.AddWithValue("@newPhoneE164", normalizedPhone);
                historyCommand.Parameters.AddWithValue("@verificationStatus", snapshot.VerifiedAtUtc.HasValue ? "dogrulanmis" : "dogrulanmamis");
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string updateUserSql = """
                UPDATE users
                SET telefon = @phoneRaw,
                    telefon_e164 = @phoneE164,
                    telefon_dogrulama_kanali = 'whatsapp',
                    telefon_dogrulama_durumu = 'Beklemede',
                    telefon_son_dogrulama_istek_tarihi = SYSUTCDATETIME(),
                    telefon_degistirilme_tarihi = CASE WHEN @phoneChanged = 1 THEN SYSUTCDATETIME() ELSE telefon_degistirilme_tarihi END,
                    telefon_dogrulama_tarihi = CASE WHEN @phoneChanged = 1 THEN NULL ELSE telefon_dogrulama_tarihi END,
                    telefon_son_sahiplik_teyit_tarihi = CASE WHEN @phoneChanged = 1 THEN NULL ELSE telefon_son_sahiplik_teyit_tarihi END,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @userId;
                """;
            await using (var updateUserCommand = new SqlCommand(updateUserSql, connection, (SqlTransaction)transaction))
            {
                updateUserCommand.Parameters.AddWithValue("@userId", userId);
                updateUserCommand.Parameters.AddWithValue("@phoneRaw", rawPhone);
                updateUserCommand.Parameters.AddWithValue("@phoneE164", normalizedPhone);
                updateUserCommand.Parameters.AddWithValue("@phoneChanged", phoneChanged ? 1 : 0);
                await updateUserCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string tokenSql = """
                INSERT INTO telefon_dogrulama_tokenlari
                (
                    kullanici_id, telefon_raw, telefon_e164, dogrulama_kodu_hash, dogrulama_kanali,
                    talep_durumu, deneme_sayisi, maksimum_deneme, gecerlilik_suresi, ip_adresi, user_agent,
                    olusturulma_tarihi, guncellenme_tarihi
                )
                VALUES
                (
                    @userId, @phoneRaw, @phoneE164, @verificationHash, 'whatsapp',
                    'Hazirlaniyor', 0, @maxAttemptCount, DATEADD(SECOND, @ttlSeconds, SYSUTCDATETIME()), @ipAddress, @userAgent,
                    SYSUTCDATETIME(), SYSUTCDATETIME()
                );
                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """;
            await using (var tokenCommand = new SqlCommand(tokenSql, connection, (SqlTransaction)transaction))
            {
                tokenCommand.Parameters.AddWithValue("@userId", userId);
                tokenCommand.Parameters.AddWithValue("@phoneRaw", rawPhone);
                tokenCommand.Parameters.AddWithValue("@phoneE164", normalizedPhone);
                tokenCommand.Parameters.AddWithValue("@verificationHash", verificationHash);
                tokenCommand.Parameters.AddWithValue("@maxAttemptCount", settings.MaxAttemptCount);
                tokenCommand.Parameters.AddWithValue("@ttlSeconds", settings.TtlSeconds);
                tokenCommand.Parameters.AddWithValue("@ipAddress", (object?)ipAddress ?? DBNull.Value);
                tokenCommand.Parameters.AddWithValue("@userAgent", (object?)userAgent ?? DBNull.Value);
                tokenId = Convert.ToInt64(await tokenCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            const string logSql = """
                INSERT INTO whatsapp_mesaj_loglari
                (
                    kullanici_id, telefon_e164, template_name, delivery_status, request_payload, response_payload,
                    olusturulma_tarihi, guncellenme_tarihi
                )
                VALUES
                (
                    @userId, @phoneE164, @templateName, 'Hazirlaniyor', '', '',
                    SYSUTCDATETIME(), SYSUTCDATETIME()
                );
                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """;
            await using (var logCommand = new SqlCommand(logSql, connection, (SqlTransaction)transaction))
            {
                logCommand.Parameters.AddWithValue("@userId", userId);
                logCommand.Parameters.AddWithValue("@phoneE164", normalizedPhone);
                logCommand.Parameters.AddWithValue("@templateName", settings.TemplateName);
                messageLogId = Convert.ToInt64(await logCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        var sendResult = await _whatsAppCloudApiService.SendVerificationTemplateAsync(new WhatsAppCloudSendRequest
        {
            PhoneNumberId = settings.PhoneNumberId,
            AccessToken = settings.AccessToken,
            RecipientPhoneE164 = normalizedPhone,
            TemplateName = settings.TemplateName,
            LanguageCode = settings.LanguageCode,
            VerificationCode = verificationCode
        }, cancellationToken);

        const string updateAfterSendSql = """
            UPDATE telefon_dogrulama_tokenlari
            SET meta_mesaj_id = NULLIF(@messageId, ''),
                talep_durumu = @requestStatus,
                son_hata_mesaji = NULLIF(@errorMessage, ''),
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @tokenId;

            UPDATE whatsapp_mesaj_loglari
            SET meta_mesaj_id = NULLIF(@messageId, ''),
                delivery_status = @requestStatus,
                request_payload = @requestPayload,
                response_payload = @responsePayload,
                error_code = NULLIF(@errorCode, ''),
                error_message = NULLIF(@errorMessage, ''),
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @messageLogId;
            """;
        await using (var updateAfterSendCommand = new SqlCommand(updateAfterSendSql, connection))
        {
            updateAfterSendCommand.Parameters.AddWithValue("@tokenId", tokenId);
            updateAfterSendCommand.Parameters.AddWithValue("@messageLogId", messageLogId);
            updateAfterSendCommand.Parameters.AddWithValue("@messageId", sendResult.MessageId ?? string.Empty);
            updateAfterSendCommand.Parameters.AddWithValue("@requestStatus", sendResult.Success ? "Gonderildi" : "GonderimHatasi");
            updateAfterSendCommand.Parameters.AddWithValue("@requestPayload", sendResult.RequestPayload ?? string.Empty);
            updateAfterSendCommand.Parameters.AddWithValue("@responsePayload", sendResult.ResponsePayload ?? string.Empty);
            updateAfterSendCommand.Parameters.AddWithValue("@errorCode", sendResult.ErrorCode ?? string.Empty);
            updateAfterSendCommand.Parameters.AddWithValue("@errorMessage", sendResult.ErrorMessage ?? string.Empty);
            await updateAfterSendCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return sendResult.Success
            ? (true, "Doğrulama kodu WhatsApp üzerinden gönderildi.")
            : (false, $"WhatsApp mesajı gönderilemedi. {sendResult.ErrorMessage}".Trim());
    }

    public async Task<(bool Success, string Message)> VerifyCodeAsync(long userId, string verificationCode, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(verificationCode))
        {
            return (false, "Doğrulama kodunu giriniz.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT TOP (1)
                id, dogrulama_kodu_hash, deneme_sayisi, maksimum_deneme, gecerlilik_suresi, telefon_e164
            FROM telefon_dogrulama_tokenlari
            WHERE kullanici_id = @userId
              AND kullanildi_mi = 0
            ORDER BY id DESC;
            """;

        long tokenId;
        string storedHash;
        short attemptCount;
        short maxAttemptCount;
        DateTime expiresAtUtc;
        string phoneE164;
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, "Doğrulama için aktif bir kod bulunamadı. Lütfen yeni kod isteyin.");
            }

            tokenId = reader.GetInt64(0);
            storedHash = reader.GetString(1);
            attemptCount = reader.GetInt16(2);
            maxAttemptCount = reader.GetInt16(3);
            expiresAtUtc = reader.GetDateTime(4);
            phoneE164 = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
        }

        if (expiresAtUtc < DateTime.UtcNow)
        {
            await ExpireTokenAsync(connection, tokenId, cancellationToken);
            return (false, "Doğrulama kodunun süresi doldu. Lütfen yeni kod isteyin.");
        }

        if (attemptCount >= maxAttemptCount)
        {
            return (false, "Bu kod için deneme hakkınız doldu. Lütfen yeni kod isteyin.");
        }

        var incomingHash = ComputeSha256Hex(verificationCode.Trim());
        if (!string.Equals(storedHash, incomingHash, StringComparison.OrdinalIgnoreCase))
        {
            const string attemptSql = """
                UPDATE telefon_dogrulama_tokenlari
                SET deneme_sayisi = deneme_sayisi + 1,
                    son_hata_mesaji = 'Kod doğrulaması başarısız',
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @tokenId;
                """;
            await using var attemptCommand = new SqlCommand(attemptSql, connection);
            attemptCommand.Parameters.AddWithValue("@tokenId", tokenId);
            await attemptCommand.ExecuteNonQueryAsync(cancellationToken);
            return (false, "Girdiğiniz kod doğru değil.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        const string verifyTokenSql = """
            UPDATE telefon_dogrulama_tokenlari
            SET kullanildi_mi = 1,
                kullanilma_tarihi = SYSUTCDATETIME(),
                talep_durumu = 'Dogrulandi',
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @tokenId;
            """;
        await using (var verifyTokenCommand = new SqlCommand(verifyTokenSql, connection, (SqlTransaction)transaction))
        {
            verifyTokenCommand.Parameters.AddWithValue("@tokenId", tokenId);
            await verifyTokenCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        const string verifyUserSql = """
            UPDATE users
            SET telefon_e164 = NULLIF(@phoneE164, ''),
                telefon_dogrulama_kanali = 'whatsapp',
                telefon_dogrulama_durumu = 'Dogrulandi',
                telefon_dogrulama_tarihi = SYSUTCDATETIME(),
                telefon_son_sahiplik_teyit_tarihi = SYSUTCDATETIME(),
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @userId;
            """;
        await using (var verifyUserCommand = new SqlCommand(verifyUserSql, connection, (SqlTransaction)transaction))
        {
            verifyUserCommand.Parameters.AddWithValue("@userId", userId);
            verifyUserCommand.Parameters.AddWithValue("@phoneE164", phoneE164);
            await verifyUserCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        const string logSql = """
            INSERT INTO kullanici_aktivite_loglari
            (
                kullanici_id, aktivite_turu, ip_adresi, user_agent, basarili_mi, olusma_tarihi
            )
            VALUES
            (
                @userId, N'Telefon Doğrulama', @ipAddress, @userAgent, 1, SYSUTCDATETIME()
            );
            """;
        await using (var logCommand = new SqlCommand(logSql, connection, (SqlTransaction)transaction))
        {
            logCommand.Parameters.AddWithValue("@userId", userId);
            logCommand.Parameters.AddWithValue("@ipAddress", (object?)ipAddress ?? DBNull.Value);
            logCommand.Parameters.AddWithValue("@userAgent", (object?)userAgent ?? DBNull.Value);
            await logCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, "Telefon numaranız WhatsApp doğrulaması ile onaylandı.");
    }

    public async Task<AdminWhatsAppCloudApiPageViewModel> GetAdminSettingsPageAsync(AdminShellViewModel shell, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var settings = await GetActiveSettingsAsync(connection, cancellationToken);
        var model = new AdminWhatsAppCloudApiPageViewModel
        {
            Shell = shell,
            ConnectionSummary = settings is null
                ? "Henüz kayıtlı WhatsApp Cloud API ayarı yok."
                : $"Aktif ayar yüklü. Phone Number ID: {MaskMiddle(settings.PhoneNumberId)}, Template: {settings.TemplateName}",
            DeliverySummary = await BuildDeliverySummaryAsync(connection, cancellationToken)
        };

        if (settings is not null)
        {
            model.Form = new AdminWhatsAppCloudApiSettingsForm
            {
                AppId = settings.AppId,
                BusinessAccountId = settings.BusinessAccountId,
                PhoneNumberId = settings.PhoneNumberId,
                VerificationTemplateName = settings.TemplateName,
                DefaultLanguageCode = settings.LanguageCode,
                OtpCodeLength = settings.CodeLength,
                OtpTtlSeconds = settings.TtlSeconds,
                ResendCooldownSeconds = settings.ResendCooldownSeconds,
                MaxAttemptCount = settings.MaxAttemptCount,
                PhoneReverifyAfterDays = settings.PhoneReverifyAfterDays,
                ReservationPhoneVerificationRequired = settings.ReservationPhoneVerificationRequired,
                IsActive = settings.IsActive,
                TestRecipientPhoneE164 = settings.TestRecipientPhoneE164
            };
            model.HasSavedToken = !string.IsNullOrWhiteSpace(settings.AccessToken);
            model.HasSavedVerifyToken = !string.IsNullOrWhiteSpace(settings.WebhookVerifyToken);
            model.HasSavedAppSecret = !string.IsNullOrWhiteSpace(settings.AppSecret);
        }

        const string recentLogSql = """
            SELECT TOP (20) telefon_e164, template_name, delivery_status, error_message, olusturulma_tarihi
            FROM whatsapp_mesaj_loglari
            ORDER BY id DESC;
            """;
        await using var command = new SqlCommand(recentLogSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var deliveryStatus = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            model.RecentLogs.Add(new AdminWhatsAppMessageLogRowViewModel
            {
                PhoneNumberE164 = reader.IsDBNull(0) ? "-" : reader.GetString(0),
                TemplateName = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                DeliveryStatus = string.IsNullOrWhiteSpace(deliveryStatus) ? "Bilinmiyor" : deliveryStatus,
                DeliveryToneClass = MapDeliveryTone(deliveryStatus),
                ErrorText = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAtText = reader.IsDBNull(4)
                    ? "-"
                    : reader.GetDateTime(4).ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SaveAdminSettingsAsync(long adminUserId, AdminWhatsAppCloudApiSettingsForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var current = await GetActiveSettingsAsync(connection, cancellationToken);

        var appSecret = !string.IsNullOrWhiteSpace(form.AppSecret)
            ? form.AppSecret.Trim()
            : current?.AppSecret ?? string.Empty;
        var accessToken = !string.IsNullOrWhiteSpace(form.PermanentAccessToken)
            ? form.PermanentAccessToken.Trim()
            : current?.AccessToken ?? string.Empty;
        var verifyToken = !string.IsNullOrWhiteSpace(form.WebhookVerifyToken)
            ? form.WebhookVerifyToken.Trim()
            : current?.WebhookVerifyToken ?? string.Empty;

        if (form.IsActive)
        {
            if (string.IsNullOrWhiteSpace(form.PhoneNumberId)
                || string.IsNullOrWhiteSpace(form.BusinessAccountId)
                || string.IsNullOrWhiteSpace(form.AppId)
                || string.IsNullOrWhiteSpace(accessToken)
                || string.IsNullOrWhiteSpace(verifyToken)
                || string.IsNullOrWhiteSpace(form.VerificationTemplateName))
            {
                return (false, "Aktif yapılandırma için App ID, Business Account ID, Phone Number ID, access token, verify token ve template adı zorunludur.");
            }
        }

        const string sql = """
            IF EXISTS (SELECT 1 FROM whatsapp_cloud_api_ayarlari WHERE id = 1)
            BEGIN
                UPDATE whatsapp_cloud_api_ayarlari
                SET app_id = NULLIF(@appId, ''),
                    app_secret_encrypted = NULLIF(@appSecretEncrypted, ''),
                    business_account_id = NULLIF(@businessAccountId, ''),
                    phone_number_id = NULLIF(@phoneNumberId, ''),
                    permanent_access_token_encrypted = NULLIF(@accessTokenEncrypted, ''),
                    webhook_verify_token_encrypted = NULLIF(@verifyTokenEncrypted, ''),
                    verification_template_name = NULLIF(@templateName, ''),
                    default_language_code = NULLIF(@languageCode, ''),
                    otp_code_length = @codeLength,
                    otp_ttl_seconds = @ttlSeconds,
                    resend_cooldown_seconds = @cooldownSeconds,
                    max_attempt_count = @maxAttemptCount,
                    phone_reverify_after_days = @reverifyAfterDays,
                    reservation_phone_verification_required = @reservationRequired,
                    is_active = @isActive,
                    test_recipient_phone_e164 = NULLIF(@testPhone, ''),
                    updated_by_user_id = @adminUserId,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = 1;
            END
            ELSE
            BEGIN
                INSERT INTO whatsapp_cloud_api_ayarlari
                (
                    id, app_id, app_secret_encrypted, business_account_id, phone_number_id,
                    permanent_access_token_encrypted, webhook_verify_token_encrypted,
                    verification_template_name, default_language_code, otp_code_length, otp_ttl_seconds,
                    resend_cooldown_seconds, max_attempt_count, phone_reverify_after_days,
                    reservation_phone_verification_required, is_active, test_recipient_phone_e164,
                    created_by_user_id, updated_by_user_id, olusturulma_tarihi, guncellenme_tarihi
                )
                VALUES
                (
                    1, NULLIF(@appId, ''), NULLIF(@appSecretEncrypted, ''), NULLIF(@businessAccountId, ''), NULLIF(@phoneNumberId, ''),
                    NULLIF(@accessTokenEncrypted, ''), NULLIF(@verifyTokenEncrypted, ''),
                    NULLIF(@templateName, ''), NULLIF(@languageCode, ''), @codeLength, @ttlSeconds,
                    @cooldownSeconds, @maxAttemptCount, @reverifyAfterDays,
                    @reservationRequired, @isActive, NULLIF(@testPhone, ''),
                    @adminUserId, @adminUserId, SYSUTCDATETIME(), SYSUTCDATETIME()
                );
            END
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@appId", (object?)form.AppId?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("@appSecretEncrypted", Protect(appSecret));
        command.Parameters.AddWithValue("@businessAccountId", (object?)form.BusinessAccountId?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("@phoneNumberId", (object?)form.PhoneNumberId?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("@accessTokenEncrypted", Protect(accessToken));
        command.Parameters.AddWithValue("@verifyTokenEncrypted", Protect(verifyToken));
        command.Parameters.AddWithValue("@templateName", form.VerificationTemplateName?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("@languageCode", form.DefaultLanguageCode?.Trim() ?? "tr");
        command.Parameters.AddWithValue("@codeLength", Math.Clamp(form.OtpCodeLength, (byte)4, (byte)8));
        command.Parameters.AddWithValue("@ttlSeconds", Math.Clamp(form.OtpTtlSeconds, 60, 900));
        command.Parameters.AddWithValue("@cooldownSeconds", Math.Clamp(form.ResendCooldownSeconds, 30, 300));
        command.Parameters.AddWithValue("@maxAttemptCount", Math.Clamp(form.MaxAttemptCount, (byte)3, (byte)10));
        command.Parameters.AddWithValue("@reverifyAfterDays", Math.Clamp(form.PhoneReverifyAfterDays, 30, 730));
        command.Parameters.AddWithValue("@reservationRequired", form.ReservationPhoneVerificationRequired ? 1 : 0);
        command.Parameters.AddWithValue("@isActive", form.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@testPhone", (object?)NormalizePhoneNumber(form.TestRecipientPhoneE164) ?? string.Empty);
        command.Parameters.AddWithValue("@adminUserId", adminUserId);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (true, "WhatsApp Cloud API ayarları kaydedildi.");
    }

    public async Task<(bool Success, string Message)> SendAdminTestMessageAsync(long adminUserId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var settings = await GetActiveSettingsAsync(connection, cancellationToken);
        if (settings is null || !settings.IsActive)
        {
            return (false, "Önce aktif WhatsApp Cloud API ayarlarını kaydetmelisiniz.");
        }

        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return (false, "Test için geçerli bir telefon numarası giriniz.");
        }

        var code = CreateNumericCode(settings.CodeLength);
        var sendResult = await _whatsAppCloudApiService.SendVerificationTemplateAsync(new WhatsAppCloudSendRequest
        {
            PhoneNumberId = settings.PhoneNumberId,
            AccessToken = settings.AccessToken,
            RecipientPhoneE164 = normalizedPhone,
            TemplateName = settings.TemplateName,
            LanguageCode = settings.LanguageCode,
            VerificationCode = code
        }, cancellationToken);

        const string logSql = """
            INSERT INTO whatsapp_mesaj_loglari
            (
                kullanici_id, telefon_e164, template_name, meta_mesaj_id, delivery_status, request_payload,
                response_payload, error_code, error_message, olusturulma_tarihi, guncellenme_tarihi
            )
            VALUES
            (
                @userId, @phoneE164, @templateName, NULLIF(@messageId, ''), @status, @requestPayload,
                @responsePayload, NULLIF(@errorCode, ''), NULLIF(@errorMessage, ''), SYSUTCDATETIME(), SYSUTCDATETIME()
            );
            UPDATE whatsapp_cloud_api_ayarlari
            SET last_test_message_at = SYSUTCDATETIME(),
                test_recipient_phone_e164 = @phoneE164,
                updated_by_user_id = @userId,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = 1;
            """;
        await using var command = new SqlCommand(logSql, connection);
        command.Parameters.AddWithValue("@userId", adminUserId);
        command.Parameters.AddWithValue("@phoneE164", normalizedPhone);
        command.Parameters.AddWithValue("@templateName", settings.TemplateName);
        command.Parameters.AddWithValue("@messageId", sendResult.MessageId ?? string.Empty);
        command.Parameters.AddWithValue("@status", sendResult.Success ? "TestGonderildi" : "TestHatasi");
        command.Parameters.AddWithValue("@requestPayload", sendResult.RequestPayload ?? string.Empty);
        command.Parameters.AddWithValue("@responsePayload", sendResult.ResponsePayload ?? string.Empty);
        command.Parameters.AddWithValue("@errorCode", sendResult.ErrorCode ?? string.Empty);
        command.Parameters.AddWithValue("@errorMessage", sendResult.ErrorMessage ?? string.Empty);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return sendResult.Success
            ? (true, $"Test mesajı {normalizedPhone} numarasına gönderildi.")
            : (false, $"Test mesajı gönderilemedi. {sendResult.ErrorMessage}".Trim());
    }

    public async Task<(bool Success, string Message)> HandleWebhookAsync(string? rawPayload, string? signatureHeader, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return (false, "Webhook payload boş.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        var settings = await GetActiveSettingsAsync(connection, cancellationToken);
        if (settings is null)
        {
            return (false, "Aktif WhatsApp ayarı bulunamadı.");
        }

        if (!IsSignatureValid(rawPayload, signatureHeader, settings.AppSecret))
        {
            return (false, "Webhook imzası doğrulanamadı.");
        }

        using var document = JsonDocument.Parse(rawPayload);
        if (!document.RootElement.TryGetProperty("entry", out var entryArray) || entryArray.ValueKind != JsonValueKind.Array)
        {
            return (true, "Webhook alındı.");
        }

        foreach (var entry in entryArray.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changesArray) || changesArray.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var change in changesArray.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var valueElement))
                {
                    continue;
                }

                if (!valueElement.TryGetProperty("statuses", out var statusesArray) || statusesArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var statusElement in statusesArray.EnumerateArray())
                {
                    var messageId = statusElement.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
                    var deliveryStatus = statusElement.TryGetProperty("status", out var statusValueElement) ? statusValueElement.GetString() ?? string.Empty : string.Empty;
                    var recipientId = statusElement.TryGetProperty("recipient_id", out var recipientElement) ? recipientElement.GetString() ?? string.Empty : string.Empty;
                    var errorCode = string.Empty;
                    var errorMessage = string.Empty;

                    if (statusElement.TryGetProperty("errors", out var errorsElement)
                        && errorsElement.ValueKind == JsonValueKind.Array
                        && errorsElement.GetArrayLength() > 0)
                    {
                        var firstError = errorsElement[0];
                        if (firstError.TryGetProperty("code", out var codeElement))
                        {
                            errorCode = codeElement.ToString();
                        }

                        if (firstError.TryGetProperty("title", out var titleElement))
                        {
                            errorMessage = titleElement.GetString() ?? string.Empty;
                        }
                    }

                    const string updateSql = """
                        UPDATE whatsapp_mesaj_loglari
                        SET delivery_status = NULLIF(@deliveryStatus, ''),
                            error_code = NULLIF(@errorCode, ''),
                            error_message = NULLIF(@errorMessage, ''),
                            guncellenme_tarihi = SYSUTCDATETIME()
                        WHERE meta_mesaj_id = @messageId;

                        UPDATE telefon_dogrulama_tokenlari
                        SET talep_durumu = CASE
                                WHEN @deliveryStatus = 'delivered' THEN 'TeslimEdildi'
                                WHEN @deliveryStatus = 'read' THEN 'Okundu'
                                WHEN @deliveryStatus = 'sent' THEN 'Gonderildi'
                                WHEN @deliveryStatus = 'failed' THEN 'TeslimHatasi'
                                ELSE talep_durumu
                            END,
                            son_hata_mesaji = NULLIF(@errorMessage, ''),
                            guncellenme_tarihi = SYSUTCDATETIME()
                        WHERE meta_mesaj_id = @messageId;
                        """;
                    await using var command = new SqlCommand(updateSql, connection);
                    command.Parameters.AddWithValue("@messageId", messageId);
                    command.Parameters.AddWithValue("@deliveryStatus", deliveryStatus);
                    command.Parameters.AddWithValue("@errorCode", errorCode);
                    command.Parameters.AddWithValue("@errorMessage", string.IsNullOrWhiteSpace(errorMessage) ? recipientId : errorMessage);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        return (true, "Webhook işlendi.");
    }

    public async Task<bool> VerifyWebhookChallengeAsync(string verifyToken, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var settings = await GetActiveSettingsAsync(connection, cancellationToken);
        return settings is not null
            && !string.IsNullOrWhiteSpace(settings.WebhookVerifyToken)
            && string.Equals(settings.WebhookVerifyToken, verifyToken, StringComparison.Ordinal);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static UserPhoneVerificationStatusViewModel BuildStatus(UserPhoneSnapshot snapshot, PhoneVerificationSettingsRecord? settings)
    {
        var reverifyAfterDays = settings?.PhoneReverifyAfterDays ?? 180;
        var lastOwnershipProof = snapshot.LastOwnershipConfirmedAtUtc ?? snapshot.VerifiedAtUtc;
        var needsReverification = snapshot.HasPhoneNumber
            && (
                !snapshot.VerifiedAtUtc.HasValue
                || !lastOwnershipProof.HasValue
                || lastOwnershipProof.Value < DateTime.UtcNow.AddDays(-reverifyAfterDays)
            );

        var status = new UserPhoneVerificationStatusViewModel
        {
            PhoneNumber = snapshot.PhoneNumber ?? string.Empty,
            PhoneNumberE164 = snapshot.PhoneNumberE164 ?? string.Empty,
            VerificationChannel = "WhatsApp",
            HasPhoneNumber = snapshot.HasPhoneNumber,
            IsVerified = snapshot.VerifiedAtUtc.HasValue && !needsReverification,
            NeedsReverification = needsReverification,
            CanSendCode = settings is not null && settings.IsActive && snapshot.HasPhoneNumber,
            VerifiedAtUtc = snapshot.VerifiedAtUtc,
            VerifiedAtText = snapshot.VerifiedAtUtc.HasValue
                ? snapshot.VerifiedAtUtc.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                : "Henüz doğrulanmadı",
            LastSentAtUtc = snapshot.LastSentAtUtc,
            LastSentAtText = snapshot.LastSentAtUtc.HasValue
                ? snapshot.LastSentAtUtc.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                : "Kod gönderilmedi"
        };

        if (!snapshot.HasPhoneNumber)
        {
            status.StatusText = "Telefon eklenmedi";
            status.StatusToneClass = "secondary";
            status.HelpText = "Rezervasyon oluşturabilmek için önce telefon numaranızı ekleyip WhatsApp ile doğrulamanız gerekiyor.";
            return status;
        }

        if (settings is null || !settings.IsActive)
        {
            status.StatusText = "Doğrulama servisi kapalı";
            status.StatusToneClass = "secondary";
            status.HelpText = "WhatsApp doğrulama servisi henüz yönetici tarafından aktif edilmedi.";
            return status;
        }

        if (status.IsVerified)
        {
            status.StatusText = "Telefon doğrulandı";
            status.StatusToneClass = "success";
            status.HelpText = "Rezervasyonlar için telefon doğrulamanız aktif.";
            return status;
        }

        if (needsReverification)
        {
            status.StatusText = "Yeniden doğrulama gerekli";
            status.StatusToneClass = "warning";
            status.HelpText = "Numara sahipliğini güncellemek için WhatsApp kodunu tekrar doğrulayın.";
            return status;
        }

        status.StatusText = "Doğrulama bekleniyor";
        status.StatusToneClass = "info";
        status.HelpText = "Kod gönderip WhatsApp üzerinden gelen doğrulama kodunu girin.";
        return status;
    }

    private async Task<UserPhoneSnapshot> LoadUserPhoneSnapshotAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1)
                COALESCE(telefon, '') AS telefon,
                COALESCE(telefon_e164, '') AS telefon_e164,
                telefon_dogrulama_tarihi,
                telefon_son_dogrulama_istek_tarihi,
                telefon_son_sahiplik_teyit_tarihi,
                COALESCE(telefon_dogrulama_durumu, '') AS telefon_dogrulama_durumu,
                COALESCE(telefon_dogrulama_kanali, '') AS telefon_dogrulama_kanali
            FROM users
            WHERE id = @userId;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new UserPhoneSnapshot();
        }

        var rawPhone = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
        var e164Phone = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        return new UserPhoneSnapshot
        {
            PhoneNumber = rawPhone,
            PhoneNumberE164 = string.IsNullOrWhiteSpace(e164Phone) ? NormalizePhoneNumber(rawPhone) ?? string.Empty : e164Phone,
            VerifiedAtUtc = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
            LastSentAtUtc = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
            LastOwnershipConfirmedAtUtc = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            VerificationStatus = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            VerificationChannel = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
        };
    }

    private async Task<PhoneVerificationSettingsRecord?> GetActiveSettingsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "whatsapp_cloud_api_ayarlari", cancellationToken))
        {
            return null;
        }

        const string sql = """
            SELECT TOP (1)
                id, COALESCE(app_id, ''), COALESCE(app_secret_encrypted, ''), COALESCE(business_account_id, ''),
                COALESCE(phone_number_id, ''), COALESCE(permanent_access_token_encrypted, ''),
                COALESCE(webhook_verify_token_encrypted, ''), COALESCE(verification_template_name, ''),
                COALESCE(default_language_code, 'tr'), otp_code_length, otp_ttl_seconds, resend_cooldown_seconds,
                max_attempt_count, phone_reverify_after_days, reservation_phone_verification_required, is_active,
                COALESCE(test_recipient_phone_e164, '')
            FROM whatsapp_cloud_api_ayarlari
            ORDER BY is_active DESC, id ASC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PhoneVerificationSettingsRecord
        {
            Id = reader.GetInt64(0),
            AppId = reader.GetString(1),
            AppSecret = Unprotect(reader.GetString(2)),
            BusinessAccountId = reader.GetString(3),
            PhoneNumberId = reader.GetString(4),
            AccessToken = Unprotect(reader.GetString(5)),
            WebhookVerifyToken = Unprotect(reader.GetString(6)),
            TemplateName = reader.GetString(7),
            LanguageCode = reader.GetString(8),
            CodeLength = reader.GetByte(9),
            TtlSeconds = Convert.ToInt32(reader.GetValue(10), CultureInfo.InvariantCulture),
            ResendCooldownSeconds = Convert.ToInt32(reader.GetValue(11), CultureInfo.InvariantCulture),
            MaxAttemptCount = reader.GetByte(12),
            PhoneReverifyAfterDays = Convert.ToInt32(reader.GetValue(13), CultureInfo.InvariantCulture),
            ReservationPhoneVerificationRequired = !reader.IsDBNull(14) && Convert.ToInt32(reader.GetValue(14), CultureInfo.InvariantCulture) == 1,
            IsActive = !reader.IsDBNull(15) && Convert.ToInt32(reader.GetValue(15), CultureInfo.InvariantCulture) == 1,
            TestRecipientPhoneE164 = reader.GetString(16)
        };
    }

    private async Task<string> BuildDeliverySummaryAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "whatsapp_mesaj_loglari", cancellationToken))
        {
            return "Mesaj logu tablosu henuz hazir degil.";
        }

        const string sql = """
            SELECT
                COUNT(*) AS total_count,
                SUM(CASE WHEN delivery_status IN ('delivered', 'read', 'TeslimEdildi', 'Okundu') THEN 1 ELSE 0 END) AS delivered_count,
                SUM(CASE WHEN delivery_status IN ('failed', 'GonderimHatasi', 'TeslimHatasi', 'TestHatasi') THEN 1 ELSE 0 END) AS failed_count
            FROM whatsapp_mesaj_loglari
            WHERE olusturulma_tarihi >= DATEADD(DAY, -7, SYSUTCDATETIME());
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return "Mesaj logu bulunmuyor.";
        }

        var totalCount = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
        var deliveredCount = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
        var failedCount = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
        return totalCount <= 0
            ? "Son 7 günde mesaj logu yok."
            : $"Son 7 gün: {totalCount} gönderim, {deliveredCount} başarılı durum, {failedCount} hata kaydı.";
    }

    private async Task<bool> HasActiveCooldownAsync(SqlConnection connection, long userId, int resendCooldownSeconds, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "telefon_dogrulama_tokenlari", cancellationToken))
        {
            return false;
        }

        const string sql = """
            SELECT TOP (1) olusturulma_tarihi
            FROM telefon_dogrulama_tokenlari
            WHERE kullanici_id = @userId
            ORDER BY id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result == DBNull.Value)
        {
            return false;
        }

        var lastCreatedAtUtc = Convert.ToDateTime(result, CultureInfo.InvariantCulture);
        return lastCreatedAtUtc > DateTime.UtcNow.AddSeconds(-resendCooldownSeconds);
    }

    private async Task<string> ResolvePhoneVerificationRedirectUrlAsync(SqlConnection connection, long userId, string returnUrl, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1) COALESCE(rol, ''), firma_id
            FROM users
            WHERE id = @userId;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var isFirmaUser = false;
        if (await reader.ReadAsync(cancellationToken))
        {
            var role = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            isFirmaUser = !reader.IsDBNull(1) || role.StartsWith("firma_", StringComparison.OrdinalIgnoreCase);
        }

        var baseUrl = isFirmaUser ? "/panel/firma/dashboard" : "/panel/user/profil-bilgilerim";
        return $"{baseUrl}?openPhoneVerification=1&returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    private async Task ExpireTokenAsync(SqlConnection connection, long tokenId, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "telefon_dogrulama_tokenlari", cancellationToken))
        {
            return;
        }

        const string sql = """
            UPDATE telefon_dogrulama_tokenlari
            SET talep_durumu = 'SuresiDoldu',
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @tokenId;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tokenId", tokenId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string CreateNumericCode(int codeLength)
    {
        var normalizedLength = Math.Clamp(codeLength, 4, 8);
        var maxExclusive = (int)Math.Pow(10, normalizedLength);
        var minInclusive = (int)Math.Pow(10, normalizedLength - 1);
        return RandomNumberGenerator.GetInt32(minInclusive, maxExclusive).ToString(CultureInfo.InvariantCulture);
    }

    private static string ComputeSha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string Protect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return _protector.Protect(value.Trim());
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

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = @tableName;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null
            && result != DBNull.Value
            && Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static string MaskMiddle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= 6)
        {
            return value ?? string.Empty;
        }

        return $"{value[..3]}***{value[^3..]}";
    }

    private static string MapDeliveryTone(string deliveryStatus)
    {
        return deliveryStatus switch
        {
            "delivered" or "read" or "TeslimEdildi" or "Okundu" or "TestGonderildi" => "success",
            "sent" or "Gonderildi" => "info",
            "failed" or "GonderimHatasi" or "TeslimHatasi" or "TestHatasi" => "danger",
            _ => "secondary"
        };
    }

    private static bool IsSignatureValid(string rawPayload, string? signatureHeader, string? appSecret)
    {
        if (string.IsNullOrWhiteSpace(appSecret))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var incomingSignatureHex = signatureHeader["sha256=".Length..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var computedSignature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload))).ToLowerInvariant();
        var incomingBytes = Encoding.UTF8.GetBytes(incomingSignatureHex.ToLowerInvariant());
        var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
        return incomingBytes.Length == computedBytes.Length
            && CryptographicOperations.FixedTimeEquals(incomingBytes, computedBytes);
    }

    public static string? NormalizePhoneNumber(string? rawPhoneNumber)
    {
        if (string.IsNullOrWhiteSpace(rawPhoneNumber))
        {
            return null;
        }

        var trimmed = rawPhoneNumber.Trim();
        var plusPrefix = trimmed.StartsWith('+');
        var digitsOnly = new string(trimmed.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digitsOnly))
        {
            return null;
        }

        if (plusPrefix)
        {
            return $"+{digitsOnly}";
        }

        if (digitsOnly.StartsWith("00", StringComparison.Ordinal))
        {
            return $"+{digitsOnly[2..]}";
        }

        if (digitsOnly.StartsWith("90", StringComparison.Ordinal) && digitsOnly.Length == 12)
        {
            return $"+{digitsOnly}";
        }

        if (digitsOnly.Length == 11 && digitsOnly.StartsWith("0", StringComparison.Ordinal))
        {
            return $"+90{digitsOnly[1..]}";
        }

        if (digitsOnly.Length == 10)
        {
            return $"+90{digitsOnly}";
        }

        return digitsOnly.Length is >= 11 and <= 15 ? $"+{digitsOnly}" : null;
    }

    private sealed class PhoneVerificationSettingsRecord
    {
        public long Id { get; set; }
        public string AppId { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
        public string BusinessAccountId { get; set; } = string.Empty;
        public string PhoneNumberId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string WebhookVerifyToken { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = "tr";
        public byte CodeLength { get; set; } = 6;
        public int TtlSeconds { get; set; } = 300;
        public int ResendCooldownSeconds { get; set; } = 60;
        public byte MaxAttemptCount { get; set; } = 5;
        public int PhoneReverifyAfterDays { get; set; } = 180;
        public bool ReservationPhoneVerificationRequired { get; set; }
        public bool IsActive { get; set; }
        public string TestRecipientPhoneE164 { get; set; } = string.Empty;
    }

    private sealed class UserPhoneSnapshot
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string PhoneNumberE164 { get; set; } = string.Empty;
        public DateTime? VerifiedAtUtc { get; set; }
        public DateTime? LastSentAtUtc { get; set; }
        public DateTime? LastOwnershipConfirmedAtUtc { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public string VerificationChannel { get; set; } = string.Empty;
        public bool HasPhoneNumber => !string.IsNullOrWhiteSpace(PhoneNumber) || !string.IsNullOrWhiteSpace(PhoneNumberE164);
    }
}
