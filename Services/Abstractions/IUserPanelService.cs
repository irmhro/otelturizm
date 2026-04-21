using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Paneller.User;

namespace otelturizmnew.Services.Abstractions;

public interface IUserPanelService
{
    Task<UserDashboardPageViewModel> GetDashboardAsync(
        long userId,
        string? reservationStatus = null,
        DateOnly? reservationStartDate = null,
        DateOnly? reservationEndDate = null,
        int reservationPage = 1,
        int reservationPageSize = 5,
        CancellationToken cancellationToken = default);
    Task<UserReservationsPageViewModel> GetReservationsAsync(
        long userId,
        string? statusFilter = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int page = 1,
        int pageSize = 5,
        CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CancelReservationAsync(long userId, long reservationId, string cancellationReason, CancellationToken cancellationToken = default);
    Task<UserReservationReviewPageViewModel?> GetReservationReviewPageAsync(long userId, long reservationId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SubmitReservationReviewAsync(long userId, UserReservationReviewForm form, CancellationToken cancellationToken = default);
    Task<UserMessagesPageViewModel> GetMessagesAsync(long userId, long? conversationId, CancellationToken cancellationToken = default);
    Task<UserLoyaltyPageViewModel> GetLoyaltyAsync(long userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveBudgetPlanAsync(long userId, UserLoyaltyBudgetPlanForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveTravelPlanAsync(long userId, UserLoyaltyTravelPlanForm form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendMessageAsync(long userId, MessageSendRequest form, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteMessageAsync(long userId, MessageDeleteRequest form, CancellationToken cancellationToken = default);
    Task<UserProfilePageViewModel> GetProfileAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> SaveProfileAsync(long userId, UserProfileForm form, CancellationToken cancellationToken = default);
    Task<UserNotificationsPageViewModel> GetNotificationsAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> SaveNotificationsAsync(long userId, UserNotificationPreferencesForm form, CancellationToken cancellationToken = default);
    Task<UserSecurityPageViewModel> GetSecurityAsync(long userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ChangePasswordAsync(long userId, UserChangePasswordForm form, CancellationToken cancellationToken = default);
    Task<bool> SaveTwoFactorAsync(long userId, UserTwoFactorForm form, CancellationToken cancellationToken = default);
    Task<UserPaymentMethodsPageViewModel> GetPaymentMethodsAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> SavePaymentMethodAsync(long userId, UserPaymentMethodForm form, CancellationToken cancellationToken = default);
    Task<bool> DeletePaymentMethodAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default);
}
