using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services;

namespace otelturizmnew.Controllers.Locale;

[Route("locale")]
public class LocaleController : Controller
{
    private readonly otelturizmnew.Services.Abstractions.IUserPreferenceService _prefs;

    public LocaleController(otelturizmnew.Services.Abstractions.IUserPreferenceService prefs)
    {
        _prefs = prefs;
    }

    [HttpGet("set")]
    public async Task<IActionResult> Set([FromQuery] string? lang, [FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCulture(lang);
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalized));

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue,
            new CookieOptions
            {
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/"
            });

        Response.Cookies.Append(
            "site_language_v1",
            normalized[..2].ToLowerInvariant(),
            new CookieOptions
            {
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/"
            });

        Response.Cookies.Append(
            "site_language_explicit_v1",
            "1",
            new CookieOptions
            {
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/"
            });

        if (User?.Identity?.IsAuthenticated == true)
        {
            var userId = TryGetUserId();
            if (userId > 0)
            {
                await _prefs.TryPersistLocaleAsync(userId, normalized, cancellationToken);
            }
        }

        var target = ResolveLocalizedReturnUrl(returnUrl, normalized);
        if (!string.IsNullOrWhiteSpace(target))
        {
            return Redirect(target);
        }

        return Redirect("/");
    }

    private long TryGetUserId()
    {
        var idClaim = User.Claims.FirstOrDefault(x => x.Type is "id" or "userId" or "sub")?.Value;
        return long.TryParse(idClaim, out var id) ? id : 0;
    }

    private string? ResolveLocalizedReturnUrl(string? returnUrl, string normalizedCulture)
    {
        string? local = null;
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            local = returnUrl;
        }
        else
        {
            var referer = Request.Headers.Referer.ToString();
            if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            {
                var fromReferer = uri.PathAndQuery;
                if (Url.IsLocalUrl(fromReferer))
                {
                    local = fromReferer;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(local))
        {
            return null;
        }

        if (!Uri.TryCreate(local, UriKind.Relative, out var relative))
        {
            return local;
        }

        var path = relative.ToString();
        var queryIndex = path.IndexOf('?', StringComparison.Ordinal);
        var pathOnly = queryIndex >= 0 ? path[..queryIndex] : path;
        var query = queryIndex >= 0 ? path[queryIndex..] : string.Empty;

        if (InternationalSeoPaths.HasLocalePathPrefix(pathOnly)
            || !pathOnly.StartsWith("/hotel", StringComparison.OrdinalIgnoreCase))
        {
            return local;
        }

        var lang = normalizedCulture.Length >= 2
            ? normalizedCulture[..2].ToLowerInvariant()
            : "tr";
        var localizedPath = InternationalSeoPaths.LocalizePath(pathOnly, lang);
        return localizedPath + query;
    }

    private static string NormalizeCulture(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length == 0) return "tr-TR";

        // allow short codes
        v = v.ToLowerInvariant() switch
        {
            "tr" => "tr-TR",
            "en" => "en-US",
            "fr" => "fr-FR",
            "de" => "de-DE",
            "es" => "es-ES",
            "ru" => "ru-RU",
            _ => v
        };

        // allowlist (Program.cs supported cultures)
        return v switch
        {
            "tr-tr" => "tr-TR",
            "en-us" => "en-US",
            "en-gb" => "en-GB",
            "de-de" => "de-DE",
            "fr-fr" => "fr-FR",
            "es-es" => "es-ES",
            "ru-ru" => "ru-RU",
            "ru" => "ru-RU",
            _ => "tr-TR"
        };
    }
}

