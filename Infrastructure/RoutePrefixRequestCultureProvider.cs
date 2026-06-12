using Microsoft.AspNetCore.Localization;
using otelturizmnew.Services;

namespace otelturizmnew.Infrastructure;

/// <summary>
/// Path prefix is the source of truth for public SEO routes.
/// Turkish canonical paths (/hotel, /kampanyalar, /) are always tr-TR.
/// Cookie, Accept-Language and ?lang= are ignored (no silent locale drift).
/// </summary>
public sealed class RoutePrefixRequestCultureProvider : IRequestCultureProvider
{
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
            return Task.FromResult<ProviderCultureResult?>(ToResult(pathCulture));
        }
        catch
        {
            return Task.FromResult<ProviderCultureResult?>(ToResult("tr"));
        }
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
