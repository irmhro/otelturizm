using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Services;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Reservations;

/// <summary>
/// Tamamlanmamis rezervasyon taslagini iptal etme (silme).
/// </summary>
[Route("rezervasyon-taslagi")]
public sealed class ReservationDraftController : Controller
{
    private readonly IReservationDraftService _draftService;

    public ReservationDraftController(IReservationDraftService draftService)
    {
        _draftService = draftService;
    }

    [HttpPost("iptal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long draftId, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (draftId <= 0)
        {
            return SafeRedirect(returnUrl);
        }

        var userId = GetCurrentUserId();
        var sessionKey = Request.Cookies.TryGetValue(ReservationDraftService.DraftCookieName, out var rawKey)
            ? rawKey
            : null;

        await _draftService.CancelDraftAsync(draftId, userId, sessionKey, cancellationToken);

        return SafeRedirect(returnUrl);
    }

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }

    private IActionResult SafeRedirect(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
