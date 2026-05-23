namespace otelturizmnew.Services;

/// <summary>Kültür prefix + otel listesi path kuralları (H9 Faz 1).</summary>
public static class InternationalSeoPaths
{
    public const string PageKindListing = "listing";
    public const string PageKindDetail = "detail";
    public const string PageKindMap = "map";

    private static readonly Dictionary<string, CultureRoute> CultureRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tr"] = new CultureRoute("", "oteller"),
        ["en"] = new CultureRoute("en", "hotels"),
        ["de"] = new CultureRoute("de", "hotels"),
        ["fr"] = new CultureRoute("fr", "hotels"),
        ["es"] = new CultureRoute("es", "hoteles"),
        ["ru"] = new CultureRoute("ru", "oteli"),
        ["ar"] = new CultureRoute("ar", "oteller")
    };

    public static string BuildPublicPath(string culture, string pageKind, string? citySlug, string? hotelSlug)
    {
        var c = NormalizeCulture(culture);
        if (!CultureRoutes.TryGetValue(c, out var route))
        {
            route = CultureRoutes["tr"];
        }

        var segment = route.ListSegment.Trim('/');
        var prefix = string.IsNullOrEmpty(route.Prefix) ? string.Empty : "/" + route.Prefix.Trim('/');
        var root = prefix + "/" + segment;

        if (string.Equals(pageKind, PageKindDetail, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(hotelSlug))
        {
            return root + "/" + NormalizeSlug(hotelSlug);
        }

        if (!string.IsNullOrWhiteSpace(citySlug))
        {
            return root + "/" + NormalizeSlug(citySlug);
        }

        return root;
    }

    public static string ResolveCultureFromPath(string path)
    {
        var segments = (path ?? "/").Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && CultureRoutes.ContainsKey(segments[0]) && segments[0] != "tr")
        {
            foreach (var kv in CultureRoutes)
            {
                if (kv.Key == "tr")
                {
                    continue;
                }

                if (string.Equals(kv.Value.ListSegment, segments[1], StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Key;
                }
            }
        }

        if (segments.Length >= 1 && string.Equals(segments[0], "oteller", StringComparison.OrdinalIgnoreCase))
        {
            return "tr";
        }

        return "tr";
    }

    public static (string PageKind, string? CitySlug, string? HotelSlug) ParseListingPath(string path)
    {
        var segments = (path ?? "/").Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return (PageKindListing, null, null);
        }

        if (segments.Length >= 2 && CultureRoutes.ContainsKey(segments[0]) && segments[0] != "tr")
        {
            if (segments.Length == 2)
            {
                return (PageKindListing, null, null);
            }

            var slug = segments[2];
            return slug.Equals("harita", StringComparison.OrdinalIgnoreCase)
                ? (PageKindMap, null, null)
                : (GuessListingOrDetail(slug), slug, slug);
        }

        if (string.Equals(segments[0], "oteller", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length == 1)
            {
                return (PageKindListing, null, null);
            }

            var slug = segments[1];
            return slug.Equals("harita", StringComparison.OrdinalIgnoreCase)
                ? (PageKindMap, null, null)
                : (GuessListingOrDetail(slug), slug, slug);
        }

        return (PageKindListing, null, null);
    }

    public static string LocalizePath(string path, string targetCulture)
    {
        var (pageKind, citySlug, hotelSlug) = ParseListingPath(path);
        return BuildPublicPath(targetCulture, pageKind, citySlug, hotelSlug);
    }

    private static string GuessListingOrDetail(string slug)
        => slug.Equals("istanbul", StringComparison.OrdinalIgnoreCase) ? PageKindListing : PageKindDetail;

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return "tr";
        }

        var two = culture.Length >= 2 ? culture[..2].ToLowerInvariant() : culture.ToLowerInvariant();
        return CultureRoutes.ContainsKey(two) ? two : "tr";
    }

    private static string NormalizeSlug(string slug) => slug.Trim().Trim('/').ToLowerInvariant();

    private sealed record CultureRoute(string Prefix, string ListSegment);
}
