using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminSectionPageViewModel> GetSectionPageAsync(string sectionKey, string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminSystemHealthPageViewModel> GetSystemHealthAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminPartnerApplicationsPageViewModel> GetPartnerApplicationsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReviewPartnerApplicationAsync(long adminUserId, AdminPartnerApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<AdminCompanyApplicationsPageViewModel> GetCompanyApplicationsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReviewCompanyApplicationAsync(long adminUserId, AdminCompanyApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<AdminCommissionManagementPageViewModel> GetCommissionManagementAsync(string fullName, string email, string userRole, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveCommissionRuleAsync(long adminUserId, AdminCommissionRuleForm request, CancellationToken cancellationToken = default);

    Task<AdminListingSubscriptionsPageViewModel> GetListingSubscriptionsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReviewListingSubscriptionAsync(long adminUserId, AdminListingSubscriptionDecisionRequest request, CancellationToken cancellationToken = default);

    // Paket 181-190 (Admin ops)
    Task<AdminActionLogsPageViewModel> GetAdminActionLogsAsync(string fullName, string email, string userRole, AdminActionLogFilter filter, CancellationToken cancellationToken = default);
    Task<string> ExportAdminActionLogsCsvAsync(AdminActionLogFilter filter, CancellationToken cancellationToken = default);

    Task<AdminEmailQueuePageViewModel> GetEmailQueueAsync(string fullName, string email, string userRole, AdminEmailQueueFilter filter, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ForceRetryEmailAsync(long adminUserId, long queueId, string reason, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> MarkEmailFailedAsync(long adminUserId, long queueId, string reason, CancellationToken cancellationToken = default);

    Task<AdminUnifiedReservationsPageViewModel> GetUnifiedReservationsAsync(string fullName, string email, string userRole, string? q, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminRateLimitStatsPageViewModel> GetRateLimitStatsAsync(string fullName, string email, string userRole, int windowHours = 24, CancellationToken cancellationToken = default);
    Task<AdminSettingsMonitorPageViewModel> GetSettingsMonitorAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);

    Task<AdminCommerceInsightPageViewModel> GetCommerceInsightPageAsync(string fullName, string email, string userRole, long priceHistoryHotelId, CancellationToken cancellationToken = default);
}

