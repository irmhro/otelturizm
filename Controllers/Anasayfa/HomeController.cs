using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using otelturizmnew.Constants;
using otelturizmnew.Models;
using otelturizmnew.Models.Anasayfa;
using otelturizmnew.Services.Abstractions;
using System.Diagnostics;

namespace otelturizmnew.Controllers.Anasayfa;

public class HomeController : Controller
{
    private readonly IHotelService _hotelService;
    private readonly IUserFavoriteService _userFavoriteService;

    public HomeController(IHotelService hotelService, IUserFavoriteService userFavoriteService)
    {
        _hotelService = hotelService;
        _userFavoriteService = userFavoriteService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["PageCss"] = "home-index";
        ViewData["PageCssMobile"] = "home-index.mobile";
        var model = await _hotelService.GetHomepageAsync(cancellationToken);
        await ApplyFavoriteStatesAsync(model, cancellationToken);

        return View("~/Views/Anasayfa/Anasayfa.cshtml", model);
    }

    public IActionResult Kurumsal()
    {
        return Redirect("/kurumsal");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task ApplyFavoriteStatesAsync(AnasayfaViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0 || !string.Equals(User.FindFirstValue(AuthClaimTypes.AccountType), "user", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var hotelIds = model.PopularHotels.Select(x => x.Id)
            .Concat(model.WeekendHotels.Select(x => x.Id))
            .Distinct()
            .ToArray();

        var favoriteIds = await _userFavoriteService.GetFavoriteHotelIdsAsync(userId, hotelIds, cancellationToken);
        foreach (var hotel in model.PopularHotels)
        {
            hotel.IsFavorite = favoriteIds.Contains(hotel.Id);
        }

        foreach (var hotel in model.WeekendHotels)
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


