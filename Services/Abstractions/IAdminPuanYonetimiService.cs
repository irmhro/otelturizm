using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminPuanYonetimiService
{
    Task<AdminPuanYonetimiPageViewModel> GetPageAsync(
        string fullName,
        string email,
        string userRole,
        string? tab = null,
        long? editRuleId = null,
        long? filterUserId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> SaveRuleAsync(AdminPuanAyarForm form, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> DeleteRuleAsync(long id, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> AdjustUserPointsAsync(AdminPuanKullaniciAdjustForm form, CancellationToken cancellationToken = default);
}
