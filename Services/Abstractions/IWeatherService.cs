using otelturizmnew.Models.Oteller;

namespace otelturizmnew.Services.Abstractions;

public interface IWeatherService
{
    Task<HotelWeatherWidgetViewModel?> GetForecastAsync(string district, string city, decimal? latitude = null, decimal? longitude = null, CancellationToken cancellationToken = default);
}
