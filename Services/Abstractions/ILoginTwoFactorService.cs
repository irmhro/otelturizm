namespace otelturizmnew.Services.Abstractions;

public interface ILoginTwoFactorService
{
    Task<(bool Success, string Message, string Channel)> SendCodeAsync(long userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> VerifyCodeAsync(long userId, string verificationCode, CancellationToken cancellationToken = default);
}

