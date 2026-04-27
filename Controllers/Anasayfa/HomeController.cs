using Microsoft.AspNetCore.Diagnostics;
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
    private readonly IDeadLinkRedirectService _deadLinkRedirectService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IHotelService hotelService,
        IUserFavoriteService userFavoriteService,
        IDeadLinkRedirectService deadLinkRedirectService,
        ILogger<HomeController> logger)
    {
        _hotelService = hotelService;
        _userFavoriteService = userFavoriteService;
        _deadLinkRedirectService = deadLinkRedirectService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["PageCss"] = "home-index";
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

    [HttpGet("/status/{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus([FromQuery] int code)
    {
        if (code == 404)
        {
            var reExec = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            var originalPath = reExec?.OriginalPath ?? Request.Path.Value ?? "/";
            if (_deadLinkRedirectService.TryResolvePermanentRedirect(originalPath) is { } target)
            {
                _logger.LogInformation("DEAD_LINK_REDIRECT from={From} to={To}", originalPath, target);
                return RedirectPermanent(target);
            }
        }

        Response.StatusCode = code;
        ViewData["Title"] = code == 404 ? "Sayfa bulunamadı" : "İşlem tamamlanamadı";
        ViewData["HeaderContext"] = "home";
        ViewData["MetaDescription"] = code == 404
            ? "Aradığınız sayfa bulunamadı. Otelleri görüntüleyebilir, kampanyaları keşfedebilir veya ana sayfaya dönebilirsiniz."
            : "İşlem tamamlanamadı. Lütfen tekrar deneyin veya ana sayfaya dönün.";
        ViewData["Canonical"] = (HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration)?["App:PublicBaseUrl"]?.TrimEnd('/') is string baseUrl && !string.IsNullOrWhiteSpace(baseUrl)
            ? baseUrl + "/status/" + code
            : "https://otelturizm.com/status/" + code;

        return View("~/Views/Shared/StatusCode.cshtml", code);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task ApplyFavoriteStatesAsync(AnasayfaViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
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


