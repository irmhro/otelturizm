using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using otelturizmnew.Models.Anasayfa;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class UtilityPulseService : IUtilityPulseService
{
    private const string CacheKey = "utility-pulse-bar:v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly string _connectionString;
    private readonly IMemoryCache _cache;
    private readonly ITimeZoneService _timeZoneService;

    public UtilityPulseService(
        IConfiguration configuration,
        IMemoryCache cache,
        ITimeZoneService timeZoneService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _cache = cache;
        _timeZoneService = timeZoneService;
    }

    public async Task<UtilityPulseBarViewModel> GetBarAsync(CancellationToken cancellationToken = default)
    {
        var nowLocal = _timeZoneService.ToLocal(DateTime.UtcNow);
        var timeLabel = nowLocal.ToString("HH:mm");
        var greeting = ResolveGreeting(nowLocal.Hour);

        if (_cache.TryGetValue(CacheKey, out UtilityPulseBarViewModel? cached) && cached is not null)
        {
            return cached with
            {
                Greeting = greeting,
                LocalTimeLabel = timeLabel
            };
        }

        var model = await LoadFromDatabaseAsync(greeting, timeLabel, cancellationToken);
        _cache.Set(CacheKey, model, CacheDuration);
        return model;
    }

    private async Task<UtilityPulseBarViewModel> LoadFromDatabaseAsync(
        string greeting,
        string timeLabel,
        CancellationToken cancellationToken)
    {
        var destinations = new List<UtilityPulseDestinationViewModel>();
        var hotelCount = 0;
        var campaignCount = 0;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string hotelCountSql = """
            SELECT COUNT(*)
            FROM oteller o
            WHERE o.yayin_durumu = N'Yayında';
            """;

        await using (var hotelCountCommand = new SqlCommand(hotelCountSql, connection))
        {
            var result = await hotelCountCommand.ExecuteScalarAsync(cancellationToken);
            hotelCount = result is int count ? count : Convert.ToInt32(result ?? 0);
        }

        const string campaignCountSql = """
            SELECT COUNT(*)
            FROM kampanyalar k
            WHERE k.aktif_mi = 1
              AND k.gorunurluk_durumu = N'Yayında';
            """;

        await using (var campaignCountCommand = new SqlCommand(campaignCountSql, connection))
        {
            var result = await campaignCountCommand.ExecuteScalarAsync(cancellationToken);
            campaignCount = result is int count ? count : Convert.ToInt32(result ?? 0);
        }

        const string destinationsSql = """
            SELECT TOP (4)
                LTRIM(RTRIM(o.sehir)) AS city,
                COUNT(*) AS hotel_count
            FROM oteller o
            WHERE o.yayin_durumu = N'Yayında'
              AND NULLIF(LTRIM(RTRIM(o.sehir)), '') IS NOT NULL
            GROUP BY LTRIM(RTRIM(o.sehir))
            ORDER BY COUNT(*) DESC;
            """;

        await using (var destinationsCommand = new SqlCommand(destinationsSql, connection))
        await using (var reader = await destinationsCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var city = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
                if (string.IsNullOrWhiteSpace(city))
                {
                    continue;
                }

                destinations.Add(new UtilityPulseDestinationViewModel
                {
                    City = city,
                    HotelCount = reader.GetInt32(1),
                    ListingUrl = $"/oteller?q={Uri.EscapeDataString(city)}"
                });
            }
        }

        if (destinations.Count == 0)
        {
            destinations.AddRange(new[]
            {
                new UtilityPulseDestinationViewModel { City = "Antalya", HotelCount = 0, ListingUrl = "/oteller?q=Antalya" },
                new UtilityPulseDestinationViewModel { City = "İstanbul", HotelCount = 0, ListingUrl = "/oteller?q=%C4%B0stanbul" },
                new UtilityPulseDestinationViewModel { City = "Muğla", HotelCount = 0, ListingUrl = "/oteller?q=Mu%C4%9Fla" },
                new UtilityPulseDestinationViewModel { City = "Kapadokya", HotelCount = 0, ListingUrl = "/oteller?q=Kapadokya" }
            });
        }

        var insights = BuildInsights(hotelCount, campaignCount);

        return new UtilityPulseBarViewModel
        {
            ActiveHotelCount = hotelCount,
            ActiveCampaignCount = campaignCount,
            TopDestinations = destinations,
            Insights = insights,
            Greeting = greeting,
            LocalTimeLabel = timeLabel
        };
    }

    private static IReadOnlyList<UtilityPulseInsightViewModel> BuildInsights(int hotelCount, int campaignCount)
    {
        var insights = new List<UtilityPulseInsightViewModel>
        {
            new()
            {
                IconClass = "fas fa-shield-halved",
                Text = "Güvenli ödeme altyapısı"
            },
            new()
            {
                IconClass = "fas fa-headset",
                Text = "7/24 müşteri desteği",
                LinkUrl = "/yardim-merkezi"
            },
            new()
            {
                IconClass = "fas fa-hotel",
                Text = hotelCount > 0 ? $"{FormatCount(hotelCount)} onaylı otel" : "Onaylı otel ağı",
                LinkUrl = "/oteller"
            }
        };

        if (campaignCount > 0)
        {
            insights.Add(new UtilityPulseInsightViewModel
            {
                IconClass = "fas fa-tags",
                Text = $"{campaignCount} aktif kampanya",
                LinkUrl = "/kampanyalar"
            });
        }
        else
        {
            insights.Add(new UtilityPulseInsightViewModel
            {
                IconClass = "fas fa-rotate-left",
                Text = "Şeffaf iptal koşulları"
            });
        }

        return insights;
    }

    private static string ResolveGreeting(int hour) => hour switch
    {
        >= 5 and < 12 => "Günaydın",
        >= 12 and < 18 => "İyi günler",
        >= 18 and < 23 => "İyi akşamlar",
        _ => "İyi geceler"
    };

    private static string FormatCount(int count)
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        return count >= 1000
            ? $"{count.ToString("N0", tr)}+"
            : count.ToString("N0", tr);
    }
}
