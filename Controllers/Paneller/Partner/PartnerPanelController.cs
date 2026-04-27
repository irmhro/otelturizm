using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller.Partner;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Partner;

[Authorize]
[Route("panel/partner")]
public class PartnerPanelController : Controller
{
    private readonly IPartnerService _partnerService;
    private readonly IAuthService _authService;
    private readonly IOutputCacheStore _outputCacheStore;

    public PartnerPanelController(IPartnerService partnerService, IAuthService authService, IOutputCacheStore outputCacheStore)
    {
        _partnerService = partnerService;
        _authService = authService;
        _outputCacheStore = outputCacheStore;
    }

    private async Task EvictPublicOutputCacheAsync(CancellationToken cancellationToken)
    {
        await _outputCacheStore.EvictByTagAsync("public", cancellationToken);
        await _outputCacheStore.EvictByTagAsync("public-short", cancellationToken);
        await _outputCacheStore.EvictByTagAsync("public-medium", cancellationToken);
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetDashboardAsync(GetUserId(), otelId, cancellationToken);
            ViewData["Title"] = "Partner Paneli";
            ViewData["PageCssPath"] = "paneller/partner/dashboard";
            return View("~/Views/Paneller/Partner/Dashboard.cshtml", model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("yetkili otel", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Title"] = "Partner Paneli";
            ViewData["PageCssPath"] = "paneller/partner/dashboard";
            return View("~/Views/Paneller/Partner/NoHotelAssigned.cshtml");
        }
    }

    [HttpGet("guvenlik")]
    public async Task<IActionResult> Security(CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var dashboard = await _partnerService.GetDashboardAsync(GetUserId(), null, cancellationToken);
        ViewData["PartnerShell"] = dashboard.Shell;
        var model = await _authService.GetTwoFactorSecurityAsync(GetUserId(), "partner", cancellationToken);
        ViewData["Title"] = "Partner Güvenlik";
        ViewData["PageCssPath"] = "panel-user-security";
        return View("~/Views/Paneller/Partner/Security.cshtml", model);
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

    [HttpGet("rezervasyonlar")]
    public async Task<IActionResult> Reservations(long? otelId, DateTime? dateFrom, DateTime? dateTo, string? status, string? paymentMethod, int page = 1, int pageSize = 10, long? conversationId = null, CancellationToken cancellationToken = default)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetReservationsAsync(GetUserId(), otelId, dateFrom, dateTo, status, paymentMethod, page, pageSize, conversationId, cancellationToken);
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

        var conversationQuery = request.ConversationId.HasValue && request.ConversationId.Value > 0
            ? $"&conversationId={request.ConversationId.Value}"
            : string.Empty;
        return Redirect($"/panel/partner/rezervasyonlar?otelId={request.HotelId}{conversationQuery}");
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
            ViewData["Title"] = "Firmalara Özel Fiyatlar";
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
            return Redirect($"/panel/partner/firma-fiyatlari?otelId={request.HotelId}&companyId={request.CompanyId}&roomId={request.RoomTypeId}&month={request.StartDate:yyyy-MM}");
        }
        catch (Exception ex)
        {
            TempData["PartnerError"] = "Firma fiyatları kaydedilemedi: " + ex.Message;
            return Redirect("/panel/partner/firma-fiyatlari");
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
        var result = await _partnerService.UpdateHotelInfoAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/otel-bilgileri?otelId={request.HotelId}");
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
    public async Task<IActionResult> Reviews(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetReviewsAsync(GetUserId(), otelId, cancellationToken);
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
    [HttpGet("basvuru-ve-evraklar")]
    public async Task<IActionResult> Preferences(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetApplicationAsync(GetUserId(), otelId, cancellationToken);
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

    [HttpPost("tercihler/kaydet")]
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
    public async Task<IActionResult> Support(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        try
        {
            var model = await _partnerService.GetSupportAsync(GetUserId(), otelId, cancellationToken);
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

    private bool IsPartnerUser()
    {
        var accountType = User.FindFirst(AuthClaimTypes.AccountType)?.Value;
        return string.Equals(accountType, "partner", StringComparison.OrdinalIgnoreCase);
    }

    private long GetUserId()
        => long.Parse(User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0", System.Globalization.CultureInfo.InvariantCulture);
}
