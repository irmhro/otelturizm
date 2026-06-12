using System.Globalization;
using otelturizmnew.Models.Kampanyalar;
using otelturizmnew.Models.SeyahatPlanlama;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class SeyahatPlanlamaService : ISeyahatPlanlamaService
{
    private static readonly IReadOnlyList<TravelDestinationCardViewModel> CuratedRoutes =
    [
        new()
        {
            Key = "istanbul",
            Title = "İstanbul",
            Subtitle = "Tarih, gastronomi ve Boğaz manzarası",
            SearchQuery = "İstanbul",
            HotelsUrl = "/hotel?q=%C4%B0stanbul",
            ThemeClass = "plan-theme-istanbul",
            IconClass = "fas fa-mosque",
            TypicalNights = 2,
            MinNightlyTry = 1850m
        },
        new()
        {
            Key = "antalya",
            Title = "Antalya",
            Subtitle = "Akdeniz sahili ve aile dostu tesisler",
            SearchQuery = "Antalya",
            HotelsUrl = "/hotel?q=Antalya",
            ThemeClass = "plan-theme-antalya",
            IconClass = "fas fa-umbrella-beach",
            TypicalNights = 3,
            MinNightlyTry = 2200m
        },
        new()
        {
            Key = "kapadokya",
            Title = "Kapadokya",
            Subtitle = "Balon turu ve butik konaklama",
            SearchQuery = "Kapadokya",
            HotelsUrl = "/hotel?q=Kapadokya",
            ThemeClass = "plan-theme-kapadokya",
            IconClass = "fas fa-hot-air-balloon",
            TypicalNights = 2,
            MinNightlyTry = 2600m
        },
        new()
        {
            Key = "bodrum",
            Title = "Bodrum",
            Subtitle = "Marina, plaj kulüpleri ve yaz kaçamağı",
            SearchQuery = "Bodrum",
            HotelsUrl = "/hotel?q=Bodrum",
            ThemeClass = "plan-theme-bodrum",
            IconClass = "fas fa-ship",
            TypicalNights = 3,
            MinNightlyTry = 3200m
        }
    ];

    private readonly ICampaignService _campaignService;

    public SeyahatPlanlamaService(ICampaignService campaignService)
    {
        _campaignService = campaignService;
    }

    public async Task<SeyahatPlanlamaPageViewModel> BuildPageAsync(
        string? destinationKey,
        int? nights,
        decimal? budgetTry,
        bool budgetSubmitted,
        bool isAuthenticated,
        CancellationToken cancellationToken = default)
    {
        var model = new SeyahatPlanlamaPageViewModel
        {
            IsAuthenticated = isAuthenticated,
            RouteSuggestions = CuratedRoutes.ToList(),
            WeekendEscapes = BuildWeekendEscapes(),
            MetaDescription = "Rota önerileri, bütçe tahmini, hafta sonu rotaları ve kampanya bazlı otel önerileri ile seyahatinizi planlayın."
        };

        var listing = await _campaignService.GetCampaignListingPageAsync(cancellationToken: cancellationToken);
        model.CampaignSuggestions = listing.Campaigns
            .OrderByDescending(c => c.IsFeatured)
            .ThenByDescending(c => c.HotelCount)
            .Take(3)
            .ToList();

        var destKey = NormalizeDestinationKey(destinationKey);
        var nightCount = Math.Clamp(nights ?? 2, 1, 14);
        var budget = Math.Max(500m, budgetTry ?? 6000m);

        model.BudgetForm = new TravelBudgetFormViewModel
        {
            DestinationKey = destKey,
            Nights = nightCount,
            BudgetTry = budget,
            Submitted = budgetSubmitted,
            SearchUrl = BuildHotelsSearchUrl(destKey, nightCount, budget, fitsBudget: true)
        };

        if (budgetSubmitted)
        {
            model.BudgetEstimate = BuildBudgetEstimate(destKey, nightCount, budget);
            model.BudgetForm.SearchUrl = model.BudgetEstimate.HotelsUrl;
        }

        return model;
    }

    private static List<TravelWeekendCardViewModel> BuildWeekendEscapes() =>
    [
        new()
        {
            Title = "İstanbul hafta sonu",
            Description = "Şehir otelleri ve erken çıkış fırsatları",
            Url = "/hotel?q=%C4%B0stanbul&etiket=hafta-sonu-firsatlari",
            BadgeText = "Cuma–Pazar",
            ThemeClass = "plan-weekend-city"
        },
        new()
        {
            Title = "Antalya kaçamağı",
            Description = "Sahil otelleri ve aile paketleri",
            Url = "/hotel?q=Antalya&etiket=hafta-sonu-firsatlari",
            BadgeText = "2 gece+",
            ThemeClass = "plan-weekend-sea"
        },
        new()
        {
            Title = "Tüm hafta sonu fırsatları",
            Description = "Platform genelinde etiketli fırsatlar",
            Url = "/hafta-sonu-firsatlari",
            BadgeText = "Tümü",
            ThemeClass = "plan-weekend-all"
        }
    ];

    private static TravelBudgetEstimateViewModel BuildBudgetEstimate(string destKey, int nights, decimal budgetTry)
    {
        var dest = CuratedRoutes.FirstOrDefault(d => d.Key == destKey) ?? CuratedRoutes[0];
        var minTotal = decimal.Round(dest.MinNightlyTry * nights, 0);
        var maxTotal = decimal.Round(minTotal * 1.38m, 0);
        var fits = budgetTry >= minTotal * 0.92m;
        var summary = fits
            ? $"{dest.Title} için {nights} gece tahmini konaklama: {FormatTry(minTotal)} – {FormatTry(maxTotal)} (vergiler dahil başlangıç fiyatlarına göre)."
            : $"{dest.Title} için {nights} gece minimum yaklaşık {FormatTry(minTotal)}; bütçenize uygun otelleri filtreleyerek listeleyebilirsiniz.";

        return new TravelBudgetEstimateViewModel
        {
            DestinationLabel = dest.Title,
            Nights = nights,
            BudgetTry = budgetTry,
            EstimatedMinTotalTry = minTotal,
            EstimatedMaxTotalTry = maxTotal,
            MinNightlyTry = dest.MinNightlyTry,
            FitsBudget = fits,
            SummaryText = summary,
            HotelsUrl = BuildHotelsSearchUrl(destKey, nights, budgetTry, fits)
        };
    }

    private static string BuildHotelsSearchUrl(string destKey, int nights, decimal budgetTry, bool fitsBudget)
    {
        var dest = CuratedRoutes.FirstOrDefault(d => d.Key == destKey) ?? CuratedRoutes[0];
        var q = Uri.EscapeDataString(dest.SearchQuery);
        var url = $"/hotel?q={q}";
        if (!fitsBudget || budgetTry < dest.MinNightlyTry * Math.Max(1, nights) * 0.95m)
        {
            url += "&etiket=butceme-uygun-oteller";
        }

        return url;
    }

    private static string NormalizeDestinationKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "istanbul";
        var normalized = key.Trim().ToLowerInvariant();
        return CuratedRoutes.Any(d => d.Key == normalized) ? normalized : "istanbul";
    }

    private static string FormatTry(decimal amount)
        => amount.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")) + " TRY";
}
