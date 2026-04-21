namespace otelturizmnew.Models.TelefonDogrulama;

public class UserPhoneVerificationStatusViewModel
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string PhoneNumberE164 { get; set; } = string.Empty;
    public string VerificationChannel { get; set; } = "WhatsApp";
    public string StatusText { get; set; } = "Telefon eklenmedi";
    public string StatusToneClass { get; set; } = "secondary";
    public bool HasPhoneNumber { get; set; }
    public bool IsVerified { get; set; }
    public bool NeedsReverification { get; set; }
    public bool CanSendCode { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string VerifiedAtText { get; set; } = string.Empty;
    public DateTime? LastSentAtUtc { get; set; }
    public string LastSentAtText { get; set; } = string.Empty;
    public string HelpText { get; set; } = string.Empty;
}

public class PhoneVerificationReservationRequirementResult
{
    public bool IsAllowed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
}

public class AdminWhatsAppCloudApiPageViewModel
{
    public otelturizmnew.Models.Paneller.Admin.AdminShellViewModel Shell { get; set; } = new();
    public AdminWhatsAppCloudApiSettingsForm Form { get; set; } = new();
    public bool HasSavedToken { get; set; }
    public bool HasSavedVerifyToken { get; set; }
    public bool HasSavedAppSecret { get; set; }
    public string ConnectionSummary { get; set; } = "Henüz yapılandırılmadı.";
    public string DeliverySummary { get; set; } = "Mesaj logu bulunmuyor.";
    public List<AdminWhatsAppMessageLogRowViewModel> RecentLogs { get; set; } = new();
}

public class AdminWhatsAppCloudApiSettingsForm
{
    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string? BusinessAccountId { get; set; }
    public string? PhoneNumberId { get; set; }
    public string? PermanentAccessToken { get; set; }
    public string? WebhookVerifyToken { get; set; }
    public string VerificationTemplateName { get; set; } = "phone_verification";
    public string DefaultLanguageCode { get; set; } = "tr";
    public byte OtpCodeLength { get; set; } = 6;
    public int OtpTtlSeconds { get; set; } = 300;
    public int ResendCooldownSeconds { get; set; } = 60;
    public byte MaxAttemptCount { get; set; } = 5;
    public int PhoneReverifyAfterDays { get; set; } = 180;
    public bool ReservationPhoneVerificationRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? TestRecipientPhoneE164 { get; set; }
}

public class AdminWhatsAppMessageLogRowViewModel
{
    public string PhoneNumberE164 { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public string DeliveryToneClass { get; set; } = "secondary";
    public string CreatedAtText { get; set; } = string.Empty;
    public string? ErrorText { get; set; }
}

public class WhatsAppCloudSendRequest
{
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RecipientPhoneE164 { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "tr";
    public string VerificationCode { get; set; } = string.Empty;
}

public class WhatsAppCloudSendResult
{
    public bool Success { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string RequestPayload { get; set; } = string.Empty;
    public string ResponsePayload { get; set; } = string.Empty;
}
