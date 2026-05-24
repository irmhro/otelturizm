using Microsoft.AspNetCore.Localization;
using otelturizmnew.Services;

namespace otelturizmnew.Infrastructure;

/// <summary>
/// Path prefix is the source of truth for public SEO routes.
/// Turkish canonical paths (/oteller, /kampanyalar, /) stay tr-TR unless ?lang= is present.
/// Cookie and Accept-Language are ignored (no silent ar/en drift).
/// </summary>
public sealed class RoutePrefixRequestCultureProvider : IRequestCultureProvider
{
    private const string LangQueryKey = "lang";

    public Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        try
        {
            if (httpContext?.Request is null)
            {
                return Task.FromResult<ProviderCultureResult?>(ToResult("tr"));
            }

            var path = httpContext.Request.Path.Value;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = "/";
            }

            var pathCulture = InternationalSeoPaths.ResolveCultureFromPath(path);

            if (!string.Equals(pathCulture, "tr", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<ProviderCultureResult?>(ToResult(pathCulture));
            }

            if (TryGetQueryCulture(httpContext, out var queryCulture))
            {
                return Task.FromResult<ProviderCultureResult?>(ToResult(queryCulture));
            }

            return Task.FromResult<ProviderCultureResult?>(ToResult("tr"));
        }
        catch
        {
            return Task.FromResult<ProviderCultureResult?>(ToResult("tr"));
        }
    }

    private static bool TryGetQueryCulture(HttpContext httpContext, out string cultureCode)
    {
        cultureCode = "tr";
        if (!httpContext.Request.Query.TryGetValue(LangQueryKey, out var values))
        {
            return false;
        }

        var raw = values.ToString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var two = raw.Length >= 2 ? raw[..2].ToLowerInvariant() : raw.ToLowerInvariant();
        cultureCode = two switch
        {
            "en" or "de" or "fr" or "es" or "ru" or "tr" => two,
            _ => "tr"
        };
        return true;
    }

    private static ProviderCultureResult ToResult(string cultureCode)
    {
        var culture = cultureCode switch
        {
            "en" => "en-US",
            "de" => "de-DE",
            "fr" => "fr-FR",
            "es" => "es-ES",
            "ru" => "ru-RU",
            _ => "tr-TR"
        };

        return new ProviderCultureResult(culture, culture);
    }
}
