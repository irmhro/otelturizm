using otelturizmnew.Models.Paneller.Developer;

namespace otelturizmnew.Services.Abstractions;

public interface IDevelopmentRequestService
{
    Task<DeveloperDashboardViewModel> GetDeveloperDashboardAsync(long currentUserId, string fullName, string email, string? searchText = null, string? statusFilter = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreateRequestAsync(long currentUserId, DeveloperRequestCreateForm form, string? imageUrl, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> AddDeveloperReplyAsync(long currentUserId, DeveloperRequestReplyForm form, string? imageUrl, CancellationToken cancellationToken = default);
    Task<AdminDevelopmentRequestsPageViewModel> GetAdminPageAsync(string fullName, string email, string userRole, string? searchText = null, string? statusFilter = null, string? priorityFilter = null, long? developerFilterUserId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveAdminRequestAsync(long adminUserId, AdminDevelopmentRequestUpdateForm form, string? imageUrl, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteRequestAsync(long adminUserId, long requestId, string? note, CancellationToken cancellationToken = default);
}
