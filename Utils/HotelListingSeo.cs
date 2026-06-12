using Microsoft.AspNetCore.Http;
using otelturizmnew.Services;

namespace otelturizmnew.Utils;

/// <summary>Otel listesi canonical ve robots kuralları (T148/T305/T451).</summary>
public static class HotelListingSeo
{
    public sealed record Result(string Canonical, string? Robots);

    public static Result Build(
        string publicBaseUrl,
        PathString requestPath,
        string? normalizedSearchTerm,
        string normalizedTag,
        string normalizedCampaignSlug,
        int currentPage,
        IQueryCollection query)
        => Build(publicBaseUrl, requestPath, normalizedSearchTerm, normalizedTag, normalizedCampaignSlug, currentPage, query, culture: null);

    public static Result Build(
        string publicBaseUrl,
        PathString requestPath,
        string? normalizedSearchTerm,
        string normalizedTag,
        string normalizedCampaignSlug,
        int currentPage,
        IQueryCollection query,
        string? culture)
    {
        var path = requestPath.HasValue ? requestPath.Value! : "/hotel";
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        var seoCulture = string.IsNullOrWhiteSpace(culture)
            ? InternationalSeoPaths.ResolveCultureFromPath(path)
            : culture;

        var hasSearchQuery = query.ContainsKey("q") || query.ContainsKey("city");
        var hasLegacyFilter = query.ContainsKey("filter");
        var hasKampanyaQuery = query.ContainsKey("kampanya");
        var hasSearch = hasSearchQuery || !string.IsNullOrWhiteSpace(normalizedSearchTerm);
        var hasTag = !string.IsNullOrWhiteSpace(normalizedTag);
        var hasCampaign = !string.IsNullOrWhiteSpace(normalizedCampaignSlug);

        var canonicalQuery = new List<string>();
        if (hasTag)
        {
            canonicalQuery.Add("etiket=" + Uri.EscapeDataString(normalizedTag));
        }
        else if (hasCampaign)
        {
            canonicalQuery.Add("kampanya=" + Uri.EscapeDataString(normalizedCampaignSlug));
        }

        var noindex = currentPage > 1
                      || hasSearch
                      || hasLegacyFilter
                      || hasKampanyaQuery
                      || (hasTag && hasCampaign)
                      || (hasTag && hasSearchQuery);

        var baseUrl = publicBaseUrl.TrimEnd('/');
        var canonicalPath = string.IsNullOrWhiteSpace(normalizedSearchTerm)
            ? path
            : InternationalSeoPaths.BuildPublicPath(
                seoCulture,
                InternationalSeoPaths.PageKindListing,
                normalizedSearchTerm.Trim().ToLowerInvariant().Replace(" ", "-"),
                hotelSlug: null);
        var canonical = baseUrl + canonicalPath;
        if (canonicalQuery.Count > 0)
        {
            canonical += "?" + string.Join('&', canonicalQuery);
        }

        return new Result(canonical, noindex ? "noindex, follow" : null);
    }
}
