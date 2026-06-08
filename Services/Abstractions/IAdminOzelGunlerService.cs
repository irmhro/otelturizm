using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminOzelGunlerService
{
    Task<AdminOzelGunlerPageViewModel> GetPageAsync(string fullName, string email, string userRole, int? editId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveAsync(AdminOzelGunForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
