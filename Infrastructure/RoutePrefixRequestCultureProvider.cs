using Microsoft.AspNetCore.Localization;
using otelturizmnew.Services;

namespace otelturizmnew.Infrastructure;

/// <summary>/en/hotels, /de/hotels, /fr/hotels, /es/hoteles path prefix ile UI kültürü (H9/H13).</summary>
public sealed class RoutePrefixRequestCultureProvider : IRequestCultureProvider
{
    public Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? "/";
        var cultureCode = InternationalSeoPaths.ResolveCultureFromPath(path);

        var culture = cultureCode switch
        {
            "en" => "en-US",
            "de" => "de-DE",
            "fr" => "fr-FR",
            "es" => "es-ES",
            "ru" => "ru-RU",
            "ar" => "ar-SA",
            _ => "tr-TR"
        };

        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
    }
}
