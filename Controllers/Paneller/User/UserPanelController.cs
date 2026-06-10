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
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly ISecureFileService _secureFileService;
    private readonly IPanelThemeService _panelThemeService;

    public UserPanelController(
        IUserFavoriteService userFavoriteService,
        IUserPanelService userPanelService,
        IPhoneVerificationService phoneVerificationService,
        ISecureFileService secureFileService,
        IPanelThemeService panelThemeService)
    {
        _userFavoriteService = userFavoriteService;
        _userPanelService = userPanelService;
        _phoneVerificationService = phoneVerificationService;
        _secureFileService = secureFileService;
        _panelThemeService = panelThemeService;
    }

    [HttpGet("")]
    [HttpGet("index")]
    [HttpGet("dashboard")]
    [HttpGet("/UserPanel")]
    [HttpGet("/UserPanel/Index")]
    public async Task<IActionResult> Dashboard(
        string? reservationStatus = null,
        DateOnly? reservationStartDate = null,
        DateOnly? reservationEndDate = null,
        int reservationPage = 1,
        int reservationPageSize = 5,
        string? favoriteSort = null,
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
            favoriteSort,
            cancellationToken);
        ViewData["PageCssPath"] = "kullanici_panel_dashboard_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_dashboard_mobil";
        ViewData["PanelTitle"] = "Dashboard";
        ViewData["PanelSubtitle"] = "Rezervasyon, favori otel ve mesaj özetlerini tek ekranda takip edin.";
        ViewData["FavoriteCount"] = model.FavoriteCount;
        ViewData["ReservationCount"] = model.TotalReservationCount;
        ViewData["MessageCount"] = model.MessageCount;
        return View("~/Views/Paneller/User/Dashboard.cshtml", model);
    }

    [HttpGet("rezervasyonlarim")]
    public async Task<IActionResult> Reservations(
        string? status = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int page = 1,
        int pageSize = 5,
        string? searchTerm = null,
        string? sort = null,
        int? created = null,
        string? @ref = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (created == 1)
        {
            var successMessage = string.IsNullOrWhiteSpace(@ref)
                ? "Teşekkür ederiz! Rezervasyonunuz oluşturuldu."
                : $"Teşekkür ederiz! Rezervasyonunuz oluşturuldu. Rezervasyon numaranız: {@ref}";
            ViewData["UserReservationSuccess"] = successMessage;
        }

        var userId = GetCurrentUserId();
        var model = await _userPanelService.GetReservationsAsync(userId, status, startDate, endDate, page, pageSize, searchTerm, sort, cancellationToken);
        ViewData["FavoriteCount"] = await _userFavoriteService.GetFavoriteCountAsync(userId, cancellationToken);
        ViewData["ReservationCount"] = model.TotalCount;
        ViewData["PageCssPath"] = "kullanici_panel_reservations_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_reservations_mobil";
        ViewData["PanelTitle"] = "Rezervasyonlarım";
        ViewData["PanelSubtitle"] = "Konaklama kayıtlarını filtrele, takip et ve yönet.";
        return View("~/Views/Paneller/User/Reservations.cshtml", model);
    }

    [HttpGet("rezervasyonlarim/disa-aktar")]
    public async Task<IActionResult> ExportReservations(
        string? status = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? searchTerm = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var csv = await _userPanelService.ExportReservationsCsvAsync(
            GetCurrentUserId(),
            status,
            startDate,
            endDate,
            searchTerm,
            sort,
            cancellationToken);
        var fileName = $"rezervasyonlarim-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    [HttpPost("tema/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTheme(otelturizmnew.Models.Paneller.Partner.PanelThemeViewModel theme, CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _panelThemeService.SaveAsync("user", GetCurrentUserId(), theme, cancellationToken);
        TempData[result.Success ? "UserSuccess" : "UserError"] = result.Message;
        return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } refUrl ? refUrl : "/panel/user/dashboard");
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

    [HttpGet("rezervasyonlarim/yorum/{reservationId:long}")]
    public async Task<IActionResult> ReservationReview(long reservationId, CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var userId = GetCurrentUserId();
        var model = await _userPanelService.GetReservationReviewPageAsync(userId, reservationId, cancellationToken);
        if (model is null)
        {
            TempData["UserReservationError"] = "Bu rezervasyon için yorum formu açılamıyor (otel onayı, giriş/tamamlanma durumu veya mevcut yorum).";
            return RedirectToAction(nameof(Reservations));
        }

        ViewData["FavoriteCount"] = await _userFavoriteService.GetFavoriteCountAsync(userId, cancellationToken);
        ViewData["PageCssPath"] = "kullanici_panel_reviews_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_reviews_mobil";
        ViewData["PanelTitle"] = "Konaklama değerlendirmesi";
        ViewData["PanelSubtitle"] = "Konakladığınız tesis hakkında geri bildirim verin.";
        return View("~/Views/Paneller/User/ReservationReview.cshtml", model);
    }

    [HttpPost("rezervasyonlarim/yorum")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReservationReviewPosted([Bind(Prefix = "Form")] UserReservationReviewForm form, CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!ModelState.IsValid)
        {
            TempData["UserReservationError"] = "Formu kontrol edin.";
            return RedirectToAction(nameof(Reservations));
        }

        var result = await _userPanelService.SubmitReservationReviewAsync(GetCurrentUserId(), form, cancellationToken);
        TempData[result.Success ? "UserReviewSuccess" : "UserReservationError"] = result.Message;
        return RedirectToAction(result.Success ? nameof(Reviews) : nameof(Reservations));
    }

    [HttpGet("yorumlarim")]
    public async Task<IActionResult> Reviews(string? status = null, string? searchTerm = null, int page = 1, long? focusReservationId = null, CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var userId = GetCurrentUserId();
        if (focusReservationId is > 0 && await _userPanelService.CanUserWriteReviewForReservationAsync(userId, focusReservationId.Value, cancellationToken))
        {
            return RedirectToAction(nameof(ReservationReview), new { reservationId = focusReservationId.Value });
        }

        var model = await _userPanelService.GetReviewsAsync(userId, status, searchTerm, page, cancellationToken);
        ViewData["FavoriteCount"] = await _userFavoriteService.GetFavoriteCountAsync(userId, cancellationToken);
        ViewData["PageCssPath"] = "kullanici_panel_reviews_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_reviews_mobil";
        ViewData["PanelTitle"] = "Yorumlarım";
        ViewData["PanelSubtitle"] = "Onaylı konaklamaların için yorum yaz, 7 gün içinde düzenle veya sil.";
        return View("~/Views/Paneller/User/Reviews.cshtml", model);
    }

    [HttpPost("yorumlarim/guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReview(UserReviewUpdateForm form, string? status = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.UpdateReviewAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "UserReviewSuccess" : "UserReviewError"] = result.Message;
        }

        return RedirectToAction(nameof(Reviews), new { status, searchTerm, page = 1 });
    }

    [HttpPost("yorumlarim/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(UserReviewDeleteForm form, string? status = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.DeleteReviewAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "UserReviewSuccess" : "UserReviewError"] = result.Message;
        }

        return RedirectToAction(nameof(Reviews), new { status, searchTerm, page = 1 });
    }

    [HttpGet("favorilerim")]
    public async Task<IActionResult> Favorites(string? searchTerm = null, string? sort = null, int page = 1, CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userFavoriteService.GetFavoritesPageAsync(GetCurrentUserId(), searchTerm, sort, page, cancellationToken);
        ViewData["PageCssPath"] = "kullanici_panel_favorites_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_favorites_mobil";
        ViewData["PanelTitle"] = "Favorilerim";
        ViewData["PanelSubtitle"] = "Kaydettiğiniz otelleri karşılaştırın, düzenleyin ve rezervasyona dönüştürün.";
        ViewData["FavoriteCount"] = model.FavoriteCount;
        return View("~/Views/Paneller/User/Favorites.cshtml", model);
    }

    [HttpPost("favorilerim/fiyat-alarmi")]
    [ValidateAntiForgeryToken]
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

    [HttpPost("otelpuan-programi/fiyat-alarmi/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePriceAlertFromLoyalty(long hotelId, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userFavoriteService.DeletePriceAlertAsync(GetCurrentUserId(), hotelId, cancellationToken);
            TempData[result.Success ? "UserLoyaltySuccess" : "UserLoyaltyError"] = result.Message;
        }

        return RedirectToAction(nameof(Loyalty));
    }

    [HttpGet("otelpuan-programi")]
    [HttpGet("puanlarim")]
    public async Task<IActionResult> Loyalty(CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetLoyaltyAsync(GetCurrentUserId(), cancellationToken);
        ViewData["PageCssPath"] = "kullanici_panel_loyalty_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_loyalty_mobil";
        ViewData["PanelTitle"] = "Puanlarım";
        ViewData["PanelSubtitle"] = "OtelPuan bakiyenizi, üye seviyenizi ve kullanabileceğiniz ödülleri tek ekranda yönetin.";
        return View("~/Views/Paneller/User/Loyalty.cshtml", model);
    }

    [HttpPost("otelpuan-programi/butce-planlayici")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBudgetPlan(UserLoyaltyBudgetPlanForm form, CancellationToken cancellationToken = default)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.SaveBudgetPlanAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "UserLoyaltySuccess" : "UserLoyaltyError"] = result.Message;
        }

        return RedirectToAction(nameof(Loyalty));
    }

    [HttpPost("otelpuan-programi/seyahat-plani")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTravelPlan(UserLoyaltyTravelPlanForm form, CancellationToken cancellationToken = default)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.SaveTravelPlanAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "UserLoyaltySuccess" : "UserLoyaltyError"] = result.Message;
        }

        return RedirectToAction(nameof(Loyalty));
    }

    [HttpPost("puanlarim/odul-kullan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedeemReward(UserLoyaltyRedeemForm form, CancellationToken cancellationToken = default)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.RedeemRewardAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "UserLoyaltySuccess" : "UserLoyaltyError"] = result.Message;
        }

        return RedirectToAction(nameof(Loyalty));
    }

    [HttpGet("mesajlarim")]
    public async Task<IActionResult> Messages([FromQuery] long? conversationId, CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetMessagesAsync(GetCurrentUserId(), conversationId, cancellationToken);
        ViewData["PageCssPath"] = "kullanici_panel_messages_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_messages_mobil";
        ViewData["PanelTitle"] = "Mesajlarım";
        ViewData["PanelSubtitle"] = "Oteller ve destek ekipleri ile tüm mesajlaşma akışlarını yönetin.";
        return View("~/Views/Paneller/User/Messages.cshtml", model);
    }

    [HttpGet("profil-bilgilerim")]
    public async Task<IActionResult> Profile([FromQuery] bool openCompletion = false, [FromQuery] bool openPhoneVerification = false, [FromQuery] bool openEmailUpdate = false, [FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetProfileAsync(GetCurrentUserId(), cancellationToken);
        model.OpenCompletionModal = openCompletion;
        model.OpenPhoneVerification = openPhoneVerification;
        model.OpenEmailUpdate = openEmailUpdate;
        model.PhoneVerification = await _phoneVerificationService.GetUserStatusAsync(GetCurrentUserId(), cancellationToken);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            model.ReturnUrl = returnUrl;
        }
        ViewData["PageCssPath"] = "kullanici_panel_profile_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_profile_mobil";
        ViewData["PanelTitle"] = "Profil Bilgilerim";
        ViewData["PanelSubtitle"] = "Kişisel bilgilerinizi, iletişim verilerinizi ve seyahat tercihlerinizi düzenleyin.";
        return View("~/Views/Paneller/User/Profile.cshtml", model);
    }

    [HttpGet("faturalarim")]
    public async Task<IActionResult> Invoices(CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetInvoicesAsync(GetCurrentUserId(), cancellationToken);
        ViewData["PageCssPath"] = "kullanici_panel_invoices_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_invoices_mobil";
        ViewData["PanelTitle"] = "Faturalarım";
        ViewData["PanelSubtitle"] = "Tamamlanan konaklamalarınıza ait yüklenmiş fatura belgelerini görüntüleyin.";
        return View("~/Views/Paneller/User/Invoices.cshtml", model);
    }

    [HttpGet("odeme-yontemleri")]
    public async Task<IActionResult> PaymentMethods(CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _userPanelService.GetPaymentMethodsAsync(GetCurrentUserId(), cancellationToken);
        ViewData["PageCssPath"] = "kullanici_panel_payment_methods_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_payment_methods_mobil";
        ViewData["PanelTitle"] = "Ödeme Yöntemleri";
        ViewData["PanelSubtitle"] = "Kayıtlı kartlarınızı, fatura bilgilerinizi ve ödeme güvenliğini yönetin.";
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
        ViewData["PageCssPath"] = "kullanici_panel_notifications_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_notifications_mobil";
        ViewData["PanelTitle"] = "Bildirim Tercihleri";
        ViewData["PanelSubtitle"] = "E-posta, SMS ve uygulama içi bildirim tercihlerinizi özelleştirin.";
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
        ViewData["PageCssPath"] = "kullanici_panel_security_masaustu";
        ViewData["PageCssMobilePath"] = "kullanici_panel_security_mobil";
        ViewData["PanelTitle"] = "Güvenlik ve Giriş";
        ViewData["PanelSubtitle"] = "Şifre, aktif oturumlar ve iki aşamalı doğrulama ayarlarını yönetin.";
        return View("~/Views/Paneller/User/Security.cshtml", model);
    }

    [HttpPost("profil-bilgilerim/kaydet")]
    [ValidateAntiForgeryToken]
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

    [HttpPost("profil-bilgilerim/seyahat-tercihleri")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTravelPreferences(UserTravelPreferencesForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var saved = await _userPanelService.SaveTravelPreferencesAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[saved ? "UserProfileSuccess" : "UserProfileError"] = saved
                ? "Seyahat tercihleri güncellendi."
                : "Seyahat tercihleri güncellenemedi.";
        }

        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("profil-bilgilerim/telefon-kodu-gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPhoneVerificationCode(string? phoneNumber, string? returnUrl, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _phoneVerificationService.SendVerificationCodeAsync(
                GetCurrentUserId(),
                phoneNumber,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            TempData[result.Success ? "UserProfileSuccess" : "UserProfileError"] = result.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Profile), new { openPhoneVerification = true });
    }

    [HttpPost("profil-bilgilerim/telefon-kodu-dogrula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPhoneVerificationCode(string verificationCode, string? returnUrl, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _phoneVerificationService.VerifyCodeAsync(
                GetCurrentUserId(),
                verificationCode,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            TempData[result.Success ? "UserProfileSuccess" : "UserProfileError"] = result.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Profile), new { openPhoneVerification = true });
    }

    [HttpPost("profil-bilgilerim/eposta-kodu-gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmailUpdateCode([Bind(Prefix = "EmailUpdate")] UserEmailUpdateRequestForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.RequestEmailUpdateAsync(
                GetCurrentUserId(),
                form,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            TempData[result.Success ? "UserProfileSuccess" : "UserProfileError"] = result.Message;
        }

        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }

        return RedirectToAction(nameof(Profile), new { openEmailUpdate = true });
    }

    [HttpPost("profil-bilgilerim/eposta-kodu-dogrula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmailUpdateCode([Bind(Prefix = "EmailUpdateVerify")] UserEmailUpdateVerifyForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.VerifyEmailUpdateAsync(
                GetCurrentUserId(),
                form,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            TempData[result.Success ? "UserProfileSuccess" : "UserProfileError"] = result.Message;
        }

        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }

        return RedirectToAction(nameof(Profile), new { openEmailUpdate = true });
    }

    [HttpPost("profil-bilgilerim/profil-resmi-yukle")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 12582912)]
    [RequestSizeLimit(12582912)]
    public async Task<IActionResult> UploadProfileImage(IFormFile profileImage, CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (profileImage is null || profileImage.Length <= 0)
        {
            TempData["UserProfileError"] = "Lütfen bir görsel seçin.";
            return RedirectToAction(nameof(Profile));
        }

        if (profileImage.Length > 10 * 1024 * 1024)
        {
            TempData["UserProfileError"] = "Profil görseli en fazla 10 MB olabilir.";
            return RedirectToAction(nameof(Profile));
        }

        if (string.IsNullOrWhiteSpace(profileImage.ContentType) || !profileImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            TempData["UserProfileError"] = "Sadece görsel dosyası yükleyebilirsiniz.";
            return RedirectToAction(nameof(Profile));
        }

        var userId = GetCurrentUserId();
        try
        {
            var saved = await _secureFileService.SaveAsync(profileImage, new SecureFileSaveRequest
            {
                ContextTable = "KULLANICILAR",
                ContextId = userId,
                OwnerUserId = userId,
                VisibilityScope = "user-only",
                Category = "profile"
            }, cancellationToken);

            await _userPanelService.SaveProfileImageAsync(userId, $"secure:{saved.FileId}", "secure-upload", cancellationToken);
            TempData["UserProfileSuccess"] = "Profil görseliniz güncellendi.";
        }
        catch (Exception ex)
        {
            TempData["UserProfileError"] = $"Profil görseli yüklenemedi: {ex.Message}";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("profil-bilgilerim/profil-resmi-sec")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPresetProfileImage(string avatarUrl, CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var normalized = (avatarUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.StartsWith("/uploads/demo/avatars/", StringComparison.OrdinalIgnoreCase))
        {
            TempData["UserProfileError"] = "Geçersiz görsel seçimi.";
            return RedirectToAction(nameof(Profile));
        }

        await _userPanelService.SaveProfileImageAsync(GetCurrentUserId(), normalized, "preset", cancellationToken);
        TempData["UserProfileSuccess"] = "Profil görseliniz güncellendi.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("profil-bilgilerim/profil-resmi-sec-secure")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectUploadedProfileImage(long fileId, CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (fileId <= 0)
        {
            TempData["UserProfileError"] = "Geçersiz profil görseli.";
            return RedirectToAction(nameof(Profile));
        }

        await _userPanelService.SaveProfileImageAsync(GetCurrentUserId(), $"secure:{fileId}", "secure-upload", cancellationToken);
        TempData["UserProfileSuccess"] = "Profil görseliniz güncellendi.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("profil-bilgilerim/profil-resmi-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProfileImage(long fileId, CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var deleted = await _userPanelService.DeleteProfileImageAsync(GetCurrentUserId(), fileId, cancellationToken);
        TempData[deleted ? "UserProfileSuccess" : "UserProfileError"] = deleted
            ? "Profil görseli silindi."
            : "Profil görseli silinemedi.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("mesajlarim/gonder")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 31457280)]
    [RequestSizeLimit(31457280)]
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(MessageDeleteRequest form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            TempData["UserMessageError"] = "Mesaj silme islemi devre disi. Sadece mesaj duzenleme ve yanitlama yapabilirsiniz.";
        }

        return RedirectToAction(nameof(Messages), new { conversationId = form.ConversationId });
    }

    [HttpPost("rezervasyonlarim/not")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveReservationNote(UserReservationNoteForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.SaveReservationNoteAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "UserReservationSuccess" : "UserReservationError"] = result.Message;
        }

        return RedirectToAction(nameof(Reservations));
    }

    [HttpPost("bildirim-tercihleri/kaydet")]
    [ValidateAntiForgeryToken]
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
    [ValidateAntiForgeryToken]
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTwoFactor(UserTwoFactorForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var success = await _userPanelService.SaveTwoFactorAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[success ? "UserSecuritySuccess" : "UserSecurityError"] = success
                ? "Güvenlik tercihi güncellendi."
                : "İki aşamalı doğrulama tercihi kaydedilemedi. E-posta veya telefon doğrulama durumunu kontrol edin.";
        }

        return RedirectToAction(nameof(Security));
    }

    [HttpPost("odeme-yontemleri/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePaymentMethod(UserPaymentMethodForm form, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var result = await _userPanelService.SavePaymentMethodAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "UserPaymentSuccess" : "UserPaymentError"] = result.Message;
        }

        return RedirectToAction(nameof(PaymentMethods));
    }

    [HttpPost("odeme-yontemleri/fatura-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBilling(UserBillingForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessUserPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _userPanelService.SaveBillingInfoAsync(GetCurrentUserId(), form, cancellationToken);
        TempData[result.Success ? "UserPaymentSuccess" : "UserPaymentError"] = result.Message;
        return RedirectToAction(nameof(PaymentMethods));
    }

    [HttpPost("odeme-yontemleri/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePaymentMethod(long paymentMethodId, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            await _userPanelService.DeletePaymentMethodAsync(GetCurrentUserId(), paymentMethodId, cancellationToken);
            TempData["UserPaymentSuccess"] = "Ödeme yöntemi kaldırıldı.";
        }

        return RedirectToAction(nameof(PaymentMethods));
    }

    [HttpPost("odeme-yontemleri/varsayilan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultPaymentMethod(long paymentMethodId, CancellationToken cancellationToken)
    {
        if (CanAccessUserPanel())
        {
            var success = await _userPanelService.SetDefaultPaymentMethodAsync(GetCurrentUserId(), paymentMethodId, cancellationToken);
            TempData[success ? "UserPaymentSuccess" : "UserPaymentError"] = success
                ? "Varsayılan kart güncellendi."
                : "Varsayılan kart ayarlanamadı.";
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
