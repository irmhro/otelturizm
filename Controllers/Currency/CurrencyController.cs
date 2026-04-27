using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Currency;

[Route("currency")]
public sealed class CurrencyController : Controller
{
    private readonly IUserPreferenceService _prefs;

    public CurrencyController(IUserPreferenceService prefs)
    {
        _prefs = prefs;
    }

    [HttpGet("set")]
    public async Task<IActionResult> Set([FromQuery] string? code, [FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var normalized = UserPreferenceService.NormalizeCurrency(code);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "TRY";
        }

        Response.Cookies.Append(
            UserPreferenceService.CurrencyCookieName,
            normalized,
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
                await _prefs.TryPersistCurrencyAsync(userId, normalized, cancellationToken);
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
}

