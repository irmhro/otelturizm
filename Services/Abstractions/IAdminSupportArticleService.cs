using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminSupportArticleService
{
    Task<AdminSupportArticlePageViewModel> GetPageAsync(
        AdminShellViewModel shell,
        string? searchText,
        long? categoryIdFilter,
        string? statusFilter,
        long? editArticleId,
        CancellationToken cancellationToken = default);

    Task<AdminSupportArticleActionResult> SaveAsync(long adminUserId, AdminSupportArticleForm form, CancellationToken cancellationToken = default);
    Task<AdminSupportArticleActionResult> DeleteAsync(long adminUserId, long articleId, CancellationToken cancellationToken = default);
}
