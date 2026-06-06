namespace otelturizmnew.Models.Oteller;

public sealed record HotelDetailLoadOptions
{
    public DateOnly? CheckIn { get; init; }
    public DateOnly? CheckOut { get; init; }
    public long? RoomTypeId { get; init; }

    public bool HasFilters =>
        CheckIn.HasValue || CheckOut.HasValue || RoomTypeId is > 0;
}
