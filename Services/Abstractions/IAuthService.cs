using otelturizmnew.Models.Giris;
using otelturizmnew.Models.Register;

namespace otelturizmnew.Services.Abstractions;

public interface IAuthService
{
    Task<UserSessionModel?> AuthenticateUserAsync(string identity, string password, CancellationToken cancellationToken = default);
    Task<UserSessionModel?> AuthenticatePartnerAsync(string identity, string password, CancellationToken cancellationToken = default);
    Task<UserSessionModel?> AuthenticateFirmaAsync(string identity, string password, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, UserSessionModel? User)> RegisterUserAsync(UserRegistrationModel model, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, UserSessionModel? User)> RegisterPartnerAsync(PartnerRegistrationModel model, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, UserSessionModel? User)> RegisterFirmaAsync(FirmaRegistrationModel model, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> VerifyEmailAsync(string email, string code, string? token, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ResendVerificationEmailAsync(string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendPasswordResetAsync(string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default);
    Task<string> ResolveLoginPathByEmailAsync(string? email, CancellationToken cancellationToken = default);
    Task<string> ResolveLoginPathByResetTokenAsync(string? token, CancellationToken cancellationToken = default);
}

