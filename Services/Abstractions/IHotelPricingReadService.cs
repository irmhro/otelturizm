namespace otelturizmnew.Services.Abstractions;

public interface IHotelPricingReadService
{
    Task<decimal?> GetHotelEffectivePriceAsync(long hotelId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, decimal>> GetHotelEffectivePriceMapAsync(IReadOnlyCollection<long> hotelIds, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<decimal> GetRoomAverageNightlyPriceAsync(long roomTypeId, DateOnly checkInDate, DateOnly checkOutDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, decimal>> GetRoomAverageNightlyPriceMapAsync(IReadOnlyCollection<long> roomTypeIds, DateOnly checkInDate, DateOnly checkOutDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomNightlyPricePoint>> GetRoomNightlyBreakdownAsync(long roomTypeId, DateOnly checkInDate, DateOnly checkOutDate, CancellationToken cancellationToken = default);
}

public sealed class RoomNightlyPricePoint
{
    public DateOnly Date { get; init; }
    public decimal EffectivePrice { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? DiscountPrice { get; init; }
    public short RemainingRooms { get; init; }
    public bool IsClosed { get; init; }
    public bool IsAvailable { get; init; } = true;
}
