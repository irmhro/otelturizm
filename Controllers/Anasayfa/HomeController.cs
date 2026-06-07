using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text.Json;
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
    private readonly IDawnSurpriseService _dawnSurpriseService;
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;

    public HomeController(
        IHotelService hotelService,
        IUserFavoriteService userFavoriteService,
        IDeadLinkRedirectService deadLinkRedirectService,
        IDawnSurpriseService dawnSurpriseService,
        ILogger<HomeController> logger,
        IConfiguration configuration)
    {
        _hotelService = hotelService;
        _userFavoriteService = userFavoriteService;
        _deadLinkRedirectService = deadLinkRedirectService;
        _dawnSurpriseService = dawnSurpriseService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["PageCss"] = "home-index";
        ViewData["Title"] = "Oteller, kampanyalar ve güvenli rezervasyon";
        ViewData["MetaDescription"] =
            "Otelturizm ile yüzlerce oteli karşılaştırın, kampanyalı fiyatları görün ve onaylı rezervasyon oluşturun. Çok otelli yapı, güvenli ödeme ve şeffaf iptal koşulları.";
        var publicBase = (_configuration["App:PublicBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        ViewData["Canonical"] = publicBase + "/";
        ViewData["OgImage"] = publicBase + "/uploads/logo/logo.png";
        var jsonLd = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = "Otelturizm",
            ["url"] = publicBase,
            ["potentialAction"] = new Dictionary<string, object?>
            {
                ["@type"] = "SearchAction",
                ["target"] = $"{publicBase}/oteller?q={{search_term_string}}",
                ["query-input"] = "required name=search_term_string"
            }
        };
        ViewData["HomeJsonLd"] = JsonSerializer.Serialize(jsonLd);

        var model = await _hotelService.GetHomepageAsync(cancellationToken);
        await ApplyFavoriteStatesAsync(model, cancellationToken);

        return View("~/Views/Anasayfa/Anasayfa.cshtml", model);
    }

    [HttpGet("/api/dawn-surprise/status")]
    [IgnoreAntiforgeryToken]
    public IActionResult DawnSurpriseStatus()
    {
        var eligible = _dawnSurpriseService.IsEligible(HttpContext);
        if (!eligible)
        {
            return Json(new { active = false, eligible = false });
        }

        var state = _dawnSurpriseService.GetActive(HttpContext);
        if (state is null)
        {
            return Json(new { active = false, eligible = true });
        }

        return Json(new
        {
            active = true,
            eligible = true,
            percent = state.Percent,
            remainingSeconds = state.RemainingSeconds
        });
    }

    [HttpPost("/api/dawn-surprise/open")]
    [IgnoreAntiforgeryToken]
    public IActionResult DawnSurpriseOpen()
    {
        if (!_dawnSurpriseService.IsEligible(HttpContext))
        {
            return Json(new { success = false, message = "Bu kampanya yalnizca Turkiye kullanicilari icindir." });
        }

        var result = _dawnSurpriseService.Open(HttpContext);
        if (result is null)
        {
            return Json(new { success = false, message = "Kutu acilamadi. Lutfen tekrar deneyin." });
        }

        return Json(new
        {
            success = true,
            percent = result.Percent,
            isNew = result.IsNew,
            remainingSeconds = result.RemainingSeconds
        });
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


