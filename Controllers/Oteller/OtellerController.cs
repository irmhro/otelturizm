using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using otelturizmnew.Constants;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Oteller;

[Route("oteller")]
public class OtellerController : Controller
{
    private readonly IHotelService _hotelService;
    private readonly IWeatherService _weatherService;
    private readonly IUserFavoriteService _userFavoriteService;

    public OtellerController(IHotelService hotelService, IWeatherService weatherService, IUserFavoriteService userFavoriteService)
    {
        _hotelService = hotelService;
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

        ViewData["Title"] = "Otel Detay";
        ViewData["PageCss"] = "otel-detay";
        return View("~/Views/Oteller/OtelDetay.cshtml", model);
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
}


