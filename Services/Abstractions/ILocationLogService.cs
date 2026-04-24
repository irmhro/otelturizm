namespace otelturizmnew.Services.Abstractions;

public interface ILocationLogService
{
    Task SaveUserLocationAsync(LocationLogEntryInput input, CancellationToken cancellationToken = default);
}

public sealed class LocationLogEntryInput
{
    public long? UserId { get; set; }
    public string SessionKey { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int? RadiusKm { get; set; }
    public int? VisibleHotelCount { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public string SearchRegion { get; set; } = string.Empty;
    public string Source { get; set; } = "otel-listeleme";
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string PhoneHint { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
}
