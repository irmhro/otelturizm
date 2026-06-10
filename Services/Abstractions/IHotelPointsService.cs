namespace otelturizmnew.Services.Abstractions;

public interface IHotelPointsService
{
    void InvalidateCache();

    int CalculateEarnPoints(decimal totalAmount);

    decimal? CalculateDiscountPercent(int availablePoints);

    int? EstimateFromNightlyPrice(decimal? nightlyPrice);

    Task<bool> TryAwardReservationPointsAsync(
        long userId,
        long hotelId,
        long reservationId,
        decimal totalAmount,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> AdjustUserHotelPointsAsync(
        long userId,
        long hotelId,
        int pointDelta,
        string reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserHotelPointsBalance>> GetUserHotelBalancesAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartnerDistributedPointsRow>> GetPartnerDistributedPointsAsync(
        long partnerUserId,
        long? hotelId = null,
        CancellationToken cancellationToken = default);
}

public sealed class UserHotelPointsBalance
{
    public long HotelId { get; init; }
    public string HotelName { get; init; } = string.Empty;
    public string HotelCity { get; init; } = string.Empty;
    public int TotalEarned { get; init; }
    public int AvailablePoints { get; init; }
    public int UsedPoints { get; init; }
    public decimal? DiscountPercent { get; init; }
    public string? LastEarnedText { get; init; }
    public int StayCount { get; init; }
    public string? LastStayText { get; init; }
    public IReadOnlyList<UserHotelPointMovement> RecentMovements { get; init; } = Array.Empty<UserHotelPointMovement>();
}

public sealed class UserHotelPointMovement
{
    public string DateText { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int PointChange { get; init; }
    public string PointChangeText { get; init; } = string.Empty;
}

public sealed class PartnerDistributedPointsRow
{
    public long HotelId { get; init; }
    public string HotelName { get; init; } = string.Empty;
    public long UserId { get; init; }
    public string UserDisplayName { get; init; } = string.Empty;
    public int TotalEarned { get; init; }
    public int AvailablePoints { get; init; }
    public int UsedPoints { get; init; }
    public string? LastEarnedText { get; init; }
}
