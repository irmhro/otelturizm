using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminHotelManagementService
{
    Task<AdminHotelsPageViewModel> GetHotelsPageAsync(string fullName, string email, string userRole, string? searchTerm = null, string? city = null, string? district = null, string? neighborhood = null, string? publishStatus = null, string? approvalStatus = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<AdminHotelManagementPageViewModel> GetHotelManagementPageAsync(long hotelId, string fullName, string email, string userRole, long? roomId = null, long? hotelPhotoId = null, long? roomPhotoId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveHotelAsync(long adminUserId, AdminHotelEditForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, long? HotelId)> CreateHotelAsync(long adminUserId, AdminHotelEditForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteHotelAsync(long hotelId, long adminUserId, CancellationToken cancellationToken = default);
    Task<AdminHotelManagementPageViewModel> GetNewHotelFormPageAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveRoomAsync(AdminRoomEditForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeactivateHotelAsync(long hotelId, long adminUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ActivateHotelAsync(long hotelId, long adminUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, int UpdatedCount)> BulkUpdateHotelPublishStatusAsync(IReadOnlyList<long> hotelIds, bool publish, long adminUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeactivateRoomAsync(long hotelId, long roomId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UploadHotelPhotosAsync(long adminUserId, AdminHotelPhotoUploadForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateHotelPhotoAsync(AdminHotelPhotoEditForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SetHotelCoverAsync(long hotelId, long photoId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteHotelPhotoAsync(long hotelId, long photoId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UploadRoomPhotosAsync(long adminUserId, AdminRoomPhotoUploadForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateRoomPhotoAsync(AdminRoomPhotoEditForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SetRoomCoverAsync(long hotelId, long roomId, long photoId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteRoomPhotoAsync(long hotelId, long roomId, long photoId, CancellationToken cancellationToken = default);
}
