using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _campaignService.GetCampaignListingPageAsync(cancellationToken);
        ViewData["Title"] = "Kampanyalar";
        ViewData["PageCss"] = "kampanyalar";
        return View("~/Views/Kampanyalar/Index.cshtml", model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        var model = await _campaignService.GetCampaignDetailPageAsync(slug, cancellationToken);
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
        if (userId <= 0 || !string.Equals(User.FindFirstValue(AuthClaimTypes.AccountType), "user", StringComparison.OrdinalIgnoreCase))
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
