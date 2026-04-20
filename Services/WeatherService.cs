using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public WeatherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<HotelWeatherWidgetViewModel?> GetForecastAsync(string district, string city, decimal? latitude = null, decimal? longitude = null, CancellationToken cancellationToken = default)
    {
        var location = await ResolveLocationAsync(district, city, latitude, longitude, cancellationToken);
        if (location is null)
        {
            return null;
        }

        var forecastUrl =
            $"https://api.open-meteo.com/v1/forecast?latitude={location.Value.Latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={location.Value.Longitude.ToString(CultureInfo.InvariantCulture)}" +
            "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum" +
            "&timezone=Europe%2FIstanbul&forecast_days=5";

        using var forecastResponse = await _httpClient.GetAsync(forecastUrl, cancellationToken);
        if (!forecastResponse.IsSuccessStatusCode)
        {
            return null;
        }

        await using var forecastStream = await forecastResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var forecastDocument = await JsonDocument.ParseAsync(forecastStream, cancellationToken: cancellationToken);

        if (!forecastDocument.RootElement.TryGetProperty("daily", out var daily))
        {
            return null;
        }

        var dates = daily.GetProperty("time").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        var weatherCodes = daily.GetProperty("weather_code").EnumerateArray().Select(x => x.GetInt32()).ToList();
        var maxTemps = daily.GetProperty("temperature_2m_max").EnumerateArray().Select(x => x.GetDouble()).ToList();
        var minTemps = daily.GetProperty("temperature_2m_min").EnumerateArray().Select(x => x.GetDouble()).ToList();
        var precipitation = daily.GetProperty("precipitation_sum").EnumerateArray().Select(x => x.GetDouble()).ToList();

        var indices = new[] { 0, 1, 2 }
            .Where(index => index < dates.Count && index < weatherCodes.Count && index < maxTemps.Count && index < minTemps.Count && index < precipitation.Count)
            .ToList();

        if (indices.Count == 0)
        {
            return null;
        }

        var model = new HotelWeatherWidgetViewModel
        {
            LocationLabel = string.IsNullOrWhiteSpace(district) ? city : $"{district}, {city}",
            Summary = "Bugün dahil 3 günlük ilçe bazlı hava tahmini"
        };

        foreach (var index in indices)
        {
            var (conditionText, iconClass) = MapWeatherCode(weatherCodes[index]);
            var date = DateTime.TryParse(dates[index], out var parsedDate)
                ? parsedDate.ToString("dd MMMM ddd", new CultureInfo("tr-TR"))
                : dates[index];

            model.Days.Add(new HotelWeatherDayViewModel
            {
                PeriodLabel = index switch
                {
                    0 => "Bugün",
                    1 => "Yarın",
                    2 => "3. Gün",
                    _ => $"{index + 1} Günlük"
                },
                DateLabel = date,
                ConditionText = conditionText,
                IconClass = iconClass,
                TemperatureText = $"{Math.Round(maxTemps[index], 0, MidpointRounding.AwayFromZero):0}° / {Math.Round(minTemps[index], 0, MidpointRounding.AwayFromZero):0}°",
                PrecipitationText = precipitation[index] > 0
                    ? $"Yağış {precipitation[index]:0.#} mm"
                    : "Yağış beklenmiyor"
            });
        }

        return model;
    }

    private async Task<(double Latitude, double Longitude)?> ResolveLocationAsync(string district, string city, decimal? latitude, decimal? longitude, CancellationToken cancellationToken)
    {
        if (latitude.HasValue && longitude.HasValue)
        {
            return ((double)latitude.Value, (double)longitude.Value);
        }

        var districtCoordinates = await ResolveDistrictCoordinatesFromDatabaseAsync(district, city, cancellationToken);
        if (districtCoordinates is not null)
        {
            return districtCoordinates;
        }

        var queries = new List<string>();

        if (!string.IsNullOrWhiteSpace(district) && !string.IsNullOrWhiteSpace(city))
        {
            queries.Add($"{district}, {city}, Türkiye");
            queries.Add($"{district}, {city}, Turkey");
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            queries.Add($"{city}, Türkiye");
            queries.Add($"{city}, Turkey");
        }

        foreach (var query in queries.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=8&language=tr&format=json";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            JsonElement? bestMatch = null;
            var districtLower = district?.Trim().ToLowerInvariant() ?? string.Empty;
            var cityLower = city?.Trim().ToLowerInvariant() ?? string.Empty;

            foreach (var item in results.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var nameProp) ? (nameProp.GetString() ?? string.Empty).ToLowerInvariant() : string.Empty;
                var admin2 = item.TryGetProperty("admin2", out var admin2Prop) ? (admin2Prop.GetString() ?? string.Empty).ToLowerInvariant() : string.Empty;
                var admin1 = item.TryGetProperty("admin1", out var admin1Prop) ? (admin1Prop.GetString() ?? string.Empty).ToLowerInvariant() : string.Empty;
                var country = item.TryGetProperty("country", out var countryProp) ? (countryProp.GetString() ?? string.Empty).ToLowerInvariant() : string.Empty;

                if (!country.Contains("tur", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(districtLower) &&
                    (name.Contains(districtLower, StringComparison.OrdinalIgnoreCase) || admin2.Contains(districtLower, StringComparison.OrdinalIgnoreCase)))
                {
                    if (string.IsNullOrWhiteSpace(cityLower) || admin1.Contains(cityLower, StringComparison.OrdinalIgnoreCase) || admin2.Contains(cityLower, StringComparison.OrdinalIgnoreCase))
                    {
                        bestMatch = item;
                        break;
                    }
                }

                bestMatch ??= item;
            }

            if (bestMatch.HasValue &&
                bestMatch.Value.TryGetProperty("latitude", out var latitudeProp) &&
                bestMatch.Value.TryGetProperty("longitude", out var longitudeProp))
            {
                return (latitudeProp.GetDouble(), longitudeProp.GetDouble());
            }
        }

        return null;
    }

    private async Task<(double Latitude, double Longitude)?> ResolveDistrictCoordinatesFromDatabaseAsync(string district, string city, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(district))
        {
            return null;
        }

        var normalizedDistrict = NormalizeLocationText(district);
        var normalizedCity = NormalizeLocationText(city);

        const string sql = """
            SELECT ic.ilce_adi, il.il_adi, ic.enlem, ic.boylam
            FROM ilceler ic
            INNER JOIN iller il ON il.id = ic.il_id
            WHERE ic.aktif_mi = 1
              AND il.aktif_mi = 1
              AND ic.enlem IS NOT NULL
              AND ic.boylam IS NOT NULL;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var districtName = NormalizeLocationText(reader.GetString(0));
            var cityName = NormalizeLocationText(reader.GetString(1));

            if (districtName == normalizedDistrict && (string.IsNullOrWhiteSpace(normalizedCity) || cityName == normalizedCity))
            {
                var latitudeValue = reader.GetValue(2);
                var longitudeValue = reader.GetValue(3);
                return (
                    Convert.ToDouble(latitudeValue, CultureInfo.InvariantCulture),
                    Convert.ToDouble(longitudeValue, CultureInfo.InvariantCulture));
            }
        }

        return null;
    }

    private static (string Text, string IconClass) MapWeatherCode(int code)
    {
        return code switch
        {
            0 => ("Açık", "fa-sun"),
            1 or 2 => ("Az Bulutlu", "fa-cloud-sun"),
            3 => ("Bulutlu", "fa-cloud"),
            45 or 48 => ("Sisli", "fa-smog"),
            51 or 53 or 55 => ("Çisenti", "fa-cloud-rain"),
            56 or 57 => ("Donan Çisenti", "fa-cloud-meatball"),
            61 or 63 or 65 => ("Yağmurlu", "fa-cloud-showers-heavy"),
            66 or 67 => ("Dondurucu Yağmur", "fa-cloud-rain"),
            71 or 73 or 75 or 77 => ("Karlı", "fa-snowflake"),
            80 or 81 or 82 => ("Sağanak", "fa-cloud-showers-heavy"),
            85 or 86 => ("Kar Sağanağı", "fa-snowflake"),
            95 => ("Gök Gürültülü", "fa-bolt"),
            96 or 99 => ("Fırtına", "fa-cloud-bolt"),
            _ => ("Değişken", "fa-cloud-sun")
        };
    }

    private static string NormalizeLocationText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("I", "i", StringComparison.Ordinal)
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("Ğ", "G", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("Ü", "U", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("Ş", "S", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("Ö", "O", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("Ç", "C", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
