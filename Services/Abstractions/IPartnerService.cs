using otelturizmnew.Models.Paneller.Partner;

namespace otelturizmnew.Services.Abstractions;

public interface IPartnerService
{
    Task<PartnerDashboardViewModel> GetDashboardAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? status = null, string? paymentMethod = null, int pageSize = 7, long? conversationId = null, CancellationToken cancellationToken = default);
    Task<PartnerReservationsPageViewModel> GetReservationsAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? status = null, string? paymentMethod = null, int page = 1, int pageSize = 7, long? conversationId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateReservationStatusAsync(long userId, PartnerReservationStatusRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendGuestMessageAsync(long userId, PartnerGuestMessageRequest request, CancellationToken cancellationToken = default);
    Task<PartnerGuestMessagesPageViewModel> GetGuestMessagesAsync(long userId, long? hotelId = null, long? conversationId = null, CancellationToken cancellationToken = default);
    Task<PartnerReservationCalendarPageViewModel> GetReservationCalendarAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default);
    Task<PartnerCancellationNoShowPageViewModel> GetCancellationNoShowAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<PartnerPaymentStatusesPageViewModel> GetPaymentStatusesAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? paymentStatus = null, string? paymentMethod = null, CancellationToken cancellationToken = default);

    Task<PartnerCompanyReservationsPageViewModel> GetCompanyReservationsAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, long? companyId = null, string? status = null, string? dateRangeMode = null, bool completedStaysOnly = false, CancellationToken cancellationToken = default);
    Task<PartnerCompanyAnalyticsPageViewModel> GetCompanyAnalyticsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<PartnerCompanyRequestsPageViewModel> GetCompanyRequestsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
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
    Task<(bool Success, string Message)> UpdateHotelAmenitiesAsync(long userId, PartnerHotelAmenitiesUpdateRequest request, CancellationToken cancellationToken = default);
    Task<PartnerHotelLocationPageViewModel> GetHotelLocationAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<List<PartnerLocationOptionViewModel>> GetCityOptionsAsync(CancellationToken cancellationToken = default);
    Task<List<PartnerLocationOptionViewModel>> GetDistrictOptionsAsync(long cityId, CancellationToken cancellationToken = default);
    Task<List<PartnerLocationOptionViewModel>> GetNeighborhoodOptionsAsync(long districtId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateHotelLocationAsync(long userId, PartnerHotelLocationUpdateRequest request, CancellationToken cancellationToken = default);
    Task<PartnerHotelPoliciesPageViewModel> GetHotelPoliciesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateHotelPoliciesAsync(long userId, PartnerHotelPoliciesForm request, CancellationToken cancellationToken = default);
    Task<PartnerRoomFeaturesPageViewModel> GetRoomFeaturesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> AddRoomFeatureAsync(long userId, PartnerRoomFeatureAddRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ToggleRoomFeatureAsync(long userId, PartnerRoomFeatureToggleRequest request, CancellationToken cancellationToken = default);
    Task<PartnerPhotosPageViewModel> GetPhotosAsync(long userId, long? hotelId = null, long? photoId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UploadPhotosAsync(long userId, PartnerPhotoUploadRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SetCoverPhotoAsync(long userId, long hotelId, long photoId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdatePhotoAsync(long userId, PartnerPhotoEditForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeletePhotoAsync(long userId, long hotelId, long photoId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> BulkDeletePhotosAsync(long userId, PartnerPhotoBulkDeleteRequest request, CancellationToken cancellationToken = default);
    Task<PartnerPerformancePageViewModel> GetPerformanceAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveCompetitorAsync(long userId, PartnerCompetitorUpsertRequest request, CancellationToken cancellationToken = default);
    Task<string> ExportPerformanceCsvAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<PartnerReviewsPageViewModel> GetReviewsAsync(long userId, long? hotelId = null, string? status = null, string? replyState = null, int page = 1, int pageSize = 7, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReplyToReviewAsync(long userId, PartnerReviewReplyRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ReportReviewAsync(long userId, PartnerReviewReportRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RequestReviewTakedownAsync(long userId, long hotelId, long reviewId, string? reason, CancellationToken cancellationToken = default);
    Task<PartnerFinancePageViewModel> GetFinanceAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default, bool includeCommissions = false, DateTime? dateFrom = null, DateTime? dateTo = null, string? paymentStatus = null, int commissionPageSize = 50, string? donem = null);

    Task<string> ExportPartnerCommissionsCsvAsync(long userId, long? hotelId, string? donem, DateTime? dateFrom, DateTime? dateTo, string? paymentStatus, CancellationToken cancellationToken = default);
    Task<PartnerFinancePageViewModel> GetPartnerCommissionsPageAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? paymentStatus = null, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> MarkCommissionPaidOnlineAsync(long userId, long hotelId, long commissionRecordId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveBankInfoAsync(long userId, PartnerBankInfoForm request, CancellationToken cancellationToken = default);
    Task<string> ExportFinanceCsvAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string ContentType, string FileName)?> DownloadInvoiceAsync(long userId, long hotelId, long invoiceId, CancellationToken cancellationToken = default);
    Task<PartnerReservationGuestInvoicesPageViewModel> GetGuestInvoicesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveGuestInvoiceAsync(long userId, long hotelId, long reservationId, long secureFileId, string? fileName, string? mimeType, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> MarkReservationPaymentCompletedAsync(long userId, long hotelId, long reservationId, CancellationToken cancellationToken = default);
    Task<PartnerApplicationPageViewModel> GetApplicationAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveApplicationAsync(long userId, PartnerApplicationProfileForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UploadApplicationDocumentAsync(long userId, PartnerApplicationDocumentUploadForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteApplicationDocumentAsync(long userId, long hotelId, long documentId, CancellationToken cancellationToken = default);
    Task<PartnerSupportPageViewModel> GetSupportAsync(long userId, long? hotelId = null, long? ticketId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreateSupportTicketAsync(long userId, PartnerSupportCreateTicketRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendSupportMessageAsync(long userId, PartnerSupportSendMessageRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CloseSupportTicketAsync(long userId, long hotelId, long ticketId, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> SaveThemeAsync(long userId, long? hotelId, string scope, PanelThemeViewModel theme, CancellationToken cancellationToken = default);

    Task<PartnerFacilityUsersPageViewModel> GetFacilityUsersAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> InviteFacilityUserAsync(long userId, PartnerFacilityUserInviteRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RevokeFacilityUserAsync(long userId, PartnerFacilityUserRevokeRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ApproveFacilityInviteAsync(long userId, string token, CancellationToken cancellationToken = default);

    Task<PartnerCompanyPricingPageViewModel> GetCompanyPricingAsync(long userId, long? hotelId = null, long? companyId = null, long? roomId = null, string? month = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ApplyCompanyBulkPricingAsync(long userId, PartnerCompanyBulkPricingUpdateRequest request, CancellationToken cancellationToken = default);

    Task<PartnerListingSubscriptionsPageViewModel> GetListingSubscriptionsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreateListingSubscriptionAsync(long userId, PartnerListingSubscriptionCreateRequest request, CancellationToken cancellationToken = default);

    Task<PartnerLocationInsightsPageViewModel> GetLocationInsightsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<PartnerFavoriteGuestsPageViewModel> GetFavoriteGuestsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<PartnerMarketingEventsPageViewModel> GetMarketingEventsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveCampaignParticipationNoteAsync(long userId, PartnerCampaignParticipationNoteRequest request, CancellationToken cancellationToken = default);

    Task<PartnerSettingsPageViewModel> GetPartnerSettingsPageAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<PartnerPreferencesPageViewModel> GetPartnerPreferencesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SavePartnerPreferencesAsync(long userId, PartnerPreferencesForm form, CancellationToken cancellationToken = default);
    Task<PartnerAccountInfoPageViewModel> GetAccountInfoAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveAccountInfoAsync(long userId, PartnerAccountInfoUpdateForm form, CancellationToken cancellationToken = default);
}
