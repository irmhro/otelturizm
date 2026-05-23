using otelturizmnew.Models.Paneller;

namespace otelturizmnew.Services.Abstractions;

public interface IPlatformPackageService
{
    Task<PartnerPlatformPackagesPageViewModel> GetPartnerCatalogAsync(long userId, long? hotelId, string? categoryCode, CancellationToken cancellationToken = default);
    Task<PartnerPlatformPackageDetailPageViewModel?> GetPartnerPackageDetailAsync(long userId, long? hotelId, long packageId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreatePartnerApplicationAsync(long userId, PartnerPlatformPackageApplicationFormModel request, CancellationToken cancellationToken = default);

    Task<AdminPlatformPackagesPageViewModel> GetAdminPageAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<string> ExportAdminApplicationsCsvAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReviewApplicationAsync(long adminUserId, AdminPlatformPackageApplicationDecisionRequest request, CancellationToken cancellationToken = default);
}
