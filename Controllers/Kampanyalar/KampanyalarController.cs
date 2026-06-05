using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using otelturizmnew.Constants;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Kampanyalar;

[Route("kampanyalar")]
public class KampanyalarController : Controller
{
    private readonly ICampaignService _campaignService;
    private readonly IUserFavoriteService _userFavoriteService;

    public KampanyalarController(ICampaignService campaignService, IUserFavoriteService userFavoriteService)
    {
        _campaignService = campaignService;
        _userFavoriteService = userFavoriteService;
    }

    [HttpGet("")]
    [OutputCache(PolicyName = "public-medium")]
    public async Task<IActionResult> Index([FromQuery] string? preset, CancellationToken cancellationToken)
    {
        var model = await _campaignService.GetCampaignListingPageAsync(preset, cancellationToken);
        ViewData["Title"] = string.IsNullOrWhiteSpace(preset) ? "Kampanyalar" : $"Kampanyalar · {preset}";
        ViewData["IncludeAnasayfaStyles"] = true;
        ViewData["PageCss"] = "kampanyalar";
        return View("~/Views/Kampanyalar/Index.cshtml", model);
    }

    [HttpGet("{slug}")]
    [OutputCache(PolicyName = "public-short")]
    public async Task<IActionResult> Detail(
        string slug,
        [FromQuery] string? q,
        [FromQuery] string? city,
        [FromQuery] string? district,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        CancellationToken cancellationToken)
    {
        var model = await _campaignService.GetCampaignDetailPageAsync(slug, q, city, district, minPrice, maxPrice, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        await ApplyFavoriteStatesAsync(model, cancellationToken);

        ViewData["Title"] = model.CampaignName;
        ViewData["PageCss"] = "kampanya-detay";
        return View("~/Views/Kampanyalar/Detail.cshtml", model);
    }

    private async Task ApplyFavoriteStatesAsync(otelturizmnew.Models.Kampanyalar.CampaignDetailPageViewModel model, CancellationToken cancellationToken)
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

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }
}
