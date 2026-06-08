namespace otelturizmnew.Services.Abstractions;

public interface IUserLoyaltyPointsService
{
    Task<bool> TryAwardReservationPointsAsync(
        long userId,
        long reservationId,
        int points = 4,
        CancellationToken cancellationToken = default);
}
