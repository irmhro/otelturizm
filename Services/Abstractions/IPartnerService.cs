using otelturizmnew.Models.Paneller.Partner;

namespace otelturizmnew.Services.Abstractions;

public interface IPartnerService
{
    Task<PartnerDashboardViewModel> GetDashboardAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<PartnerReservationsPageViewModel> GetReservationsAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? status = null, string? paymentMethod = null, int page = 1, int pageSize = 10, long? conversationId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateReservationStatusAsync(long userId, PartnerReservationStatusRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendGuestMessageAsync(long userId, PartnerGuestMessageRequest request, CancellationToken cancellationToken = default);
    Task<string> ExportReservationsCsvAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<PartnerPricingPageViewModel> GetPricingAsync(long userId, long? hotelId = null, long? roomId = null, string? month = null, CancellationToken cancellationToken = default);
    Task<PartnerCampaignsPageViewModel> GetCampaignsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ApplyBulkPricingAsync(long userId, PartnerBulkPricingUpdateRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ApplyDailyPricingAsync(long userId, PartnerDailyPricingUpdateRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> JoinCampaignAsync(long userId, PartnerCampaignJoinRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> LeaveCampaignAsync(long userId, long hotelId, long campaignId, CancellationToken cancellationToken = default);
    Task<PartnerRoomManagementPageViewModel> GetRoomsAsync(long userId, long? hotelId = null, long? roomId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpsertRoomAsync(long userId, PartnerRoomUpsertRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteRoomAsync(long userId, long hotelId, long roomId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UploadRoomPhotosAsync(long userId, PartnerRoomPhotoUploadRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SetRoomCoverAsync(long userId, long hotelId, long roomId, long photoId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteRoomPhotoAsync(long userId, long hotelId, long roomId, long photoId, CancellationToken cancellationToken = default);
    Task<PartnerHotelInfoPageViewModel> GetHotelInfoAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateHotelInfoAsync(long userId, PartnerHotelInfoForm request, CancellationToken cancellationToken = default);
    Task<PartnerPhotosPageViewModel> GetPhotosAsync(long userId, long? hotelId = null, long? photoId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UploadPhotosAsync(long userId, PartnerPhotoUploadRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SetCoverPhotoAsync(long userId, long hotelId, long photoId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdatePhotoAsync(long userId, PartnerPhotoEditForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeletePhotoAsync(long userId, long hotelId, long photoId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> BulkDeletePhotosAsync(long userId, PartnerPhotoBulkDeleteRequest request, CancellationToken cancellationToken = default);
    Task<PartnerPerformancePageViewModel> GetPerformanceAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveCompetitorAsync(long userId, PartnerCompetitorUpsertRequest request, CancellationToken cancellationToken = default);
    Task<string> ExportPerformanceCsvAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<PartnerReviewsPageViewModel> GetReviewsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReplyToReviewAsync(long userId, PartnerReviewReplyRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReportReviewAsync(long userId, PartnerReviewReportRequest request, CancellationToken cancellationToken = default);
    Task<PartnerFinancePageViewModel> GetFinanceAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveBankInfoAsync(long userId, PartnerBankInfoForm request, CancellationToken cancellationToken = default);
    Task<string> ExportFinanceCsvAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string ContentType, string FileName)?> DownloadInvoiceAsync(long userId, long hotelId, long invoiceId, CancellationToken cancellationToken = default);
    Task<PartnerPreferencesPageViewModel> GetPreferencesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SavePreferencesAsync(long userId, PartnerPreferencesForm request, CancellationToken cancellationToken = default);
    Task<PartnerSupportPageViewModel> GetSupportAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreateSupportTicketAsync(long userId, PartnerSupportCreateTicketRequest request, CancellationToken cancellationToken = default);
}
