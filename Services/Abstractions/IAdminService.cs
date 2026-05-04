using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminSectionPageViewModel> GetSectionPageAsync(string sectionKey, string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminSystemHealthPageViewModel> GetSystemHealthAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminPartnerApplicationsPageViewModel> GetPartnerApplicationsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReviewPartnerApplicationAsync(long adminUserId, AdminPartnerApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SetPartnerEmailLoginApprovalAsync(long adminUserId, AdminPartnerEmailLoginApprovalRequest request, CancellationToken cancellationToken = default);
    Task<AdminCompanyApplicationsPageViewModel> GetCompanyApplicationsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReviewCompanyApplicationAsync(long adminUserId, AdminCompanyApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<AdminApprovalCenterPageViewModel> GetApprovalCenterAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminCommissionManagementPageViewModel> GetCommissionManagementAsync(string fullName, string email, string userRole, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveCommissionRuleAsync(long adminUserId, AdminCommissionRuleForm request, CancellationToken cancellationToken = default);

    Task<AdminListingSubscriptionsPageViewModel> GetListingSubscriptionsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReviewListingSubscriptionAsync(long adminUserId, AdminListingSubscriptionDecisionRequest request, CancellationToken cancellationToken = default);

    // Paket 181-190 (Admin ops)
    Task<AdminActionLogsPageViewModel> GetAdminActionLogsAsync(string fullName, string email, string userRole, AdminActionLogFilter filter, CancellationToken cancellationToken = default);
    Task<string> ExportAdminActionLogsCsvAsync(AdminActionLogFilter filter, CancellationToken cancellationToken = default);

    Task<AdminEmailQueuePageViewModel> GetEmailQueueAsync(string fullName, string email, string userRole, AdminEmailQueueFilter filter, CancellationToken cancellationToken = default);
    Task<AdminEmailSettingsPageViewModel> GetEmailSettingsPageAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminMailCenterPageViewModel> GetMailCenterAsync(string fullName, string email, string userRole, long? accountId, bool syncInbox, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveMailAccountAsync(long adminUserId, AdminMailAccountForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteMailAccountAsync(long adminUserId, long accountId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, int ImportedCount)> SyncMailAccountAsync(long adminUserId, long accountId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, int QueuedCount)> QueueTemplateTestBatchAsync(long adminUserId, string recipientEmail, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ForceRetryEmailAsync(long adminUserId, long queueId, string reason, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, int RetriedCount)> RetryAllFailedEmailsAsync(long adminUserId, string reason, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> MarkEmailFailedAsync(long adminUserId, long queueId, string reason, CancellationToken cancellationToken = default);

    Task<AdminUnifiedReservationsPageViewModel> GetUnifiedReservationsAsync(string fullName, string email, string userRole, string? q, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminRateLimitStatsPageViewModel> GetRateLimitStatsAsync(string fullName, string email, string userRole, int windowHours = 24, CancellationToken cancellationToken = default);
    Task<AdminSettingsMonitorPageViewModel> GetSettingsMonitorAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<AdminPlatformCheckupPageViewModel> GetPlatformCheckupAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);

    Task<AdminCommerceInsightPageViewModel> GetCommerceInsightPageAsync(string fullName, string email, string userRole, long priceHistoryHotelId, CancellationToken cancellationToken = default);

    Task<AdminReviewModerationPageViewModel> GetReviewModerationPageAsync(string fullName, string email, string userRole, string? q, string? city, string? hotel, int take, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ApplyReviewModerationActionAsync(long adminUserId, AdminReviewModerationActionForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteReviewAsAdminAsync(long adminUserId, AdminReviewDeleteForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> NotifyReviewViolationAsync(long adminUserId, AdminReviewViolationNotifyForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> AddBlockedWordAsync(long adminUserId, AdminBlockedWordAddForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ToggleBlockedWordAsync(long adminUserId, AdminBlockedWordToggleForm form, CancellationToken cancellationToken = default);
}

