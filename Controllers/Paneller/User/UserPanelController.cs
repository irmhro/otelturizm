using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using otelturizmnew.Constants;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.User;

[Authorize]
[Route("panel/user")]
public class UserPanelController : Controller
{
    private readonly IUserFavoriteService _userFavoriteService;

    public UserPanelController(IUserFavoriteService userFavoriteService)
    {
        _userFavoriteService = userFavoriteService;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public IActionResult Index()
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        ViewData["PageCss"] = "panel-user-dashboard";
        ViewData["PanelTitle"] = "Kullanici Paneli";
        ViewData["PanelSubtitle"] = "Hesabini yonet, rezervasyonlarini takip et ve ozel firsatlari kesfet.";
        return View("~/Views/Paneller/User/Index.cshtml");
    }

    [HttpGet("rezervasyonlarim")]
    public IActionResult Reservations()
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        ViewData["PageCss"] = "panel-user-reservations";
        ViewData["PanelTitle"] = "Rezervasyonlarim";
        ViewData["PanelSubtitle"] = "Tum yaklasan, gecmis ve iptal edilen rezervasyonlarini yonet.";
        return View("~/Views/Paneller/User/Reservations.cshtml");
    }

    [HttpGet("favorilerim")]
    public async Task<IActionResult> Favorites(CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userFavoriteService.GetFavoritesPageAsync(GetCurrentUserId(), cancellationToken);
        ViewData["PageCss"] = "panel-user-favorites";
        ViewData["PanelTitle"] = "Favorilerim";
        ViewData["PanelSubtitle"] = "Kaydettigin otelleri karsilastir, duzenle ve rezervasyona donustur.";
        ViewData["FavoriteCount"] = model.FavoriteCount;
        return View("~/Views/Paneller/User/Favorites.cshtml", model);
    }

    [HttpGet("otelpuan-programi")]
    [HttpGet("puanlarim")]
    public IActionResult Loyalty()
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        ViewData["PageCss"] = "panel-user-loyalty";
        ViewData["PanelTitle"] = "Puanlarim";
        ViewData["PanelSubtitle"] = "OtelPuan bakiyeni, uye seviyeni ve kullanabilecegin odulleri tek ekranda yonet.";
        return View("~/Views/Paneller/User/Loyalty.cshtml");
    }

    [HttpGet("mesajlarim")]
    public IActionResult Messages()
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        ViewData["PageCss"] = "panel-user-messages";
        ViewData["PanelTitle"] = "Mesajlarim";
        ViewData["PanelSubtitle"] = "Oteller ve destek ekipleri ile tum mesajlasma akislarini yonet.";
        return View("~/Views/Paneller/User/Messages.cshtml");
    }

    [HttpGet("profil-bilgilerim")]
    public IActionResult Profile()
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        ViewData["PageCss"] = "panel-user-profile";
        ViewData["PanelTitle"] = "Profil Bilgilerim";
        ViewData["PanelSubtitle"] = "Kisisel bilgilerini, iletisim verilerini ve seyahat tercihlerini duzenle.";
        return View("~/Views/Paneller/User/Profile.cshtml");
    }

    [HttpGet("odeme-yontemleri")]
    public IActionResult PaymentMethods()
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        ViewData["PageCss"] = "panel-user-payment";
        ViewData["PanelTitle"] = "Odeme Yontemleri";
        ViewData["PanelSubtitle"] = "Kayitli kartlarini, fatura bilgilerini ve odeme guvenligini yonet.";
        return View("~/Views/Paneller/User/PaymentMethods.cshtml");
    }

    [HttpGet("bildirim-tercihleri")]
    public IActionResult Notifications()
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        ViewData["PageCss"] = "panel-user-notifications";
        ViewData["PanelTitle"] = "Bildirim Tercihleri";
        ViewData["PanelSubtitle"] = "E-posta, SMS ve uygulama ici bildirim tercihlerini ozellestir.";
        return View("~/Views/Paneller/User/Notifications.cshtml");
    }

    [HttpGet("guvenlik-ve-giris")]
    public IActionResult Security()
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        ViewData["PageCss"] = "panel-user-security";
        ViewData["PanelTitle"] = "Guvenlik ve Giris";
        ViewData["PanelSubtitle"] = "Sifre, aktif oturumlar ve iki asamali dogrulama ayarlarini yonet.";
        return View("~/Views/Paneller/User/Security.cshtml");
    }

    private bool CanAccessUserPanel()
    {
        var accountType = User.FindFirst(AuthClaimTypes.AccountType)?.Value;
        return string.Equals(accountType, "user", StringComparison.OrdinalIgnoreCase);
    }

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }
}



