using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Paneller.User;

namespace otelturizmnew.Services.Abstractions;

public interface IUserPanelService
{
    Task<UserDashboardPageViewModel> GetDashboardAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserReservationsPageViewModel> GetReservationsAsync(long userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CancelReservationAsync(long userId, long reservationId, CancellationToken cancellationToken = default);
    Task<UserMessagesPageViewModel> GetMessagesAsync(long userId, long? conversationId, CancellationToken cancellationToken = default);
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
