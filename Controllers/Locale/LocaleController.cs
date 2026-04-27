using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace otelturizmnew.Controllers.Locale;

[Route("locale")]
public class LocaleController : Controller
{
    [HttpGet("set")]
    public IActionResult Set([FromQuery] string? lang, [FromQuery] string? returnUrl = null)
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

