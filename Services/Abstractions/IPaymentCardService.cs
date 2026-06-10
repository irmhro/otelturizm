using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Paneller.User;
using otelturizmnew.Models.Payments;

namespace otelturizmnew.Services.Abstractions;

public interface IPaymentCardService
{
    Task<UserPaymentMethodsPageViewModel> GetPaymentMethodsPageAsync(long userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SavePaymentMethodAsync(long userId, UserPaymentMethodForm form, CancellationToken cancellationToken = default);
    Task<bool> DeletePaymentMethodAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default);
    Task<bool> SetDefaultPaymentMethodAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedPaymentCardOptionViewModel>> GetUserCardOptionsAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> UserOwnsActiveCardAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default);
    Task CreateReservationSnapshotAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long reservationId,
        long userId,
        long savedPaymentCardId,
        CancellationToken cancellationToken = default);
    Task<PartnerPaymentCardViewResult> TryPartnerViewCardAsync(
        long partnerUserId,
        long hotelId,
        long reservationId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<bool> ReservationHasSavedCardAsync(long reservationId, CancellationToken cancellationToken = default);
}
