using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Models.TelefonDogrulama;

namespace otelturizmnew.Services.Abstractions;

public interface IPhoneVerificationService
{
    Task<UserPhoneVerificationStatusViewModel> GetUserStatusAsync(long userId, CancellationToken cancellationToken = default);
    Task<PhoneVerificationReservationRequirementResult> GetReservationRequirementAsync(long userId, string returnUrl, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendVerificationCodeAsync(long userId, string? phoneNumber, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> VerifyCodeAsync(long userId, string verificationCode, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<AdminWhatsAppCloudApiPageViewModel> GetAdminSettingsPageAsync(AdminShellViewModel shell, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveAdminSettingsAsync(long adminUserId, AdminWhatsAppCloudApiSettingsForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendAdminTestMessageAsync(long adminUserId, string phoneNumber, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> HandleWebhookAsync(string? rawPayload, string? signatureHeader, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookChallengeAsync(string verifyToken, CancellationToken cancellationToken = default);
}
