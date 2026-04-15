using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller.Partner;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Partner;

[Authorize]
[Route("panel/partner")]
public class PartnerPanelController : Controller
{
    private readonly IPartnerService _partnerService;

    public PartnerPanelController(IPartnerService partnerService)
    {
        _partnerService = partnerService;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _partnerService.GetDashboardAsync(GetUserId(), otelId, cancellationToken);
        ViewData["Title"] = "Partner Dashboard";
        ViewData["PageCssPath"] = "paneller/partner/dashboard";
        return View("~/Views/Paneller/Partner/Dashboard.cshtml", model);
    }

    [HttpGet("rezervasyonlar")]
    public async Task<IActionResult> Reservations(long? otelId, DateTime? dateFrom, DateTime? dateTo, string? status, string? paymentMethod, int page = 1, int pageSize = 10, long? conversationId = null, CancellationToken cancellationToken = default)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _partnerService.GetReservationsAsync(GetUserId(), otelId, dateFrom, dateTo, status, paymentMethod, page, pageSize, conversationId, cancellationToken);
        ViewData["Title"] = "Partner Rezervasyonlar";
        ViewData["PageCssPath"] = "paneller/partner/reservations";
        return View("~/Views/Paneller/Partner/Reservations.cshtml", model);
    }

    [HttpPost("rezervasyonlar/durum")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReservationStatus(PartnerReservationStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.UpdateReservationStatusAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/rezervasyonlar?otelId={request.HotelId}");
    }

    [HttpPost("rezervasyonlar/misafire-mesaj")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendGuestMessage(PartnerGuestMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.SendGuestMessageAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
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
        var model = await _partnerService.GetPricingAsync(GetUserId(), otelId, roomId, month, cancellationToken);
        ViewData["Title"] = "Partner Takvim ve Fiyatlar";
        ViewData["PageCssPath"] = "paneller/partner/pricing";
        return View("~/Views/Paneller/Partner/Pricing.cshtml", model);
    }

    [HttpGet("kampanyalar")]
    public async Task<IActionResult> Campaigns(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _partnerService.GetCampaignsAsync(GetUserId(), otelId, cancellationToken);
        ViewData["Title"] = "Partner Kampanyalar";
        ViewData["PageCssPath"] = "paneller/partner/campaigns";
        return View("~/Views/Paneller/Partner/Campaigns.cshtml", model);
    }

    [HttpPost("kampanyalar/katil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinCampaign(PartnerCampaignJoinRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.JoinCampaignAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/kampanyalar?otelId={request.HotelId}");
    }

    [HttpPost("kampanyalar/ayril")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LeaveCampaign(long hotelId, long campaignId, CancellationToken cancellationToken)
    {
        var result = await _partnerService.LeaveCampaignAsync(GetUserId(), hotelId, campaignId, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/kampanyalar?otelId={hotelId}");
    }

    [HttpPost("takvim-fiyatlar/toplu-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyBulkPricing(PartnerBulkPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.ApplyBulkPricingAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        var roomId = request.ViewRoomId ?? request.RoomId ?? request.SelectedRoomIds.FirstOrDefault();
        var roomQuery = roomId > 0 ? $"&roomId={roomId}" : string.Empty;
        var monthQuery = !string.IsNullOrWhiteSpace(request.ViewMonth) ? $"&month={Uri.EscapeDataString(request.ViewMonth)}" : string.Empty;
        return Redirect($"/panel/partner/takvim-fiyatlar?otelId={request.HotelId}{roomQuery}{monthQuery}");
    }

    [HttpPost("takvim-fiyatlar/gun-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyDailyPricing(PartnerDailyPricingUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.ApplyDailyPricingAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        var monthQuery = !string.IsNullOrWhiteSpace(request.ViewMonth) ? $"&month={Uri.EscapeDataString(request.ViewMonth)}" : string.Empty;
        return Redirect($"/panel/partner/takvim-fiyatlar?otelId={request.HotelId}&roomId={request.RoomId}{monthQuery}");
    }

    [HttpGet("oda-yonetimi")]
    public async Task<IActionResult> Rooms(long? otelId, long? roomId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _partnerService.GetRoomsAsync(GetUserId(), otelId, roomId, cancellationToken);
        ViewData["Title"] = "Partner Oda Yonetimi";
        ViewData["PageCssPath"] = "paneller/partner/rooms";
        return View("~/Views/Paneller/Partner/Rooms.cshtml", model);
    }

    [HttpPost("oda-yonetimi/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRoom(PartnerRoomUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.UpsertRoomAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        if (request.RoomId.HasValue && request.RoomId.Value > 0)
        {
            return Redirect($"/panel/partner/oda-yonetimi?otelId={request.HotelId}&roomId={request.RoomId.Value}#room-form");
        }

        return Redirect($"/panel/partner/oda-yonetimi?otelId={request.HotelId}#room-form");
    }

    [HttpPost("oda-yonetimi/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoom(long hotelId, long roomId, CancellationToken cancellationToken)
    {
        var result = await _partnerService.DeleteRoomAsync(GetUserId(), hotelId, roomId, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/oda-yonetimi?otelId={hotelId}");
    }

    [HttpPost("oda-yonetimi/gorsel-yukle")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 157286400)]
    [RequestSizeLimit(157286400)]
    public async Task<IActionResult> UploadRoomPhotos(PartnerRoomPhotoUploadRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.UploadRoomPhotosAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/oda-yonetimi?otelId={request.HotelId}&roomId={request.RoomId}#room-gallery");
    }

    [HttpPost("oda-yonetimi/gorsel-kapak-yap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRoomCover(long hotelId, long roomId, long photoId, CancellationToken cancellationToken)
    {
        var result = await _partnerService.SetRoomCoverAsync(GetUserId(), hotelId, roomId, photoId, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/oda-yonetimi?otelId={hotelId}&roomId={roomId}#room-gallery");
    }

    [HttpPost("oda-yonetimi/gorsel-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoomPhoto(long hotelId, long roomId, long photoId, CancellationToken cancellationToken)
    {
        var result = await _partnerService.DeleteRoomPhotoAsync(GetUserId(), hotelId, roomId, photoId, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/oda-yonetimi?otelId={hotelId}&roomId={roomId}#room-gallery");
    }

    [HttpGet("otel-bilgileri")]
    public async Task<IActionResult> HotelInfo(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _partnerService.GetHotelInfoAsync(GetUserId(), otelId, cancellationToken);
        ViewData["Title"] = "Partner Otel Bilgileri";
        ViewData["PageCssPath"] = "paneller/partner/hotel-info";
        return View("~/Views/Paneller/Partner/HotelInfo.cshtml", model);
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
        var model = await _partnerService.GetPhotosAsync(GetUserId(), otelId, photoId, cancellationToken);
        ViewData["Title"] = "Partner Fotograflar";
        ViewData["PageCssPath"] = "paneller/partner/photos";
        return View("~/Views/Paneller/Partner/Photos.cshtml", model);
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
        return Redirect($"/panel/partner/fotograflar?otelId={request.HotelId}");
    }

    [HttpPost("fotograflar/kapak-yap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCover(long hotelId, long photoId, CancellationToken cancellationToken)
    {
        var result = await _partnerService.SetCoverPhotoAsync(GetUserId(), hotelId, photoId, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/fotograflar?otelId={hotelId}");
    }

    [HttpPost("fotograflar/guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePhoto(PartnerPhotoEditForm request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.UpdatePhotoAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/fotograflar?otelId={request.HotelId}");
    }

    [HttpPost("fotograflar/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(long hotelId, long photoId, CancellationToken cancellationToken)
    {
        var result = await _partnerService.DeletePhotoAsync(GetUserId(), hotelId, photoId, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/fotograflar?otelId={hotelId}");
    }

    [HttpPost("fotograflar/toplu-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeletePhotos(PartnerPhotoBulkDeleteRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.BulkDeletePhotosAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/fotograflar?otelId={request.HotelId}");
    }

    [HttpGet("performans")]
    public async Task<IActionResult> Performance(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _partnerService.GetPerformanceAsync(GetUserId(), otelId, cancellationToken);
        ViewData["Title"] = "Partner Performans";
        ViewData["PageCssPath"] = "paneller/partner/performance";
        return View("~/Views/Paneller/Partner/Performance.cshtml", model);
    }

    [HttpPost("performans/rakip-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCompetitor(PartnerCompetitorUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.SaveCompetitorAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
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
        var model = await _partnerService.GetReviewsAsync(GetUserId(), otelId, cancellationToken);
        ViewData["Title"] = "Partner Degerlendirmeler";
        ViewData["PageCssPath"] = "paneller/partner/reviews";
        return View("~/Views/Paneller/Partner/Reviews.cshtml", model);
    }

    [HttpPost("degerlendirmeler/yanitla")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyReview(PartnerReviewReplyRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.ReplyToReviewAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/degerlendirmeler?otelId={request.HotelId}");
    }

    [HttpPost("degerlendirmeler/raporla")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportReview(PartnerReviewReportRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.ReportReviewAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/degerlendirmeler?otelId={request.HotelId}");
    }

    [HttpGet("finans")]
    public async Task<IActionResult> Finance(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _partnerService.GetFinanceAsync(GetUserId(), otelId, cancellationToken);
        ViewData["Title"] = "Partner Finans";
        ViewData["PageCssPath"] = "paneller/partner/finance";
        return View("~/Views/Paneller/Partner/Finance.cshtml", model);
    }

    [HttpPost("finans/banka-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBankInfo(PartnerBankInfoForm request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.SaveBankInfoAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
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
    public async Task<IActionResult> Preferences(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _partnerService.GetPreferencesAsync(GetUserId(), otelId, cancellationToken);
        ViewData["Title"] = "Partner Tercihler";
        ViewData["PageCssPath"] = "paneller/partner/preferences";
        return View("~/Views/Paneller/Partner/Preferences.cshtml", model);
    }

    [HttpPost("tercihler/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreferences(PartnerPreferencesForm request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.SavePreferencesAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
        return Redirect($"/panel/partner/tercihler?otelId={request.HotelId}");
    }

    [HttpGet("724-destek")]
    public async Task<IActionResult> Support(long? otelId, CancellationToken cancellationToken)
    {
        if (!IsPartnerUser()) return Redirect("/partner-giris");
        var model = await _partnerService.GetSupportAsync(GetUserId(), otelId, cancellationToken);
        ViewData["Title"] = "Partner 7/24 Destek";
        ViewData["PageCssPath"] = "paneller/partner/support";
        return View("~/Views/Paneller/Partner/Support.cshtml", model);
    }

    [HttpPost("724-destek/talep-olustur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(PartnerSupportCreateTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await _partnerService.CreateSupportTicketAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "PartnerSuccess" : "PartnerError"] = result.Message;
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
