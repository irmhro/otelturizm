using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using otelturizmnew.Constants;
using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Paneller.User;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.User;

[Authorize]
[Route("panel/user")]
public class UserPanelController : Controller
{
    private readonly IUserFavoriteService _userFavoriteService;
    private readonly IUserPanelService _userPanelService;

    public UserPanelController(IUserFavoriteService userFavoriteService, IUserPanelService userPanelService)
    {
        _userFavoriteService = userFavoriteService;
        _userPanelService = userPanelService;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(
        string? reservationStatus = null,
        DateOnly? reservationStartDate = null,
        DateOnly? reservationEndDate = null,
        int reservationPage = 1,
        int reservationPageSize = 5,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetDashboardAsync(
            GetCurrentUserId(),
            reservationStatus,
            reservationStartDate,
            reservationEndDate,
            reservationPage,
            reservationPageSize,
            cancellationToken);
        ViewData["PageCss"] = "panel-user-dashboard";
        ViewData["PanelTitle"] = "Kullanici Paneli";
        ViewData["PanelSubtitle"] = "Hesabini yonet, rezervasyonlarini takip et ve ozel firsatlari kesfet.";
        ViewData["FavoriteCount"] = model.FavoriteCount;
        return View("~/Views/Paneller/User/Index.cshtml", model);
    }

    [HttpGet("rezervasyonlarim")]
    public async Task<IActionResult> Reservations(CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var userId = GetCurrentUserId();
        var model = await _userPanelService.GetReservationsAsync(userId, cancellationToken);
        ViewData["FavoriteCount"] = await _userFavoriteService.GetFavoriteCountAsync(userId, cancellationToken);
        ViewData["PageCss"] = "panel-user-reservations";
        ViewData["PanelTitle"] = "Rezervasyonlarim";
        ViewData["PanelSubtitle"] = "Tum yaklasan, gecmis ve iptal edilen rezervasyonlarini yonet.";
        return View("~/Views/Paneller/User/Reservations.cshtml", model);
    }

    [HttpPost("rezervasyonlarim/iptal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelReservation(long reservationId, string? cancellationReason, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.CancelReservationAsync(
                GetCurrentUserId(),
                reservationId,
                cancellationReason ?? string.Empty,
                cancellationToken);
            if (!result.Success
                && result.Message.Contains("Check-in tarihi gelen veya gecen rezervasyonlar panelden iptal edilemez", StringComparison.OrdinalIgnoreCase))
            {
                TempData["UserMessageError"] = result.Message;
                return RedirectToAction(nameof(Messages));
            }

            TempData[result.Success ? "UserReservationSuccess" : "UserReservationError"] = result.Message;
        }

        return RedirectToAction(nameof(Reservations));
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

    [HttpPost("favorilerim/fiyat-alarmi")]
    public async Task<IActionResult> SaveFavoritePriceAlert(UserFavoritePriceAlertForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userFavoriteService.SavePriceAlertAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "UserFavoriteAlertSuccess" : "UserFavoriteAlertError"] = result.Message;
        }

        return RedirectToAction(nameof(Favorites));
    }

    [HttpPost("favorilerim/fiyat-alarmi/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFavoritePriceAlert(long hotelId, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userFavoriteService.DeletePriceAlertAsync(GetCurrentUserId(), hotelId, cancellationToken);
            TempData[result.Success ? "UserFavoriteAlertSuccess" : "UserFavoriteAlertError"] = result.Message;
        }

        return RedirectToAction(nameof(Favorites));
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
    public async Task<IActionResult> Messages([FromQuery] long? conversationId, CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetMessagesAsync(GetCurrentUserId(), conversationId, cancellationToken);
        ViewData["PageCss"] = "panel-user-messages";
        ViewData["PanelTitle"] = "Mesajlarim";
        ViewData["PanelSubtitle"] = "Oteller ve destek ekipleri ile tum mesajlasma akislarini yonet.";
        return View("~/Views/Paneller/User/Messages.cshtml", model);
    }

    [HttpGet("profil-bilgilerim")]
    public async Task<IActionResult> Profile([FromQuery] bool openCompletion = false, [FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetProfileAsync(GetCurrentUserId(), cancellationToken);
        model.OpenCompletionModal = openCompletion;
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            model.ReturnUrl = returnUrl;
        }
        ViewData["PageCss"] = "panel-user-profile";
        ViewData["PanelTitle"] = "Profil Bilgilerim";
        ViewData["PanelSubtitle"] = "Kisisel bilgilerini, iletisim verilerini ve seyahat tercihlerini duzenle.";
        return View("~/Views/Paneller/User/Profile.cshtml", model);
    }

    [HttpGet("odeme-yontemleri")]
    public async Task<IActionResult> PaymentMethods(CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetPaymentMethodsAsync(GetCurrentUserId(), cancellationToken);
        ViewData["PageCss"] = "panel-user-payment";
        ViewData["PanelTitle"] = "Odeme Yontemleri";
        ViewData["PanelSubtitle"] = "Kayitli kartlarini, fatura bilgilerini ve odeme guvenligini yonet.";
        return View("~/Views/Paneller/User/PaymentMethods.cshtml", model);
    }

    [HttpGet("bildirim-tercihleri")]
    public async Task<IActionResult> Notifications(CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetNotificationsAsync(GetCurrentUserId(), cancellationToken);
        ViewData["PageCss"] = "panel-user-notifications";
        ViewData["PanelTitle"] = "Bildirim Tercihleri";
        ViewData["PanelSubtitle"] = "E-posta, SMS ve uygulama ici bildirim tercihlerini ozellestir.";
        return View("~/Views/Paneller/User/Notifications.cshtml", model);
    }

    [HttpGet("guvenlik-ve-giris")]
    public async Task<IActionResult> Security(CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetSecurityAsync(GetCurrentUserId(), cancellationToken);
        ViewData["PageCss"] = "panel-user-security";
        ViewData["PanelTitle"] = "Guvenlik ve Giris";
        ViewData["PanelSubtitle"] = "Sifre, aktif oturumlar ve iki asamali dogrulama ayarlarini yonet.";
        return View("~/Views/Paneller/User/Security.cshtml", model);
    }

    [HttpPost("profil-bilgilerim/kaydet")]
    public async Task<IActionResult> SaveProfile(UserProfileForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var saved = await _userPanelService.SaveProfileAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[saved ? "UserProfileSuccess" : "UserProfileError"] = saved
                ? "Profil bilgileri güncellendi."
                : "Profil bilgileri güncellenemedi.";
        }

        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("mesajlarim/gonder")]
    public async Task<IActionResult> SendMessage(MessageSendRequest form, List<IFormFile>? attachments, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.SendMessageAsync(GetCurrentUserId(), form, attachments, HttpContext, cancellationToken);
            TempData[result.Success ? "UserMessageStatus" : "UserMessageError"] = result.Message;
        }

        return RedirectToAction(nameof(Messages), new { conversationId = form.ConversationId });
    }

    [HttpPost("mesajlarim/sil")]
    public async Task<IActionResult> DeleteMessage(MessageDeleteRequest form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            TempData["UserMessageError"] = "Mesaj silme islemi devre disi. Sadece mesaj duzenleme ve yanitlama yapabilirsiniz.";
        }

        return RedirectToAction(nameof(Messages), new { conversationId = form.ConversationId });
    }

    [HttpPost("bildirim-tercihleri/kaydet")]
    public async Task<IActionResult> SaveNotifications(UserNotificationPreferencesForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            await _userPanelService.SaveNotificationsAsync(GetCurrentUserId(), form, cancellationToken);
            TempData["UserNotificationSuccess"] = "Bildirim tercihleri kaydedildi.";
        }

        return RedirectToAction(nameof(Notifications));
    }

    [HttpPost("guvenlik-ve-giris/sifre")]
    public async Task<IActionResult> ChangePassword(UserChangePasswordForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.ChangePasswordAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "UserSecuritySuccess" : "UserSecurityError"] = result.Message;
        }

        return RedirectToAction(nameof(Security));
    }

    [HttpPost("guvenlik-ve-giris/iki-asamali")]
    public async Task<IActionResult> SaveTwoFactor(UserTwoFactorForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            await _userPanelService.SaveTwoFactorAsync(GetCurrentUserId(), form, cancellationToken);
            TempData["UserSecuritySuccess"] = "Güvenlik tercihi güncellendi.";
        }

        return RedirectToAction(nameof(Security));
    }

    [HttpPost("odeme-yontemleri/kaydet")]
    public async Task<IActionResult> SavePaymentMethod(UserPaymentMethodForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var success = await _userPanelService.SavePaymentMethodAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[success ? "UserPaymentSuccess" : "UserPaymentError"] = success
                ? "Ödeme yöntemi eklendi."
                : "Ödeme yöntemi eklenemedi.";
        }

        return RedirectToAction(nameof(PaymentMethods));
    }

    [HttpPost("odeme-yontemleri/sil")]
    public async Task<IActionResult> DeletePaymentMethod(long paymentMethodId, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            await _userPanelService.DeletePaymentMethodAsync(GetCurrentUserId(), paymentMethodId, cancellationToken);
            TempData["UserPaymentSuccess"] = "Ödeme yöntemi kaldırıldı.";
        }

        return RedirectToAction(nameof(PaymentMethods));
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



