using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace otelturizmnew.Services;

/// <summary>Path-prefix uluslararası SEO meta ve hreflang (H9 Faz 1, T446/T449).</summary>
public sealed class InternationalSeoService
{
    private static readonly string[] HreflangLocales =
    [
        "tr-TR", "en-US", "en-GB", "de-DE", "fr-FR", "es-ES", "ru-RU"
    ];

    private static readonly Dictionary<string, string> LocaleToCulture = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tr-TR"] = "tr",
        ["en-US"] = "en",
        ["en-GB"] = "en",
        ["de-DE"] = "de",
        ["fr-FR"] = "fr",
        ["es-ES"] = "es",
        ["ru-RU"] = "ru"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly Lazy<IReadOnlyDictionary<string, string>> _enDistrictMeta;

    public InternationalSeoService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _enDistrictMeta = new Lazy<IReadOnlyDictionary<string, string>>(LoadEnDistrictMeta);
    }

    public sealed record ListingMeta(string Title, string Description);

    public sealed record DetailMeta(string Title, string Description);

    public sealed record HreflangAlternate(string Hreflang, string Href);

    public static IReadOnlyList<string> SupportedHreflangLocales => HreflangLocales;

    public ListingMeta BuildListingMeta(string culture, string? city, int? hotelCount, int page = 1)
    {
        var c = NormalizeCulture(culture);
        var cityLabel = FormatCityLabel(city, c);
        var countSuffix = hotelCount is > 0 ? $" ({hotelCount})" : string.Empty;
        _ = page;

        return c switch
        {
            "en" => new ListingMeta(
                string.IsNullOrWhiteSpace(cityLabel)
                    ? $"Hotels in Turkey{countSuffix} | Otelturizm"
                    : $"{cityLabel} Hotels{countSuffix} | Otelturizm",
                string.IsNullOrWhiteSpace(cityLabel)
                    ? "Compare hotels, campaigns and secure booking on Otelturizm."
                    : BuildEnListingDescription(city, cityLabel, hotelCount)),
            "de" => new ListingMeta(
                string.IsNullOrWhiteSpace(cityLabel)
                    ? $"Hotels in der Türkei{countSuffix} | Otelturizm"
                    : $"Hotels in {cityLabel}{countSuffix} | Otelturizm",
                string.IsNullOrWhiteSpace(cityLabel)
                    ? "Hotels vergleichen und sicher auf Otelturizm buchen."
                    : $"Hotels in {cityLabel} vergleichen und auf Otelturizm buchen."),
            "fr" => new ListingMeta(
                string.IsNullOrWhiteSpace(cityLabel)
                    ? $"Hôtels en Turquie{countSuffix} | Otelturizm"
                    : $"Hôtels à {cityLabel}{countSuffix} | Otelturizm",
                $"Réservez des hôtels à {cityLabel} sur Otelturizm."),
            "es" => new ListingMeta(
                string.IsNullOrWhiteSpace(cityLabel)
                    ? $"Hoteles en Turquía{countSuffix} | Otelturizm"
                    : $"Hoteles en {cityLabel}{countSuffix} | Otelturizm",
                $"Reserva hoteles en {cityLabel} con Otelturizm."),
            "ru" => new ListingMeta(
                string.IsNullOrWhiteSpace(cityLabel)
                    ? $"Отели Турции{countSuffix} | Otelturizm"
                    : $"Отели {cityLabel}{countSuffix} | Otelturizm",
                $"Сравните отели в {cityLabel} и забронируйте на Otelturizm."),
            _ => new ListingMeta(
                string.IsNullOrWhiteSpace(cityLabel)
                    ? $"Oteller{countSuffix} | Otelturizm"
                    : $"{cityLabel} Otelleri{countSuffix} | Otelturizm",
                string.IsNullOrWhiteSpace(cityLabel)
                    ? "Otelturizm ile otelleri karşılaştırın, kampanyaları keşfedin ve güvenle rezervasyon oluşturun."
                    : $"{cityLabel} otellerini karşılaştırın ve güvenle rezervasyon yapın.")
        };
    }

    public DetailMeta BuildHotelDetailMeta(string culture, string hotelName, string? city)
    {
        var c = NormalizeCulture(culture);
        var cityLabel = FormatCityLabel(city, c);
        var hotel = string.IsNullOrWhiteSpace(hotelName) ? "Hotel" : hotelName.Trim();

        return c switch
        {
            "en" => new DetailMeta(
                string.IsNullOrWhiteSpace(cityLabel) ? $"{hotel} | Otelturizm" : $"{hotel} — {cityLabel} — Book now",
                $"Book {hotel} online. Photos, amenities and secure reservation on Otelturizm."),
            "de" => new DetailMeta(
                string.IsNullOrWhiteSpace(cityLabel) ? $"{hotel} | Otelturizm" : $"{hotel} — {cityLabel} — Jetzt buchen",
                $"{hotel} online buchen — Otelturizm."),
            _ => new DetailMeta(
                string.IsNullOrWhiteSpace(cityLabel) ? $"{hotel} | Otelturizm" : $"{hotel} — {cityLabel}",
                $"{hotel} için fiyat, fotoğraf ve güvenli rezervasyon — Otelturizm.")
        };
    }

    public IReadOnlyList<HreflangAlternate> BuildHreflangAlternates(
        string publicBaseUrl,
        string pageKind,
        string? citySlug,
        string? hotelSlug,
        IQueryCollection? query = null)
    {
        var baseUrl = publicBaseUrl.TrimEnd('/');
        var alternates = new List<HreflangAlternate>(HreflangLocales.Length + 1);

        foreach (var locale in HreflangLocales)
        {
            if (!LocaleToCulture.TryGetValue(locale, out var culture))
            {
                continue;
            }

            var path = InternationalSeoPaths.BuildPublicPath(culture, pageKind, citySlug, hotelSlug);
            var href = baseUrl + path;
            if (query is not null && ShouldPreserveQuery(pageKind, query))
            {
                href += BuildCanonicalQuerySuffix(query);
            }

            alternates.Add(new HreflangAlternate(locale, href));
        }

        var xDefaultPath = InternationalSeoPaths.BuildPublicPath("tr", pageKind, citySlug, hotelSlug);
        alternates.Add(new HreflangAlternate("x-default", baseUrl + xDefaultPath));
        return alternates;
    }

    public IReadOnlyList<HreflangAlternate> BuildHreflangAlternatesFromRequest(
        string publicBaseUrl,
        PathString requestPath,
        IQueryCollection? query = null)
    {
        var path = requestPath.HasValue ? requestPath.Value! : "/";
        var (pageKind, citySlug, hotelSlug) = InternationalSeoPaths.ParseListingPath(path);
        return BuildHreflangAlternates(publicBaseUrl, pageKind, citySlug, hotelSlug, query);
    }

    public string? TryGetEnDistrictDescription(string? districtSlug)
    {
        if (string.IsNullOrWhiteSpace(districtSlug))
        {
            return null;
        }

        var key = districtSlug.Trim().Trim('/').ToLowerInvariant();
        return _enDistrictMeta.Value.TryGetValue(key, out var text) ? text : null;
    }

    public string ResolveCultureFromPath(PathString path)
        => InternationalSeoPaths.ResolveCultureFromPath(path.HasValue ? path.Value! : "/");

    public CultureInfo ResolveRequestCulture(string culture)
    {
        var c = NormalizeCulture(culture);
        return c switch
        {
            "en" => CultureInfo.GetCultureInfo("en-US"),
            "de" => CultureInfo.GetCultureInfo("de-DE"),
            "fr" => CultureInfo.GetCultureInfo("fr-FR"),
            "es" => CultureInfo.GetCultureInfo("es-ES"),
            "ru" => CultureInfo.GetCultureInfo("ru-RU"),
            _ => CultureInfo.GetCultureInfo("tr-TR")
        };
    }

    public string LocalizeAbsoluteUrl(string absoluteUrl, string targetCulture)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return absoluteUrl;
        }

        var localizedPath = InternationalSeoPaths.LocalizePath(uri.AbsolutePath, targetCulture);
        return uri.GetLeftPart(UriPartial.Authority) + localizedPath;
    }

    private string BuildEnListingDescription(string? citySlug, string cityLabel, int? hotelCount)
    {
        var district = TryGetEnDistrictDescription(citySlug);
        if (!string.IsNullOrWhiteSpace(district))
        {
            return district;
        }

        var countText = hotelCount is > 0 ? $" Browse {hotelCount} properties." : string.Empty;
        return $"Find and book hotels in {cityLabel}, Turkey.{countText} Secure booking on Otelturizm.";
    }

    private static bool ShouldPreserveQuery(string pageKind, IQueryCollection query)
    {
        if (!string.Equals(pageKind, InternationalSeoPaths.PageKindListing, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return query.ContainsKey("etiket") || query.ContainsKey("kampanya");
    }

    private static string BuildCanonicalQuerySuffix(IQueryCollection query)
    {
        var parts = new List<string>();
        if (query.TryGetValue("etiket", out var etiket) && !string.IsNullOrWhiteSpace(etiket))
        {
            parts.Add("etiket=" + Uri.EscapeDataString(etiket.ToString()));
        }
        else if (query.TryGetValue("kampanya", out var kampanya) && !string.IsNullOrWhiteSpace(kampanya))
        {
            parts.Add("kampanya=" + Uri.EscapeDataString(kampanya.ToString()));
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    private IReadOnlyDictionary<string, string> LoadEnDistrictMeta()
    {
        try
        {
            var path = Path.Combine(_environment.ContentRootPath, "Docs", "seo", "en-istanbul-districts-meta.json");
            if (!File.Exists(path))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(path);
            var items = JsonSerializer.Deserialize<List<DistrictMetaRow>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (items is null || items.Count == 0)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return items
                .Where(x => !string.IsNullOrWhiteSpace(x.Slug) && !string.IsNullOrWhiteSpace(x.Description))
                .ToDictionary(
                    x => x.Slug!.Trim().Trim('/').ToLowerInvariant(),
                    x => x.Description!.Trim(),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return "tr";
        }

        var two = culture.Length >= 2 ? culture[..2].ToLowerInvariant() : culture.ToLowerInvariant();
        return two is "en" or "de" or "fr" or "es" or "ru" or "tr" ? two : "tr";
    }

    private static string FormatCityLabel(string? city, string culture)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return string.Empty;
        }

        var trimmed = city.Trim();
        if ((culture == "en" || culture == "de") && trimmed.Equals("istanbul", StringComparison.OrdinalIgnoreCase))
        {
            return "Istanbul";
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.Replace('-', ' '));
    }

    private sealed class DistrictMetaRow
    {
        public string? Slug { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
