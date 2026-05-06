using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminEmailRoutingService
{
    Task<AdminEmailRoutingPageViewModel> GetPageAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveAsync(long adminUserId, AdminEmailRoutingSaveForm form, CancellationToken cancellationToken = default);

    /// <summary>Partner web kaydı tamamlandığında adminlere bildirim kuyruğa alınır.</summary>
    Task NotifyPartnerRegistrationAsync(
        long applicantUserId,
        long partnerId,
        long hotelId,
        string hotelName,
        string companyName,
        string contactName,
        string applicantEmail,
        string phone,
        string city,
        string district,
        CancellationToken cancellationToken = default);

    /// <summary>Firma web başvurusu tamamlandığında adminlere bildirim kuyruğa alınır.</summary>
    Task NotifyFirmaRegistrationAsync(
        long applicantUserId,
        long firmaId,
        string companyName,
        string contactName,
        string applicantEmail,
        string phone,
        string city,
        CancellationToken cancellationToken = default);
}
