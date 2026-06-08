using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminHomepageHotelsService
{
    Task<AdminHomepageHotelsPageViewModel> GetPageAsync(string fullName, string email, string userRole, long? activeSectionId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminHomepageHotelSearchResultViewModel>> SearchPublishedHotelsAsync(string? query, int limit = 20, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> AddHotelToSectionAsync(long sectionId, long hotelId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RemoveHotelFromSectionAsync(long entryId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReorderHotelsAsync(long sectionId, IReadOnlyList<long> entryIds, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> MoveHotelAsync(long entryId, string direction, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, long? SectionId)> CreateSectionAsync(AdminHomepageSectionForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateSectionAsync(AdminHomepageSectionForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteSectionAsync(long sectionId, CancellationToken cancellationToken = default);
}
