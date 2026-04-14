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
}

