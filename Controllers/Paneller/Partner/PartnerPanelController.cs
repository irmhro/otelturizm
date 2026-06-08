using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using otelturizmnew.Constants;
using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Paneller;
using otelturizmnew.Models.Paneller.Partner;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Partner;

[Authorize]
[Route("panel/partner")]
public class PartnerPanelController : Controller
{
    private readonly IPartnerService _partnerService;
    private readonly IAuthService _authService;
    private readonly IUserPanelService _userPanelService;
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly IPanelThemeService _panelThemeService;
    private readonly ISecureFileService _secureFileService;
    private readonly IPlatformPackageService _platformPackageService;
    private readonly IHotelPointsService _hotelPointsService;

    public PartnerPanelController(IPartnerService partnerService, IAuthService authService, IUserPanelService userPanelService, IOutputCacheStore outputCacheStore, IPanelThemeService panelThemeService, ISecureFileService secureFileService, IPlatformPackageService platformPackageService, IHotelPointsService hotelPointsService)
    {
        _partnerService = partnerService;
        _authService = authService;
        _userPanelService = userPanelService;
        _outputCacheStore = outputCacheStore;
        _panelThemeService = panelThemeService;
        _secureFileService = secureFileService;
        _platformPackageService = platformPackageService;
        _hotelPointsService = hotelPointsService;
    }

    private async Task EvictPublicOutputCacheAsync(CancellationToken cancellationToken)
    {
        await _outputCacheStore.EvictByTagAsync("public", cancellationToken);
        await _outputCacheStore.EvictByTagAsync("public-short", cancellationToken);
        await _outputCacheStore.EvictByTagAsync("public-medium", cancellationToken);
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(long? otelId, DateTime? dateFrom, DateTime? dateTo, string? status, string? paymentMethod, int pageSize = 7, long? conversationId = null, CancellationToken cancellationToken = default)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetDashboardAsync(GetUserId(), otelId, dateFrom, dateTo, status, paymentMethod, pageSize, conversationId, cancellationToken);
            ViewData["Title"] = "Partner Paneli";
            ViewData["PageCssPath"] = "partnerpanel_dashboard_masaustu";
            ViewData["PageCssMobilePath"] = "partnerpanel_dashboard_mobil";
            ViewData["BodyClass"] = "layout-boxed";
            return View("~/Views/Paneller/Partner/Dashboard.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Paneli";
            ViewData["PageCssPath"] = "partnerpanel_dashboard_masaustu";
            ViewData["PageCssMobilePath"] = "partnerpanel_dashboard_mobil";
            ViewData["BodyClass"] = "layout-boxed";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("guvenlik")]
    public async Task<IActionResult> Security(CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var dashboard = await _partnerService.GetDashboardAsync(GetUserId(), cancellationToken: cancellationToken);
        ViewData["PartnerShell"] = dashboard.Shell;
        var model = await _authService.GetTwoFactorSecurityAsync(GetUserId(), "partner", cancellationToken);
        ViewData["Title"] = "Güvenlik";
        ViewData["PageCssPath"] = "paneller/partner/security";
        return View("~/Views/Paneller/Partner/Security.cshtml", model);
    }

    [HttpGet("guvenlik/tesis-kullanicilari")]
    public async Task<IActionResult> FacilityUsers(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetFacilityUsersAsync(GetUserId(), otelId, cancellationToken);
            ViewData["PartnerShell"] = model.Shell;
            ViewData["Title"] = "Tesis Kullanıcıları";
            ViewData["PageCssPath"] = "paneller/partner/facility-users";
            return View("~/Views/Paneller/Partner/FacilityUsers.cshtml", model);
        }
        catch (InvalidOperationException)
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("guvenlik/tesis-kullanicilari/davet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InviteFacilityUser(otelturizmnew.Models.Paneller.Partner.PartnerFacilityUserInviteRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var result = await _partnerService.InviteFacilityUserAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/guvenlik/tesis-kullanicilari?otelId={request.HotelId}");
    }

    [HttpPost("guvenlik/tesis-kullanicilari/iptal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeFacilityUser(otelturizmnew.Models.Paneller.Partner.PartnerFacilityUserRevokeRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var result = await _partnerService.RevokeFacilityUserAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/guvenlik/tesis-kullanicilari?otelId={request.HotelId}");
    }

    [HttpGet("tesis-kullanici-onay")]
    [AllowAnonymous]
    public async Task<IActionResult> ApproveFacilityInvite(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return Redirect("/partner-giris");
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            var returnUrl = $"/panel/partner/tesis-kullanici-onay?token={Uri.EscapeDataString(token)}";
            return Redirect($"/partner-giris?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }
        if (!IsPartnerUser()) return Redirect("/partner-giris");

        var result = await _partnerService.ApproveFacilityInviteAsync(GetUserId(), token, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect("/panel/partner/dashboard");
    }

    [HttpPost("guvenlik/iki-asamali")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTwoFactor(otelturizmnew.Models.Paneller.User.UserTwoFactorForm form, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _authService.SaveTwoFactorSecurityAsync(GetUserId(), form, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Güvenlik ayarları kaydedilemedi: {ex.Message}";
        }
        return Redirect("/panel/partner/guvenlik");
    }

    [HttpPost("guvenlik/iki-asamali/inline")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTwoFactorInline(otelturizmnew.Models.Paneller.User.UserTwoFactorForm form, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Json(new { success = false, message = "Yetkisiz." });
        try
        {
            var result = await _authService.SaveTwoFactorSecurityAsync(GetUserId(), form, cancellationToken);
            return Json(new
            {
                success = result.Success,
                message = result.Message,
                enabled = form.Enabled,
                channel = (form.Channel ?? "email").Trim().ToLowerInvariant()
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Güvenlik ayarları kaydedilemedi: " + ex.Message });
        }
    }

    [HttpGet("bildirim-tercihleri")]
    public async Task<IActionResult> NotificationPreferences(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var dashboard = await _partnerService.GetDashboardAsync(GetUserId(), otelId, cancellationToken: cancellationToken);
            ViewData["PartnerShell"] = dashboard.Shell;
            ViewData["Title"] = "Bildirim Tercihleri";
            ViewData["PageCssPath"] = "paneller/partner/notification-preferences";

            var notifications = await _userPanelService.GetNotificationsAsync(GetUserId(), cancellationToken);
            var recent = await _authService.GetRecentLoginHistoryAsync(GetUserId(), 5, cancellationToken);

            var model = new PartnerNotificationPreferencesPageViewModel
            {
                Shell = dashboard.Shell,
                Form = notifications.Form,
                RecentLogins = recent.Select(r => new PartnerLoginHistoryRowViewModel
                {
                    TimeText = r.TimeText,
                    IpAddress = r.IpAddress,
                    DurationText = r.DurationText,
                    DeviceLabel = r.DeviceLabel
                }).ToList()
            };

            return View("~/Views/Paneller/Partner/NotificationPreferences.cshtml", model);
        }
        catch (InvalidOperationException)
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("bildirim-tercihleri/kaydet-inline")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNotificationPreferencesInline(otelturizmnew.Models.Paneller.User.UserNotificationPreferencesForm form, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Json(new { success = false, message = "Yetkisiz." });
        try
        {
            var saved = await _userPanelService.SaveNotificationsAsync(GetUserId(), form, cancellationToken);
            return Json(new { success = saved, message = saved ? "Tercihler güncellendi." : "Tercihler kaydedilemedi." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Tercihler kaydedilemedi: " + ex.Message });
        }
    }

    [HttpPost("tema/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTheme(PanelThemeViewModel theme, long? otelId, string scope = "partner", string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var normalizedScope = (scope ?? "partner").Trim().ToLowerInvariant();
            if (normalizedScope != "user" && normalizedScope != "partner") normalizedScope = "partner";

            // Partner panelinde iki katman var:
            // - user: kullanici bazli tema
            // - partner: partner bazli tema (otelId üzerinden partnerId resolve gerekiyordu; mevcut akış bunu zaten taşıyor)
            // Bu yüzden "partner" scope'da mevcut servisi kullanmaya devam ediyoruz; "user" scope ise ortak servise yazılabilir.
            var result = normalizedScope == "user"
                ? await _panelThemeService.SaveAsync("user", GetUserId(), theme, cancellationToken)
                : await _partnerService.SaveThemeAsync(GetUserId(), otelId, normalizedScope, theme, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Tema kaydedilemedi: {ex.Message}";
        }

        var redirectHotelQuery = otelId.HasValue && otelId.Value > 0 ? $"?otelId={otelId.Value}" : string.Empty;
        var safeReturn = NormalizePartnerLocalPath(returnUrl);
        if (!string.IsNullOrEmpty(safeReturn))
        {
            return Redirect($"{safeReturn}{redirectHotelQuery}");
        }

        return Redirect($"/panel/partner/dashboard{redirectHotelQuery}");
    }

    [HttpGet("rezervasyonlar")]
    public async Task<IActionResult> Reservations(long? otelId, DateTime? dateFrom, DateTime? dateTo, string? status, string? paymentMethod, int page = 1, int pageSize = 7, long? conversationId = null, string? q = null, CancellationToken cancellationToken = default)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetReservationsAsync(GetUserId(), otelId, dateFrom, dateTo, status, paymentMethod, page, pageSize, conversationId, q, cancellationToken);
            ViewData["Title"] = "Partner Rezervasyonlar";
            ViewData["PageCssPath"] = "paneller/partner/reservations";
            return View("~/Views/Paneller/Partner/Reservations.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Rezervasyonlar";
            ViewData["PageCssPath"] = "paneller/partner/reservations";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("rezervasyonlar/durum")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReservationStatus(PartnerReservationStatusRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.UpdateReservationStatusAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Rezervasyon durumu güncellenemedi: {ex.Message}";
        }

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl))
        {
            return Redirect(request.ReturnUrl);
        }
        return Redirect($"/panel/partner/rezervasyonlar?otelId={request.HotelId}");
    }

    [HttpPost("rezervasyonlar/odeme-tamamlandi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaymentCompleted(long hotelId, long reservationId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.MarkReservationPaymentCompletedAsync(GetUserId(), hotelId, reservationId, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Ödeme tamamlandı işlemi yapılamadı: {ex.Message}";
        }

        return Redirect($"/panel/partner/rezervasyonlar?otelId={hotelId}");
    }

    [HttpPost("rezervasyonlar/misafire-mesaj")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendGuestMessage(PartnerGuestMessageRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SendGuestMessageAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Misafire mesaj gönderilemedi: {ex.Message}";
        }

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl))
        {
            return Redirect(request.ReturnUrl);
        }

        if (request.ConversationId.HasValue && request.ConversationId.Value > 0)
        {
            return Redirect($"/panel/partner/rezervasyonlar/misafir-mesajlari?otelId={request.HotelId}&conversationId={request.ConversationId.Value}");
        }

        return Redirect($"/panel/partner/rezervasyonlar?otelId={request.HotelId}");
    }

    [HttpGet("rezervasyonlar/disa-aktar")]
    public async Task<IActionResult> ExportReservations(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var csv = await _partnerService.ExportReservationsCsvAsync(GetUserId(), otelId, cancellationToken);
        var fileName = $"partner-rezervasyonlar-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("takvim-fiyatlar")]
    public async Task<IActionResult> Pricing(long? otelId, long? roomId, string? month, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPricingAsync(GetUserId(), otelId, roomId, month, cancellationToken);
            ViewData["Title"] = "Partner Takvim ve Fiyatlar";
            ViewData["PageCssPath"] = "paneller/partner/pricing";
            return View("~/Views/Paneller/Partner/Pricing.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Takvim ve Fiyatlar";
            ViewData["PageCssPath"] = "paneller/partner/pricing";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("firma-fiyatlari")]
    public async Task<IActionResult> CompanyPricing(long? otelId, long? companyId, long? roomId, string? month, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetCompanyPricingAsync(GetUserId(), otelId, companyId, roomId, month, cancellationToken);
            ViewData["Title"] = "Kurumsal (Firma) Fiyatları";
            ViewData["PageCssPath"] = "paneller/partner/company-pricing";
            return View("~/Views/Paneller/Partner/CompanyPricing.cshtml", model);
        }
        catch (InvalidOperationException)
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("aboneliklerim")]
    public async Task<IActionResult> ListingSubscriptions(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetListingSubscriptionsAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Aboneliklerim";
            ViewData["PageCssPath"] = "paneller/partner/listing-subscriptions";
            return View("~/Views/Paneller/Partner/ListingSubscriptions.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Aboneliklerim";
            ViewData["PageCssPath"] = "paneller/partner/listing-subscriptions";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("aboneliklerim/talep-olustur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateListingSubscription(PartnerListingSubscriptionCreateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.CreateListingSubscriptionAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Abonelik talebi oluşturulamadı: {ex.Message}";
        }

        return Redirect($"/panel/partner/aboneliklerim?otelId={request.HotelId}");
    }

    [HttpGet("platform-paketleri")]
    public async Task<IActionResult> PlatformPackages(long? otelId, string? kategori, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _platformPackageService.GetPartnerCatalogAsync(GetUserId(), otelId, kategori, cancellationToken);
            ViewData["Title"] = "Platform Paketleri";
            ViewData["PageCssPath"] = "paneller/partner/platform-packages";
            return View("~/Views/Paneller/Partner/PlatformPackages.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Platform Paketleri";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("platform-paketleri/detay/{paketId:long}")]
    public async Task<IActionResult> PlatformPackageDetail(long paketId, long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _platformPackageService.GetPartnerPackageDetailAsync(GetUserId(), otelId, paketId, cancellationToken);
        if (model is null) return RedirectToAction(nameof(PlatformPackages), new { otelId });
        ViewData["Title"] = model.Package.Title;
        ViewData["PageCssPath"] = "paneller/partner/platform-packages";
        return View("~/Views/Paneller/Partner/PlatformPackageDetail.cshtml", model);
    }

    [HttpPost("platform-paketleri/basvuru-olustur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlatformPackageApplication(PartnerPlatformPackageApplicationFormModel request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var result = await _platformPackageService.CreatePartnerApplicationAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/platform-paketleri?otelId={request.HotelId}");
    }

    [HttpPost("firma-fiyatlari/toplu-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCompanyBulkPricing(otelturizmnew.Models.Paneller.Partner.PartnerCompanyBulkPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ApplyCompanyBulkPricingAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
            if (result.Success)
            {
                await EvictPublicOutputCacheAsync(cancellationToken);
            }
            return Redirect($"/panel/partner/firma-fiyatlari?otelId={request.HotelId}&roomId={request.RoomTypeId}&month={request.StartDate:yyyy-MM}");
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = "Firma fiyatları kaydedilemedi: " + ex.Message;
            return Redirect($"/panel/partner/firma-fiyatlari?otelId={request.HotelId}");
        }
    }

    [HttpGet("kampanyalar")]
    public async Task<IActionResult> Campaigns(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetCampaignsAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Partner Kampanyalar";
            ViewData["PageCssPath"] = "paneller/partner/campaigns";
            return View("~/Views/Paneller/Partner/Campaigns.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Kampanyalar";
            ViewData["PageCssPath"] = "paneller/partner/campaigns";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("kampanyalar/katil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinCampaign(PartnerCampaignJoinRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.JoinCampaignAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Kampanyaya katılım kaydedilemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/kampanyalar?otelId={request.HotelId}");
    }

    [HttpPost("kampanyalar/ayril")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LeaveCampaign(long hotelId, long campaignId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.LeaveCampaignAsync(GetUserId(), hotelId, campaignId, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Kampanyadan çıkış kaydedilemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/kampanyalar?otelId={hotelId}");
    }

    [HttpPost("takvim-fiyatlar/toplu-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyBulkPricing(PartnerBulkPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ApplyBulkPricingAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Toplu fiyat güncelleme uygulanamadı: {ex.Message}";
        }
        var roomId = request.ViewRoomId ?? request.RoomId ?? request.SelectedRoomIds.FirstOrDefault();
        var roomQuery = roomId > 0 ? $"&roomId={roomId}" : string.Empty;
        var monthQuery = !string.IsNullOrWhiteSpace(request.ViewMonth) ? $"&month={Uri.EscapeDataString(request.ViewMonth)}" : string.Empty;
        return Redirect($"/panel/partner/takvim-fiyatlar?otelId={request.HotelId}{roomQuery}{monthQuery}");
    }

    [HttpPost("takvim-fiyatlar/gun-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyDailyPricing(PartnerDailyPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ApplyDailyPricingAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
            if (result.Success)
            {
                await EvictPublicOutputCacheAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Gün fiyat güncelleme uygulanamadı: {ex.Message}";
        }
        var monthQuery = !string.IsNullOrWhiteSpace(request.ViewMonth) ? $"&month={Uri.EscapeDataString(request.ViewMonth)}" : string.Empty;
        return Redirect($"/panel/partner/takvim-fiyatlar?otelId={request.HotelId}&roomId={request.RoomId}{monthQuery}");
    }

    [HttpGet("oda-yonetimi")]
    public async Task<IActionResult> Rooms(long? otelId, long? roomId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetRoomsAsync(GetUserId(), otelId, roomId, cancellationToken);
            ViewData["Title"] = "Partner Oda Yönetimi";
            ViewData["PageCssPath"] = "paneller/partner/rooms";
            return View("~/Views/Paneller/Partner/Rooms.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Oda Yönetimi";
            ViewData["PageCssPath"] = "paneller/partner/rooms";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    // Geriye dönük uyumluluk: eski/yanlış URL'ler 404 vermesin.
    [HttpGet("rooms")]
    public IActionResult RoomsLegacy(long? otelId, long? roomId)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var hotelQuery = otelId.HasValue ? $"otelId={otelId.Value}" : string.Empty;
        var roomQuery = roomId.HasValue ? $"roomId={roomId.Value}" : string.Empty;
        var join = !string.IsNullOrWhiteSpace(hotelQuery) && !string.IsNullOrWhiteSpace(roomQuery) ? "&" : string.Empty;
        var qs = !string.IsNullOrWhiteSpace(hotelQuery) || !string.IsNullOrWhiteSpace(roomQuery)
            ? "?" + hotelQuery + join + roomQuery
            : string.Empty;
        return Redirect("/panel/partner/oda-yonetimi" + qs);
    }

    [HttpPost("oda-yonetimi/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRoom(PartnerRoomUpsertRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _partnerService.UpsertRoomAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            // Canlıda "Error." sayfası yerine panel içinde hata göstermek için yakala.
            TempData["PartnerError"] = $"Oda kaydedilemedi: {ex.Message}";
        }

        var roomQuery = request.RoomId.HasValue && request.RoomId.Value > 0
            ? $"&roomId={request.RoomId.Value}"
            : string.Empty;
        return Redirect($"/panel/partner/oda-yonetimi?otelId={request.HotelId}{roomQuery}#room-form");
    }

    [HttpPost("oda-yonetimi/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoom(long hotelId, long roomId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.DeleteRoomAsync(GetUserId(), hotelId, roomId, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Oda tipi silinemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/oda-yonetimi?otelId={hotelId}");
    }

    [HttpPost("oda-yonetimi/gorsel-yukle")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 157286400)]
    [RequestSizeLimit(157286400)]
    public async Task<IActionResult> UploadRoomPhotos(PartnerRoomPhotoUploadRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.UploadRoomPhotosAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Oda görselleri yüklenemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/oda-yonetimi?otelId={request.HotelId}&roomId={request.RoomId}#room-gallery");
    }

    [HttpPost("oda-yonetimi/gorsel-kapak-yap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRoomCover(long hotelId, long roomId, long photoId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SetRoomCoverAsync(GetUserId(), hotelId, roomId, photoId, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Kapak görseli ayarlanamadı: {ex.Message}";
        }
        return Redirect($"/panel/partner/oda-yonetimi?otelId={hotelId}&roomId={roomId}#room-gallery");
    }

    [HttpPost("oda-yonetimi/gorsel-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoomPhoto(long hotelId, long roomId, long photoId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.DeleteRoomPhotoAsync(GetUserId(), hotelId, roomId, photoId, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Oda görseli silinemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/oda-yonetimi?otelId={hotelId}&roomId={roomId}#room-gallery");
    }

    [HttpGet("otel-bilgileri")]
    public async Task<IActionResult> HotelInfo(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetHotelInfoAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Partner Otel Bilgileri";
            ViewData["PageCssPath"] = "paneller/partner/hotel-info";
            return View("~/Views/Paneller/Partner/HotelInfo.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Otel Bilgileri";
            ViewData["PageCssPath"] = "paneller/partner/hotel-info";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("otel-bilgileri/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHotelInfo(PartnerHotelInfoForm request, CancellationToken cancellationToken)
    {
        var coordinateError = NormalizeCoordinateInputs(Request, request);
        if (!string.IsNullOrWhiteSpace(coordinateError))
        {
            TempData["PartnerError"] = coordinateError;
            return Redirect($"/panel/partner/otel-bilgileri?otelId={request.HotelId}");
        }

        var result = await _partnerService.UpdateHotelInfoAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/otel-bilgileri?otelId={request.HotelId}");
    }

    private static string? NormalizeCoordinateInputs(HttpRequest httpRequest, PartnerHotelInfoForm request)
    {
        if (!httpRequest.HasFormContentType)
        {
            return null;
        }

        var latitudeResult = TryParseCoordinate(httpRequest.Form["Latitude"]);
        if (!latitudeResult.Success)
        {
            return "Enlem alanına 40.9060787 gibi geçerli bir koordinat girin.";
        }

        var longitudeResult = TryParseCoordinate(httpRequest.Form["Longitude"]);
        if (!longitudeResult.Success)
        {
            return "Boylam alanına 29.2809220 gibi geçerli bir koordinat girin.";
        }

        request.Latitude = latitudeResult.Value;
        request.Longitude = longitudeResult.Value;
        return null;
    }

    private static (bool Success, decimal? Value) TryParseCoordinate(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return (true, null);
        }

        var normalized = value.Replace(" ", string.Empty).Replace(",", ".", StringComparison.Ordinal);
        return decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? (true, parsed)
            : (false, null);
    }

    [HttpGet("fotograflar")]
    public async Task<IActionResult> Photos(long? otelId, long? photoId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPhotosAsync(GetUserId(), otelId, photoId, cancellationToken);
            ViewData["Title"] = "Partner Fotoğraflar";
            ViewData["PageCssPath"] = "paneller/partner/photos";
            return View("~/Views/Paneller/Partner/Photos.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Fotoğraflar";
            ViewData["PageCssPath"] = "paneller/partner/photos";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("fotograflar/yukle")]
    public IActionResult UploadPhotosPage(long? otelId)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        return Redirect(otelId.HasValue
            ? $"/panel/partner/fotograflar?otelId={otelId.Value}#fotograf-yukle"
            : "/panel/partner/fotograflar#fotograf-yukle");
    }

    [HttpPost("fotograflar/yukle")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 314572800)]
    [RequestSizeLimit(314572800)]
    public async Task<IActionResult> UploadPhotos(PartnerPhotoUploadRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.UploadPhotosAsync(GetUserId(), request, cancellationToken);
            if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    redirectUrl = $"/panel/partner/fotograflar?otelId={request.HotelId}"
                });
            }

            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(new { success = false, message = ex.Message, redirectUrl = $"/panel/partner/fotograflar?otelId={request.HotelId}" });
            }

            TempData["PartnerError"] = $"Fotoğraf yüklenemedi: {ex.Message}";
        }

        return Redirect($"/panel/partner/fotograflar?otelId={request.HotelId}");
    }

    [HttpPost("fotograflar/kapak-yap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCover(long hotelId, long photoId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SetCoverPhotoAsync(GetUserId(), hotelId, photoId, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Kapak fotoğrafı ayarlanamadı: {ex.Message}";
        }
        return Redirect($"/panel/partner/fotograflar?otelId={hotelId}");
    }

    [HttpPost("fotograflar/guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePhoto(PartnerPhotoEditForm request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.UpdatePhotoAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Fotoğraf güncellenemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/fotograflar?otelId={request.HotelId}");
    }

    [HttpPost("fotograflar/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(long hotelId, long photoId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.DeletePhotoAsync(GetUserId(), hotelId, photoId, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Fotoğraf silinemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/fotograflar?otelId={hotelId}");
    }

    [HttpPost("fotograflar/toplu-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeletePhotos(PartnerPhotoBulkDeleteRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.BulkDeletePhotosAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Toplu fotoğraf silme işlemi başarısız: {ex.Message}";
        }
        return Redirect($"/panel/partner/fotograflar?otelId={request.HotelId}");
    }

    [HttpGet("performans")]
    public async Task<IActionResult> Performance(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPerformanceAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Partner Performans";
            ViewData["PageCssPath"] = "paneller/partner/performance";
            return View("~/Views/Paneller/Partner/Performance.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Performans";
            ViewData["PageCssPath"] = "paneller/partner/performance";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("performans/rakip-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCompetitor(PartnerCompetitorUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SaveCompetitorAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Rakip kaydedilemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/performans?otelId={request.HotelId}");
    }

    [HttpGet("performans/rapor-indir")]
    public async Task<IActionResult> ExportPerformance(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var csv = await _partnerService.ExportPerformanceCsvAsync(GetUserId(), otelId, cancellationToken);
        var fileName = $"partner-performans-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("degerlendirmeler")]
    public async Task<IActionResult> Reviews(long? otelId, string? durum, string? yanit, int page = 1, int pageSize = 7, CancellationToken cancellationToken = default)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetReviewsAsync(GetUserId(), otelId, durum, yanit, page, pageSize, cancellationToken);
            ViewData["Title"] = "Partner Değerlendirmeler";
            ViewData["PageCssPath"] = "paneller/partner/reviews";
            return View("~/Views/Paneller/Partner/Reviews.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Değerlendirmeler";
            ViewData["PageCssPath"] = "paneller/partner/reviews";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("degerlendirmeler/yanitla")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyReview(PartnerReviewReplyRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ReplyToReviewAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Yorum yanıtı kaydedilemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/degerlendirmeler?otelId={request.HotelId}");
    }

    [HttpPost("degerlendirmeler/raporla")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportReview(PartnerReviewReportRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ReportReviewAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Yorum raporlanamadı: {ex.Message}";
        }
        return Redirect($"/panel/partner/degerlendirmeler?otelId={request.HotelId}");
    }

    [HttpPost("degerlendirmeler/kaldirma-talebi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReviewTakedown([FromForm] long hotelId, [FromForm] long reviewId, [FromForm] string? reason, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.RequestReviewTakedownAsync(GetUserId(), hotelId, reviewId, reason, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Kaldırma talebi gönderilemedi: {ex.Message}";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return Redirect($"/panel/partner/degerlendirmeler?otelId={hotelId}");
    }

    [HttpGet("finans")]
    public async Task<IActionResult> Finance(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetFinanceAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Partner Finans";
            ViewData["PageCssPath"] = "paneller/partner/finance";
            return View("~/Views/Paneller/Partner/Finance.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Finans";
            ViewData["PageCssPath"] = "paneller/partner/finance";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("finans/banka-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBankInfo(PartnerBankInfoForm request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SaveBankInfoAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Banka bilgileri kaydedilemedi: {ex.Message}";
        }

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl))
        {
            return Redirect(request.ReturnUrl);
        }

        return Redirect($"/panel/partner/finans?otelId={request.HotelId}");
    }

    [HttpGet("finans/disa-aktar")]
    public async Task<IActionResult> ExportFinance(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var csv = await _partnerService.ExportFinanceCsvAsync(GetUserId(), otelId, cancellationToken);
        var fileName = $"partner-finans-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("finans/fatura-indir")]
    public async Task<IActionResult> DownloadInvoice(long hotelId, long invoiceId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var document = await _partnerService.DownloadInvoiceAsync(GetUserId(), hotelId, invoiceId, cancellationToken);
        if (document is null)
        {
            TempData["PartnerError"] = "Indirilecek fatura bulunamadi.";
            return Redirect($"/panel/partner/finans?otelId={hotelId}");
        }

        return File(document.Value.Content, document.Value.ContentType, document.Value.FileName);
    }

    [HttpGet("tercihler")]
    public async Task<IActionResult> Settings(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPartnerPreferencesAsync(GetUserId(), otelId, cancellationToken);
            ViewData["PartnerShell"] = model.Shell;
            ViewData["Title"] = "Tercihler";
            ViewData["PageCssPath"] = "paneller/partner/settings";
            return View("~/Views/Paneller/Partner/Settings.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Tercihler";
            ViewData["PageCssPath"] = "paneller/partner/settings";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("basvuru-ve-evraklar")]
    public async Task<IActionResult> ApplicationDocuments(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetApplicationAsync(GetUserId(), otelId, cancellationToken);
            ViewData["PartnerShell"] = model.Shell;
            ViewData["Title"] = "Partner Başvuru ve Evraklar";
            ViewData["PageCssPath"] = "paneller/partner/preferences";
            return View("~/Views/Paneller/Partner/Preferences.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Başvuru ve Evraklar";
            ViewData["PageCssPath"] = "paneller/partner/preferences";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("basvuru-ve-evraklar/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreferences(PartnerApplicationProfileForm request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SaveApplicationAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Başvuru bilgileri kaydedilemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/basvuru-ve-evraklar?otelId={request.HotelId}");
    }

    [HttpPost("tercihler/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePartnerPreferences(PartnerPreferencesForm form, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SavePartnerPreferencesAsync(GetUserId(), form, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Tercihler kaydedilemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/tercihler?otelId={form.HotelId}");
    }

    [HttpPost("basvuru-ve-evraklar/evrak-yukle")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    [RequestSizeLimit(52428800)]
    public async Task<IActionResult> UploadApplicationDocument(PartnerApplicationDocumentUploadForm request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.UploadApplicationDocumentAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Evrak yüklenemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/basvuru-ve-evraklar?otelId={request.HotelId}");
    }

    [HttpPost("basvuru-ve-evraklar/evrak-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteApplicationDocument(long hotelId, long documentId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.DeleteApplicationDocumentAsync(GetUserId(), hotelId, documentId, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Evrak silinemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/basvuru-ve-evraklar?otelId={hotelId}");
    }

    [HttpGet("724-destek")]
    public async Task<IActionResult> Support(long? otelId, long? ticketId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetSupportAsync(GetUserId(), otelId, ticketId, cancellationToken);
            ViewData["Title"] = "Partner 7/24 Destek";
            ViewData["PageCssPath"] = "paneller/partner/support";
            return View("~/Views/Paneller/Partner/Support.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner 7/24 Destek";
            ViewData["PageCssPath"] = "paneller/partner/support";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("724-destek/talep-olustur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(PartnerSupportCreateTicketRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.CreateSupportTicketAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Destek talebi oluşturulamadı: {ex.Message}";
        }
        return Redirect($"/panel/partner/724-destek?otelId={request.HotelId}");
    }

    [HttpPost("724-destek/mesaj-gonder")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 10485760)]
    [RequestSizeLimit(10485760)]
    public async Task<IActionResult> SendTicketMessage(PartnerSupportSendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SendSupportMessageAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Mesaj gönderilemedi: {ex.Message}";
        }
        return Redirect($"/panel/partner/724-destek?otelId={request.HotelId}&ticketId={request.TicketId}");
    }

    [HttpPost("724-destek/kapat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseTicket(long hotelId, long ticketId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.CloseSupportTicketAsync(GetUserId(), hotelId, ticketId, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Talep kapatılamadı: {ex.Message}";
        }
        return Redirect($"/panel/partner/724-destek?otelId={hotelId}&ticketId={ticketId}");
    }

    [HttpGet("pazarlama/konum-icgoruleri")]
    public async Task<IActionResult> LocationInsights(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetLocationInsightsAsync(GetUserId(), otelId, cancellationToken);
            ViewData["PartnerShell"] = model.Shell;
            ViewData["Title"] = "Konum İçgörüleri";
            ViewData["PageCssPath"] = "paneller/partner/location-insights";
            return View("~/Views/Paneller/Partner/LocationInsights.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("pazarlama/favori-misafirler")]
    public async Task<IActionResult> FavoriteGuests(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetFavoriteGuestsAsync(GetUserId(), otelId, cancellationToken);
            ViewData["PartnerShell"] = model.Shell;
            ViewData["Title"] = "Favori Misafirler";
            ViewData["PageCssPath"] = "paneller/partner/favorite-guests";
            return View("~/Views/Paneller/Partner/FavoriteGuests.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("dagitilan-puanlar")]
    public async Task<IActionResult> DistributedPoints(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var dashboard = await _partnerService.GetDashboardAsync(GetUserId(), otelId, cancellationToken: cancellationToken);
            var rows = await _hotelPointsService.GetPartnerDistributedPointsAsync(GetUserId(), dashboard.Shell.SelectedHotelId, cancellationToken);
            var model = new PartnerDistributedPointsPageViewModel
            {
                Shell = dashboard.Shell,
                SelectedHotelId = dashboard.Shell.SelectedHotelId,
                TotalDistributedPoints = rows.Sum(static x => x.TotalEarned),
                ActiveBalanceCount = rows.Count(static x => x.AvailablePoints > 0),
                Rows = rows.Select(static row => new PartnerDistributedPointsRowViewModel
                {
                    HotelId = row.HotelId,
                    HotelName = row.HotelName,
                    UserId = row.UserId,
                    UserDisplayName = row.UserDisplayName,
                    TotalEarned = row.TotalEarned,
                    AvailablePoints = row.AvailablePoints,
                    UsedPoints = row.UsedPoints,
                    LastEarnedText = row.LastEarnedText
                }).ToList()
            };
            ViewData["PartnerShell"] = model.Shell;
            ViewData["Title"] = "Dağıtılan Puanlar";
            ViewData["PageCssPath"] = "paneller/partner/distributed-points";
            return View("~/Views/Paneller/Partner/DistributedPoints.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("pazarlama/etkinlikler")]
    public async Task<IActionResult> MarketingEvents(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetMarketingEventsAsync(GetUserId(), otelId, cancellationToken);
            ViewData["PartnerShell"] = model.Shell;
            ViewData["Title"] = "Etkinlik Yönetin";
            ViewData["PageCssPath"] = "paneller/partner/marketing-events";
            return View("~/Views/Paneller/Partner/MarketingEvents.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("pazarlama/etkinlikler/not")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMarketingEventNote(PartnerCampaignParticipationNoteRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SaveCampaignParticipationNoteAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Not kaydedilemedi: {ex.Message}";
        }

        return Redirect($"/panel/partner/pazarlama/etkinlikler?otelId={request.HotelId}");
    }

    [HttpGet("hesap-bilgileri")]
    public async Task<IActionResult> AccountInfo(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetAccountInfoAsync(GetUserId(), otelId, cancellationToken);
            ViewData["PartnerShell"] = model.Shell;
            ViewData["Title"] = "Hesap Bilgileri";
            ViewData["PageCssPath"] = "paneller/partner/account-info";
            return View("~/Views/Paneller/Partner/AccountInfo.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Hesap Bilgileri";
            ViewData["PageCssPath"] = "paneller/partner/account-info";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("hesap-bilgileri/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAccountInfo(otelturizmnew.Models.Paneller.Partner.PartnerAccountInfoUpdateForm form, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.SaveAccountInfoAsync(GetUserId(), form, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
            var q = form.HotelId is > 0 ? $"?otelId={form.HotelId.Value}" : string.Empty;
            return Redirect($"/panel/partner/hesap-bilgileri{q}");
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Kaydedilemedi: {ex.Message}";
            var q = form.HotelId is > 0 ? $"?otelId={form.HotelId.Value}" : string.Empty;
            return Redirect($"/panel/partner/hesap-bilgileri{q}");
        }
    }

    [HttpGet("fiyat/super-fiyat")]
    public async Task<IActionResult> SuperPrice(long? otelId, long? roomId, string? month, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPricingAsync(GetUserId(), otelId, roomId, month, cancellationToken);
            ViewData["Title"] = "Süper Fiyat";
            ViewData["PageCssPath"] = "paneller/partner/super-price";
            return View("~/Views/Paneller/Partner/SuperPrice.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Süper Fiyat";
            ViewData["PageCssPath"] = "paneller/partner/super-price";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("fiyat/super-fiyat/uygula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplySuperPrice(PartnerBulkPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ApplyBulkPricingAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Süper fiyat uygulanamadı: {ex.Message}";
        }

        var roomId = request.ViewRoomId ?? request.RoomId ?? request.SelectedRoomIds.FirstOrDefault();
        var roomQuery = roomId > 0 ? $"&roomId={roomId}" : string.Empty;
        var monthQuery = !string.IsNullOrWhiteSpace(request.ViewMonth) ? $"&month={Uri.EscapeDataString(request.ViewMonth)}" : string.Empty;
        return Redirect($"/panel/partner/fiyat/super-fiyat?otelId={request.HotelId}{roomQuery}{monthQuery}");
    }

    [HttpGet("fiyat/indirimler")]
    public async Task<IActionResult> Discounts(long? otelId, long? roomId, string? month, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPricingAsync(GetUserId(), otelId, roomId, month, cancellationToken);
            ViewData["Title"] = "İndirimler";
            ViewData["PageCssPath"] = "paneller/partner/discounts";
            return View("~/Views/Paneller/Partner/Discounts.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "İndirimler";
            ViewData["PageCssPath"] = "paneller/partner/discounts";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("fiyat/kisitlamalar")]
    public async Task<IActionResult> Restrictions(long? otelId, long? roomId, string? month, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPricingAsync(GetUserId(), otelId, roomId, month, cancellationToken);
            ViewData["Title"] = "Kısıtlamalar";
            ViewData["PageCssPath"] = "paneller/partner/restrictions";
            return View("~/Views/Paneller/Partner/Restrictions.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Kısıtlamalar";
            ViewData["PageCssPath"] = "paneller/partner/restrictions";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("fiyat/kisitlamalar/uygula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyRestrictions(PartnerBulkPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ApplyBulkPricingAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Kısıtlamalar uygulanamadı: {ex.Message}";
        }

        var roomId = request.ViewRoomId ?? request.RoomId ?? request.SelectedRoomIds.FirstOrDefault();
        var roomQuery = roomId > 0 ? $"&roomId={roomId}" : string.Empty;
        var monthQuery = !string.IsNullOrWhiteSpace(request.ViewMonth) ? $"&month={Uri.EscapeDataString(request.ViewMonth)}" : string.Empty;
        return Redirect($"/panel/partner/fiyat/kisitlamalar?otelId={request.HotelId}{roomQuery}{monthQuery}");
    }

    [HttpGet("fiyat/gunluk-notlar")]
    public async Task<IActionResult> DailyNotes(long? otelId, long? roomId, string? month, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPricingAsync(GetUserId(), otelId, roomId, month, cancellationToken);
            ViewData["Title"] = "Günlük Notlar";
            ViewData["PageCssPath"] = "paneller/partner/daily-notes";
            return View("~/Views/Paneller/Partner/DailyNotes.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Günlük Notlar";
            ViewData["PageCssPath"] = "paneller/partner/daily-notes";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("fiyat/gunluk-notlar/uygula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyDailyNotes(PartnerBulkPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ApplyBulkPricingAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Günlük notlar uygulanamadı: {ex.Message}";
        }

        var roomId = request.ViewRoomId ?? request.RoomId ?? request.SelectedRoomIds.FirstOrDefault();
        var roomQuery = roomId > 0 ? $"&roomId={roomId}" : string.Empty;
        var monthQuery = !string.IsNullOrWhiteSpace(request.ViewMonth) ? $"&month={Uri.EscapeDataString(request.ViewMonth)}" : string.Empty;
        return Redirect($"/panel/partner/fiyat/gunluk-notlar?otelId={request.HotelId}{roomQuery}{monthQuery}");
    }

    [HttpPost("fiyat/indirimler/uygula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyDiscounts(PartnerBulkPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ApplyBulkPricingAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"İndirim uygulanamadı: {ex.Message}";
        }

        var roomId = request.ViewRoomId ?? request.RoomId ?? request.SelectedRoomIds.FirstOrDefault();
        var roomQuery = roomId > 0 ? $"&roomId={roomId}" : string.Empty;
        var monthQuery = !string.IsNullOrWhiteSpace(request.ViewMonth) ? $"&month={Uri.EscapeDataString(request.ViewMonth)}" : string.Empty;
        return Redirect($"/panel/partner/fiyat/indirimler?otelId={request.HotelId}{roomQuery}{monthQuery}");
    }

    [HttpGet("fiyat/stok-kontenjan")]
    public async Task<IActionResult> StockQuota(long? otelId, long? roomId, string? month, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPricingAsync(GetUserId(), otelId, roomId, month, cancellationToken);
            ViewData["Title"] = "Oda Kontenjanları";
            ViewData["PageCssPath"] = "paneller/partner/stock-quota";
            return View("~/Views/Paneller/Partner/StockQuota.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Oda Kontenjanları";
            ViewData["PageCssPath"] = "paneller/partner/stock-quota";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("fiyat/stok-kontenjan/uygula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyStockQuota(PartnerBulkPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var result = await _partnerService.ApplyBulkPricingAsync(GetUserId(), request, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Kontenjan güncelleme uygulanamadı: {ex.Message}";
        }

        var roomId = request.ViewRoomId ?? request.RoomId ?? request.SelectedRoomIds.FirstOrDefault();
        var roomQuery = roomId > 0 ? $"&roomId={roomId}" : string.Empty;
        var monthQuery = !string.IsNullOrWhiteSpace(request.ViewMonth) ? $"&month={Uri.EscapeDataString(request.ViewMonth)}" : string.Empty;
        return Redirect($"/panel/partner/fiyat/stok-kontenjan?otelId={request.HotelId}{roomQuery}{monthQuery}");
    }

    [HttpGet("odeme/ayarlari")]
    public async Task<IActionResult> PaymentSettings(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetFinanceAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Ödeme Ayarları";
            ViewData["PageCssPath"] = "paneller/partner/finance";
            return View("~/Views/Paneller/Partner/PaymentSettings.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("finans/faturalar")]
    public async Task<IActionResult> Invoices(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetFinanceAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Faturalar";
            ViewData["PageCssPath"] = "paneller/partner/finance";
            return View("~/Views/Paneller/Partner/Invoices.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("finans/misafir-faturalari")]
    public async Task<IActionResult> GuestInvoices(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetGuestInvoicesAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Misafir Faturaları";
            ViewData["PageCssPath"] = "paneller/partner/guest-invoices";
            return View("~/Views/Paneller/Partner/GuestInvoices.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("finans/misafir-faturalari/yukle")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    [RequestSizeLimit(52428800)]
    public async Task<IActionResult> UploadGuestInvoice(long hotelId, long reservationId, IFormFile? invoiceFile, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        if (hotelId <= 0 || reservationId <= 0)
        {
            TempData["PartnerError"] = "Geçersiz istek.";
            return Redirect($"/panel/partner/finans/misafir-faturalari?otelId={hotelId}");
        }

        if (invoiceFile is null || invoiceFile.Length <= 0)
        {
            TempData["PartnerError"] = "Yüklenecek bir dosya seçmelisiniz.";
            return Redirect($"/panel/partner/finans/misafir-faturalari?otelId={hotelId}");
        }

        var ext = Path.GetExtension(invoiceFile.FileName);
        var isPdf = string.Equals(invoiceFile.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase);
        var isImage = invoiceFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                      || new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext, StringComparer.OrdinalIgnoreCase);
        if (!isPdf && !isImage)
        {
            TempData["PartnerError"] = "Sadece PDF veya görsel (JPG/PNG/WEBP) yükleyebilirsiniz.";
            return Redirect($"/panel/partner/finans/misafir-faturalari?otelId={hotelId}");
        }

        try
        {
            var stored = await _secureFileService.SaveAsync(invoiceFile, new SecureFileSaveRequest
            {
                ContextTable = "rezervasyonlar",
                ContextId = reservationId,
                OwnerUserId = GetUserId(),
                Category = "guest-invoice",
                VisibilityScope = "private"
            }, cancellationToken);

            var result = await _partnerService.SaveGuestInvoiceAsync(GetUserId(), hotelId, reservationId, stored.FileId, invoiceFile.FileName, invoiceFile.ContentType, cancellationToken);
            TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = $"Fatura yüklenemedi: {ex.Message}";
        }

        return Redirect($"/panel/partner/finans/misafir-faturalari?otelId={hotelId}");
    }

    [HttpGet("finans/komisyonlar")]
    public async Task<IActionResult> Commissions(long? otelId, string? donem, DateTime? dateFrom, DateTime? dateTo, string? paymentStatus, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetFinanceAsync(GetUserId(), otelId, cancellationToken, includeCommissions: true, dateFrom, dateTo, paymentStatus, pageSize, donem);
            ViewData["Title"] = "Komisyonlar";
            ViewData["PageCssPath"] = "paneller/partner/finance";
            return View("~/Views/Paneller/Partner/Commissions.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("finans/komisyonlar/export.csv")]
    public async Task<IActionResult> ExportPartnerCommissionsCsv(long? otelId, string? donem, DateTime? dateFrom, DateTime? dateTo, string? paymentStatus, CancellationToken cancellationToken = default)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var csv = await _partnerService.ExportPartnerCommissionsCsvAsync(GetUserId(), otelId, donem, dateFrom, dateTo, paymentStatus, cancellationToken);
            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var body = System.Text.Encoding.UTF8.GetBytes(csv);
            var bytes = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
            var fileName = $"partner-komisyonlar-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
        catch (InvalidOperationException)
        {
            var hotelQuery = otelId.HasValue ? $"?otelId={otelId.Value}" : string.Empty;
            return Redirect($"/panel/partner/finans/komisyonlar{hotelQuery}");
        }
    }

    [HttpPost("finans/komisyonlar/odendi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCommissionPaidOnline([FromForm] PartnerCommissionMarkPaidRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var (success, message) = await _partnerService.MarkCommissionPaidOnlineAsync(GetUserId(), request.HotelId, request.CommissionRecordId, cancellationToken);
        TempData[success ? "PartnerSuccess" : "PartnerError"] = message;
        if (!string.IsNullOrWhiteSpace(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl))
        {
            return Redirect(request.ReturnUrl);
        }

        return Redirect($"/panel/partner/finans/komisyonlar?otelId={request.HotelId}");
    }

    [HttpGet("finans/mutabakat")]
    public async Task<IActionResult> Reconciliation(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetFinanceAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Mutabakat";
            ViewData["PageCssPath"] = "paneller/partner/finance";
            return View("~/Views/Paneller/Partner/Reconciliation.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("firmalar/rezervasyonlar")]
    public async Task<IActionResult> CompanyReservations(long? otelId, DateTime? dateFrom, DateTime? dateTo, long? companyId, string? status, string? dateRangeMode, bool completedStaysOnly = false, CancellationToken cancellationToken = default)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetCompanyReservationsAsync(GetUserId(), otelId, dateFrom, dateTo, companyId, status, dateRangeMode, completedStaysOnly, cancellationToken);
            ViewData["Title"] = "Firma Rezervasyonları";
            ViewData["PageCssPath"] = "paneller/partner/company-reservations";
            return View("~/Views/Paneller/Partner/CompanyReservations.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("firmalar/analiz")]
    public async Task<IActionResult> CompanyAnalytics(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetCompanyAnalyticsAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Firma Analizleri";
            ViewData["PageCssPath"] = "paneller/partner/company-pricing";
            return View("~/Views/Paneller/Partner/CompanyAnalytics.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("firmalar/talepler")]
    public async Task<IActionResult> CompanyRequests(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetCompanyRequestsAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "İlişkili Kurumsal Firmalar";
            ViewData["PageCssPath"] = "paneller/partner/company-pricing";
            return View("~/Views/Paneller/Partner/CompanyRequests.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("rezervasyonlar/misafir-mesajlari")]
    public async Task<IActionResult> GuestMessages(long? otelId, long? conversationId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetGuestMessagesAsync(GetUserId(), otelId, conversationId, cancellationToken);
            ViewData["Title"] = "Misafir Mesajları";
            ViewData["PageCssPath"] = "paneller/partner/reservations";
            return View("~/Views/Paneller/Partner/GuestMessages.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("rezervasyonlar/takvim")]
    public async Task<IActionResult> ReservationCalendar(long? otelId, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetReservationCalendarAsync(GetUserId(), otelId, dateFrom, dateTo, cancellationToken);
            ViewData["Title"] = "Rezervasyon Takvimi";
            ViewData["PageCssPath"] = "paneller/partner/reservations";
            return View("~/Views/Paneller/Partner/ReservationCalendar.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("rezervasyonlar/iptal-politikalari")]
    public async Task<IActionResult> CancellationNoShow(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetCancellationNoShowAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "İptal & No-show";
            ViewData["PageCssPath"] = "paneller/partner/reservations";
            return View("~/Views/Paneller/Partner/CancellationNoShow.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("rezervasyonlar/odeme-durumlari")]
    public async Task<IActionResult> PaymentStatuses(long? otelId, DateTime? dateFrom, DateTime? dateTo, string? paymentStatus, string? paymentMethod, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetPaymentStatusesAsync(GetUserId(), otelId, dateFrom, dateTo, paymentStatus, paymentMethod, cancellationToken);
            ViewData["Title"] = "Ödeme Durumları";
            ViewData["PageCssPath"] = "paneller/partner/reservations";
            return View("~/Views/Paneller/Partner/PaymentStatuses.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("tesis/ozellikler")]
    public async Task<IActionResult> FacilityAmenities(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetHotelInfoAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Tesis Özellikleri";
            ViewData["PageCssPath"] = "paneller/partner/hotel-info";
            return View("~/Views/Paneller/Partner/FacilityAmenities.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Tesis Özellikleri";
            ViewData["PageCssPath"] = "paneller/partner/hotel-info";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("tesis/ozellikler/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFacilityAmenities(PartnerHotelAmenitiesUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var result = await _partnerService.UpdateHotelAmenitiesAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/tesis/ozellikler?otelId={request.HotelId}#ozellikler");
    }

    [HttpGet("tesis/konum")]
    public async Task<IActionResult> FacilityLocation(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetHotelLocationAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Konum ve Harita";
            ViewData["PageCssPath"] = "paneller/partner/hotel-info";
            return View("~/Views/Paneller/Partner/FacilityLocation.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Konum ve Harita";
            ViewData["PageCssPath"] = "paneller/partner/hotel-info";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("tesis/konum/ilceler")]
    public async Task<IActionResult> LocationDistricts(long cityId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Unauthorized();
        var rows = await _partnerService.GetDistrictOptionsAsync(cityId, cancellationToken);
        return Json(rows);
    }

    [HttpGet("tesis/konum/mahalleler")]
    public async Task<IActionResult> LocationNeighborhoods(long districtId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Unauthorized();
        var rows = await _partnerService.GetNeighborhoodOptionsAsync(districtId, cancellationToken);
        return Json(rows);
    }

    [HttpPost("tesis/konum/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFacilityLocation(PartnerHotelLocationUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");

        if (!Request.HasFormContentType)
        {
            TempData["PartnerError"] = "Form verisi okunamadı.";
            return Redirect($"/panel/partner/tesis/konum?otelId={request.HotelId}#adres");
        }

        var latitudeResult = TryParseCoordinate(Request.Form["Latitude"]);
        if (!latitudeResult.Success)
        {
            TempData["PartnerError"] = "Enlem alanına 40.9060787 gibi geçerli bir koordinat girin.";
            return Redirect($"/panel/partner/tesis/konum?otelId={request.HotelId}#adres");
        }

        var longitudeResult = TryParseCoordinate(Request.Form["Longitude"]);
        if (!longitudeResult.Success)
        {
            TempData["PartnerError"] = "Boylam alanına 29.2809220 gibi geçerli bir koordinat girin.";
            return Redirect($"/panel/partner/tesis/konum?otelId={request.HotelId}#adres");
        }

        request.Latitude = latitudeResult.Value;
        request.Longitude = longitudeResult.Value;

        var result = await _partnerService.UpdateHotelLocationAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/tesis/konum?otelId={request.HotelId}#adres");
    }

    [HttpGet("tesis/yemek")]
    public async Task<IActionResult> MealServices(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetMealServicesAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Yemek Hizmetleri";
            ViewData["PageCssPath"] = "paneller/partner/meal-services";
            return View("~/Views/Paneller/Partner/MealServices.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Yemek Hizmetleri";
            ViewData["PageCssPath"] = "paneller/partner/meal-services";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("tesis/yemek/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMealServices(PartnerMealServicesSaveRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var result = await _partnerService.SaveMealServicesAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/tesis/yemek?otelId={request.HotelId}");
    }

    [HttpGet("tesis/kosullar")]
    public async Task<IActionResult> FacilityPolicies(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetHotelPoliciesAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Otel Koşulları";
            ViewData["PageCssPath"] = "paneller/partner/hotel-info";
            return View("~/Views/Paneller/Partner/FacilityPolicies.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Otel Koşulları";
            ViewData["PageCssPath"] = "paneller/partner/hotel-info";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("tesis/kosullar/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFacilityPolicies(PartnerHotelPoliciesForm request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var result = await _partnerService.UpdateHotelPoliciesAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/tesis/kosullar?otelId={request.HotelId}");
    }

    [HttpGet("oda/ozellikler")]
    public async Task<IActionResult> RoomFeatures(long? otelId, long? roomId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetRoomFeaturesAsync(GetUserId(), otelId, roomId, cancellationToken);
            ViewData["Title"] = "Oda Özellikleri";
            ViewData["PageCssPath"] = "paneller/partner/room-features";
            return View("~/Views/Paneller/Partner/RoomFeatures.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Oda Özellikleri";
            ViewData["PageCssPath"] = "paneller/partner/room-features";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpPost("oda/ozellikler/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRoomFeatures(PartnerRoomFeatureSaveRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var result = await _partnerService.SaveRoomFeaturesAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        var roomQuery = request.RoomId > 0 ? $"&roomId={request.RoomId}" : string.Empty;
        return Redirect($"/panel/partner/oda/ozellikler?otelId={request.HotelId}{roomQuery}");
    }

    [HttpPost("oda/ozellikler/ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRoomFeature(PartnerRoomFeatureAddRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var result = await _partnerService.AddRoomFeatureAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/oda/ozellikler?otelId={request.HotelId}");
    }

    [HttpPost("oda/ozellikler/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRoomFeature(PartnerRoomFeatureToggleRequest request, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var result = await _partnerService.ToggleRoomFeatureAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/oda/ozellikler?otelId={request.HotelId}");
    }

    [HttpGet("tesis/genel-tanimlar")]
    public async Task<IActionResult> FacilityDefinitions(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var dashboard = await _partnerService.GetDashboardAsync(GetUserId(), otelId, cancellationToken: cancellationToken);
            ViewData["PartnerShell"] = dashboard.Shell;
            ViewData["Title"] = "Genel Tanımlar";
            ViewData["PageCssPath"] = "paneller/partner/hotel-info";
            ViewData["ModuleTables"] = GetPlannedModuleTables("/panel/partner/tesis/genel-tanimlar");
            return View("~/Views/Paneller/Partner/FacilityDefinitions.cshtml", dashboard.Shell);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    private static string GetPlannedModuleTitle(string? path)
        => (path ?? string.Empty).ToLowerInvariant() switch
        {
            var p when p.Contains("rezervasyonlar/takvim") => "Rezervasyon Takvimi",
            var p when p.Contains("misafir-mesajlari") => "Misafir Mesajları",
            var p when p.Contains("iptal-politikalari") => "İptal ve No-show Politikaları",
            var p when p.Contains("odeme-durumlari") => "Rezervasyon Ödeme Durumları",
            var p when p.Contains("stok-kontenjan") => "Oda Kontenjanları",
            var p when p.Contains("super-fiyat") => "Süper Fiyat",
            var p when p.Contains("indirimler") => "İndirimler",
            var p when p.Contains("kisitlamalar") => "Fiyat Kuralları ve Kısıtlamalar",
            var p when p.Contains("gunluk-notlar") => "Fiyat Notları",
            var p when p.Contains("tesis/ozellikler") => "Tesis Özellikleri",
            var p when p.Contains("tesis/konum") => "Konum ve Harita",
            var p when p.Contains("tesis/kosullar") => "Otel Koşulları",
            var p when p.Contains("genel-tanimlar") => "Genel Tanımlar",
            var p when p.Contains("oda/ozellikler") => "Oda Özellikleri",
            var p when p.Contains("konum-icgoruleri") => "Konum İçgörüleri",
            var p when p.Contains("favori-misafirler") => "Favori Misafirler",
            var p when p.Contains("etkinlikler") => "Etkinlik Yönetimi",
            var p when p.Contains("odeme/ayarlari") => "Ödeme Ayarları",
            var p when p.Contains("finans/faturalar") => "Faturalar",
            var p when p.Contains("finans/komisyonlar") => "Komisyonlar",
            var p when p.Contains("finans/mutabakat") => "Mutabakat",
            var p when p.Contains("firmalar/rezervasyonlar") => "Firma Rezervasyonları",
            var p when p.Contains("firmalar/analiz") => "Firma Analizleri",
            var p when p.Contains("firmalar/talepler") => "İlişkili Kurumsal Firmalar",
            var p when p.Contains("bildirim-tercihleri") => "Bildirim Tercihleri",
            var p when p.Contains("hesap-bilgileri") => "Hesap Bilgileri",
            _ => "Partner Modülü"
        };

    private static string GetPlannedModuleTables(string? path)
        => (path ?? string.Empty).ToLowerInvariant() switch
        {
            var p when p.Contains("firmalar") => "firma_oda_fiyat_musaitlik, rezervasyonlar, firma_kullanicilari, firmalar",
            var p when p.Contains("rezervasyonlar") => "rezervasyonlar, rezervasyon_durum_tanimlari, rezervasyon_odeme_kalemleri, odeme_islemleri",
            var p when p.Contains("fiyat") => "oda_fiyat_musaitlik, fiyat_indirimleri, firma_oda_fiyat_musaitlik",
            var p when p.Contains("tesis") => "oteller, otel_kosullari, otel_ozellikleri, otel_ozellik_iliskileri, otel_tipleri",
            var p when p.Contains("oda") => "oda_tipleri, oda_ozellikleri, oda_ozellik_iliskileri, oda_gorselleri",
            var p when p.Contains("pazarlama") => "kampanyalar, kampanya_oteller, otel_liste_abonelikleri, otel_istatistikleri, user_favori_oteller",
            var p when p.Contains("finans") || p.Contains("odeme") => "odeme_islemleri, odeme_yontemi_tanimlari, komisyon_muhasebe_kayitlari, komisyon_vergiler",
            var p when p.Contains("bildirim") => "bildirim_loglari, partner_panel_tercihleri",
            _ => "partner_detaylari, otel_kullanici_sahiplikleri, partner_panel_tercihleri"
        };

    private static string? NormalizePartnerLocalPath(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return null;
        var t = returnUrl.Trim();
        if (!t.StartsWith("/panel/partner/", StringComparison.OrdinalIgnoreCase)) return null;
        if (t.Contains("://", StringComparison.Ordinal) || t.Contains('\n') || t.Contains('\r')) return null;
        return t.Split('?', 2)[0];
    }

    private bool IsPartnerUser()
    {
        var accountType = User.FindFirst(AuthClaimTypes.AccountType)?.Value;
        return string.Equals(accountType, "partner", StringComparison.OrdinalIgnoreCase);
    }

    private long GetUserId()
        => long.Parse(User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0", System.Globalization.CultureInfo.InvariantCulture);
}
