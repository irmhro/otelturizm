namespace otelturizmnew.Models.Payments;

/// <summary>Ödeme / checkout JSON yanıtları için makine kodları (paket 225).</summary>
public static class CheckoutErrorCatalog
{
    public const string InvalidRoomCount = "invalid_room_count";
    public const string InvalidDateRange = "invalid_date_range";
    public const string NightRange = "invalid_night_range";
    public const string HotelNotFound = "hotel_not_found";
    public const string RoomMismatch = "room_type_mismatch";
    public const string QuoteFailed = "quote_failed";
    public const string RateLimitedVelocity = "rate_limited_velocity";

    public static object JsonError(string code, string message) => new { success = false, errorCode = code, message };
}
