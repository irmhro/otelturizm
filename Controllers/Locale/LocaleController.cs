using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

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

        if (User?.Identity?.IsAuthenticated == true)
        {
            var userId = TryGetUserId();
            if (userId > 0)
            {
                await _prefs.TryPersistLocaleAsync(userId, normalized, cancellationToken);
            }
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // fallback: previous page or home
        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
        {
            var local = uri.PathAndQuery;
            if (Url.IsLocalUrl(local))
            {
                return Redirect(local);
            }
        }

        return Redirect("/");
    }

    private long TryGetUserId()
    {
        var idClaim = User.Claims.FirstOrDefault(x => x.Type is "id" or "userId" or "sub")?.Value;
        return long.TryParse(idClaim, out var id) ? id : 0;
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
            _ => "tr-TR"
        };
    }
}

