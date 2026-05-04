using otelturizmnew.Models.Paneller.Departman;

namespace otelturizmnew.Services.Abstractions;

public interface IDepartmentPanelService
{
    Task<DepartmentDashboardPageViewModel> GetDashboardAsync(string departmentKey, string fullName, string email, string role, CancellationToken cancellationToken = default);
}
