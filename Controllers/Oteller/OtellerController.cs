using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using otelturizmnew.Constants;
using otelturizmnew.Models.Reservations;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Oteller;

[Route("oteller")]
public class OtellerController : Controller
{
    private readonly IHotelService _hotelService;
    private readonly IPublicReservationService _publicReservationService;
    private readonly IWeatherService _weatherService;
    private readonly IUserFavoriteService _userFavoriteService;

    public OtellerController(IHotelService hotelService, IPublicReservationService publicReservationService, IWeatherService weatherService, IUserFavoriteService userFavoriteService)
    {
        _hotelService = hotelService;
        _publicReservationService = publicReservationService;
        _weatherService = weatherService;
        _userFavoriteService = userFavoriteService;
    }

    [HttpGet("")]
    [HttpGet("istanbul")]
    public async Task<IActionResult> OtelListeleme([FromQuery] string? q, [FromQuery] string? city, [FromQuery] string? etiket, CancellationToken cancellationToken)
    {
        ViewData["PageCss"] = "otel-listeleme";
        var searchTerm = !string.IsNullOrWhiteSpace(q) ? q : city;
        var model = await _hotelService.GetHotelListingPageAsync(searchTerm, etiket, cancellationToken);
        await ApplyFavoriteStatesAsync(model, cancellationToken);
        ViewData["Title"] = model.CampaignTitle;
        return View("~/Views/Oteller/OtelListeleme.cshtml", model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> OtelDetay(string slug, CancellationToken cancellationToken)
    {
        var model = await _hotelService.GetHotelDetailPageAsync(slug, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        model.Weather = await _weatherService.GetForecastAsync(model.District, model.City, model.Latitude, model.Longitude, cancellationToken);
        await ApplyFavoriteStateAsync(model, cancellationToken);
        model.ReservationForm = new PublicHotelReservationForm
        {
            HotelId = model.Id,
            RoomTypeId = model.Rooms.FirstOrDefault()?.RoomTypeId ?? 0
        };
        model.ActiveDraft = await _publicReservationService.GetActiveDraftAsync(GetCurrentUserIdOrNull(), GetCurrentReservationSessionKey(), cancellationToken);

        ViewData["Title"] = "Otel Detay";
        ViewData["PageCss"] = "otel-detay";
        return View("~/Views/Oteller/OtelDetay.cshtml", model);
    }

    [HttpPost("{slug}/rezervasyon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartReservation(string slug, PublicHotelReservationForm form, CancellationToken cancellationToken)
    {
        var result = await _publicReservationService.StartReservationAsync(GetCurrentUserIdOrNull(), EnsureReservationSessionKey(), form, cancellationToken);
        TempData[result.Success ? "PublicReservationSuccess" : "PublicReservationInfo"] = result.Message;
        if (!string.IsNullOrWhiteSpace(result.RedirectUrl))
        {
            return Redirect(result.RedirectUrl);
        }

        return Redirect($"/oteller/{slug}");
    }

    private async Task ApplyFavoriteStatesAsync(otelturizmnew.Models.Oteller.HotelListingPageViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return;
        }

        var favoriteIds = await _userFavoriteService.GetFavoriteHotelIdsAsync(userId, model.Hotels.Select(x => x.Id), cancellationToken);
        foreach (var hotel in model.Hotels)
        {
            hotel.IsFavorite = favoriteIds.Contains(hotel.Id);
        }
    }

    private async Task ApplyFavoriteStateAsync(otelturizmnew.Models.Oteller.HotelDetailPageViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return;
        }

        var favoriteIds = await _userFavoriteService.GetFavoriteHotelIdsAsync(userId, new[] { model.Id }, cancellationToken);
        model.IsFavorite = favoriteIds.Contains(model.Id);
    }

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }

    private long? GetCurrentUserIdOrNull()
    {
        var userId = GetCurrentUserId();
        return userId > 0 ? userId : null;
    }

    private string? GetCurrentReservationSessionKey()
        => Request.Cookies.TryGetValue(ReservationDraftService.DraftCookieName, out var key) ? key : null;

    private string EnsureReservationSessionKey()
    {
        if (Request.Cookies.TryGetValue(ReservationDraftService.DraftCookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var key = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(ReservationDraftService.DraftCookieName, key, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddDays(90)
        });
        return key;
    }
}

