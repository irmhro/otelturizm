using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminSectionPageViewModel> GetSectionPageAsync(string sectionKey, string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminPartnerApplicationsPageViewModel> GetPartnerApplicationsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReviewPartnerApplicationAsync(long adminUserId, AdminPartnerApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<AdminCommissionManagementPageViewModel> GetCommissionManagementAsync(string fullName, string email, string userRole, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveCommissionRuleAsync(long adminUserId, AdminCommissionRuleForm request, CancellationToken cancellationToken = default);
}

