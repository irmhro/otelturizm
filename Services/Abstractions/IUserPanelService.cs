using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Models.Paneller.User;

namespace otelturizmnew.Services.Abstractions;

public interface IUserPanelService
{
    /// <summary>
    /// Kenar çubuğu rozetleri için (sayfa ViewData vermediyse layout doldurur).
    /// </summary>
    Task<(int TotalReservations, int FavoriteCount, int MessageThreads)> GetNavBadgeCountsAsync(long userId, CancellationToken cancellationToken = default);
    Task<(string TierName, int AvailablePoints)> GetLoyaltyTierChipAsync(long userId, CancellationToken cancellationToken = default);

    Task<UserDashboardPageViewModel> GetDashboardAsync(
        long userId,
        string? reservationStatus = null,
        DateOnly? reservationStartDate = null,
        DateOnly? reservationEndDate = null,
        int reservationPage = 1,
        int reservationPageSize = 5,
        string? favoriteSort = null,
        CancellationToken cancellationToken = default);
    Task<UserReservationsPageViewModel> GetReservationsAsync(
        long userId,
        string? statusFilter = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int page = 1,
        int pageSize = 5,
        string? searchTerm = null,
        string? sort = null,
        CancellationToken cancellationToken = default);
    Task<string> ExportReservationsCsvAsync(
        long userId,
        string? statusFilter = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? searchTerm = null,
        string? sort = null,
        CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CancelReservationAsync(long userId, long reservationId, string cancellationReason, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveReservationNoteAsync(long userId, UserReservationNoteForm form, CancellationToken cancellationToken = default);
    Task<UserReservationReviewPageViewModel?> GetReservationReviewPageAsync(long userId, long reservationId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SubmitReservationReviewAsync(long userId, UserReservationReviewForm form, CancellationToken cancellationToken = default);
    Task<UserReviewsPageViewModel> GetReviewsAsync(long userId, string? statusFilter = null, string? searchTerm = null, int page = 1, CancellationToken cancellationToken = default);
    Task<bool> CanUserWriteReviewForReservationAsync(long userId, long reservationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HotelEligibleReviewStayViewModel>> GetEligibleReviewStaysForHotelAsync(long userId, long hotelId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateReviewAsync(long userId, UserReviewUpdateForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteReviewAsync(long userId, UserReviewDeleteForm form, CancellationToken cancellationToken = default);
    Task<UserMessagesPageViewModel> GetMessagesAsync(long userId, long? conversationId, CancellationToken cancellationToken = default);
    Task<UserLoyaltyPageViewModel> GetLoyaltyAsync(long userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveBudgetPlanAsync(long userId, UserLoyaltyBudgetPlanForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveTravelPlanAsync(long userId, UserLoyaltyTravelPlanForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RedeemRewardAsync(long userId, UserLoyaltyRedeemForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendMessageAsync(long userId, MessageSendRequest form, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteMessageAsync(long userId, MessageDeleteRequest form, CancellationToken cancellationToken = default);
    Task<UserProfilePageViewModel> GetProfileAsync(long userId, CancellationToken cancellationToken = default);
    Task<string> GetProfileImageUrlAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> SaveProfileAsync(long userId, UserProfileForm form, CancellationToken cancellationToken = default);
    Task<bool> SaveTravelPreferencesAsync(long userId, UserTravelPreferencesForm form, CancellationToken cancellationToken = default);
    Task<bool> SaveProfileImageAsync(long userId, string imageUrl, string source, CancellationToken cancellationToken = default);
    Task<bool> DeleteProfileImageAsync(long userId, long fileId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RequestEmailUpdateAsync(long userId, UserEmailUpdateRequestForm form, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> VerifyEmailUpdateAsync(long userId, UserEmailUpdateVerifyForm form, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<UserNotificationsPageViewModel> GetNotificationsAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> SaveNotificationsAsync(long userId, UserNotificationPreferencesForm form, CancellationToken cancellationToken = default);
    Task<UserSecurityPageViewModel> GetSecurityAsync(long userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ChangePasswordAsync(long userId, UserChangePasswordForm form, CancellationToken cancellationToken = default);
    Task<bool> SaveTwoFactorAsync(long userId, UserTwoFactorForm form, CancellationToken cancellationToken = default);
    Task<UserInvoicesPageViewModel> GetInvoicesAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserPaymentMethodsPageViewModel> GetPaymentMethodsAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> SavePaymentMethodAsync(long userId, UserPaymentMethodForm form, CancellationToken cancellationToken = default);
    Task<bool> DeletePaymentMethodAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveBillingInfoAsync(long userId, UserBillingForm form, CancellationToken cancellationToken = default);
}
