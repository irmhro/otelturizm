using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Models.Paneller.Developer;
using otelturizmnew.Models.TelefonDogrulama;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Admin;

[Authorize]
[Route("admin")]
public class AdminPanelController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IAdminHotelManagementService _adminHotelManagementService;
    private readonly IContractContentService _contractContentService;
    private readonly IDevelopmentRequestService _developmentRequestService;
    private readonly IPhoneVerificationService _phoneVerificationService;

    public AdminPanelController(IAdminService adminService, IAdminHotelManagementService adminHotelManagementService, IContractContentService contractContentService, IDevelopmentRequestService developmentRequestService, IPhoneVerificationService phoneVerificationService)
    {
        _adminService = adminService;
        _adminHotelManagementService = adminHotelManagementService;
        _contractContentService = contractContentService;
        _developmentRequestService = developmentRequestService;
        _phoneVerificationService = phoneVerificationService;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetDashboardAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = "Admin Dashboard";
        ViewData["PageCss"] = "panel-admin-dashboard";
        return View("~/Views/Paneller/Admin/Dashboard.cshtml", model);
    }

    [HttpGet("kullanicilar")]
    public Task<IActionResult> Users(CancellationToken cancellationToken) => RenderSectionAsync("users", "Users", cancellationToken);

    [HttpGet("yoneticiler")]
    public Task<IActionResult> Managers(CancellationToken cancellationToken) => RenderSectionAsync("managers", "Managers", cancellationToken);

    [HttpGet("oteller")]
    public async Task<IActionResult> Hotels([FromQuery] string? q, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminHotelManagementService.GetHotelsPageAsync(GetFullName(), GetEmail(), GetUserRole(), q, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCss"] = "panel-admin-hotels";
        return View("~/Views/Paneller/Admin/Hotels.cshtml", model);
    }

    [HttpGet("otel-detay/{id:long?}")]
    public async Task<IActionResult> HotelDetail(long? id, [FromQuery] long? roomId, [FromQuery] long? hotelPhotoId, [FromQuery] long? roomPhotoId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!id.HasValue)
        {
            TempData["AdminHotelError"] = "Duzenlemek icin bir otel secmelisiniz.";
            return RedirectToAction(nameof(Hotels));
        }

        var model = await _adminHotelManagementService.GetHotelManagementPageAsync(id.Value, GetFullName(), GetEmail(), GetUserRole(), roomId, hotelPhotoId, roomPhotoId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCss"] = "panel-admin-hotels";
        return View("~/Views/Paneller/Admin/HotelDetail.cshtml", model);
    }

    [HttpGet("oteller/duzenle/{id:long}")]
    public IActionResult EditHotel(long id, [FromQuery] long? roomId, [FromQuery] long? hotelPhotoId, [FromQuery] long? roomPhotoId)
    {
        return RedirectToAction(nameof(HotelDetail), new { id, roomId, hotelPhotoId, roomPhotoId });
    }

    [HttpPost("oteller/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHotel(AdminHotelEditForm request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.SaveHotelAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = request.HotelId });
    }

    [HttpPost("oteller/oda-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRoom(AdminRoomEditForm request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.SaveRoomAsync(request, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = request.HotelId, roomId = request.RoomId });
    }

    [HttpPost("oteller/oda-pasife-al")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateRoom(long hotelId, long roomId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.DeactivateRoomAsync(hotelId, roomId, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = hotelId });
    }

    [HttpPost("oteller/pasife-al")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateHotel(long hotelId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!CanPerformCriticalAdminActions())
        {
            TempData["AdminHotelError"] = "Bu islem yalnizca admin yetkisi ile yapilabilir.";
            return RedirectToAction(nameof(HotelDetail), new { id = hotelId });
        }

        var result = await _adminHotelManagementService.DeactivateHotelAsync(hotelId, GetUserId(), cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = hotelId });
    }

    [HttpPost("oteller/aktive-et")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateHotel(long hotelId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!CanPerformCriticalAdminActions())
        {
            TempData["AdminHotelError"] = "Bu islem yalnizca admin yetkisi ile yapilabilir.";
            return RedirectToAction(nameof(HotelDetail), new { id = hotelId });
        }

        var result = await _adminHotelManagementService.ActivateHotelAsync(hotelId, GetUserId(), cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = hotelId });
    }

    [HttpPost("oteller/otel-fotograf-yukle")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 314572800)]
    [RequestSizeLimit(314572800)]
    public async Task<IActionResult> UploadHotelPhotos(AdminHotelPhotoUploadForm request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.UploadHotelPhotosAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = request.HotelId });
    }

    [HttpPost("oteller/otel-fotograf-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateHotelPhoto(AdminHotelPhotoEditForm request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.UpdateHotelPhotoAsync(request, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = request.HotelId, hotelPhotoId = request.PhotoId });
    }

    [HttpPost("oteller/otel-fotograf-kapak-yap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetHotelCover(long hotelId, long photoId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.SetHotelCoverAsync(hotelId, photoId, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = hotelId });
    }

    [HttpPost("oteller/otel-fotograf-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHotelPhoto(long hotelId, long photoId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.DeleteHotelPhotoAsync(hotelId, photoId, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = hotelId });
    }

    [HttpPost("oteller/oda-fotograf-yukle")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 314572800)]
    [RequestSizeLimit(314572800)]
    public async Task<IActionResult> UploadRoomPhotos(AdminRoomPhotoUploadForm request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.UploadRoomPhotosAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = request.HotelId, roomId = request.RoomId });
    }

    [HttpPost("oteller/oda-fotograf-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoomPhoto(AdminRoomPhotoEditForm request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.UpdateRoomPhotoAsync(request, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = request.HotelId, roomId = request.RoomId, roomPhotoId = request.PhotoId });
    }

    [HttpPost("oteller/oda-fotograf-kapak-yap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRoomCover(long hotelId, long roomId, long photoId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.SetRoomCoverAsync(hotelId, roomId, photoId, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = hotelId, roomId });
    }

    [HttpPost("oteller/oda-fotograf-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoomPhoto(long hotelId, long roomId, long photoId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminHotelManagementService.DeleteRoomPhotoAsync(hotelId, roomId, photoId, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        return RedirectToAction(nameof(HotelDetail), new { id = hotelId, roomId });
    }

    [HttpGet("rezervasyonlar")]
    public Task<IActionResult> Reservations(CancellationToken cancellationToken) => RenderSectionAsync("reservations", "Reservations", cancellationToken);

    [HttpGet("odemeler")]
    public Task<IActionResult> Payments(CancellationToken cancellationToken) => RenderSectionAsync("payments", "Payments", cancellationToken);

    [HttpGet("faturalar")]
    public Task<IActionResult> Invoices(CancellationToken cancellationToken) => RenderSectionAsync("invoices", "Invoices", cancellationToken);

    [HttpGet("komisyonlar")]
    public async Task<IActionResult> Commissions([FromQuery] long? hotelId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetCommissionManagementAsync(GetFullName(), GetEmail(), GetUserRole(), hotelId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCss"] = "panel-admin-commissions";
        return View("~/Views/Paneller/Admin/Commissions.cshtml", model);
    }

    [HttpGet("sozlesmeler")]
    public async Task<IActionResult> Contracts([FromQuery] long? contractId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _contractContentService.GetAdminContractManagementAsync(GetFullName(), GetEmail(), GetUserRole(), contractId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCss"] = "panel-admin-contracts";
        return View("~/Views/Paneller/Admin/Contracts.cshtml", model);
    }

    [HttpPost("sozlesmeler/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveContract(AdminContractForm request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _contractContentService.SaveContractAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(Contracts), new { contractId = request.ContractId });
    }

    [HttpPost("sozlesmeler/yeniden-gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendContract(long contractId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _contractContentService.ResendContractBundleAsync(GetUserId(), contractId, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(Contracts), new { contractId });
    }

    [HttpGet("sozlesmeler/onizleme")]
    public async Task<IActionResult> ContractPreview([FromQuery] long contractId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return Unauthorized();
        }

        var preview = await _contractContentService.GetAdminContractPreviewAsync(contractId, cancellationToken);
        if (preview is null)
        {
            return NotFound(new { title = "Sözleşme", html = "<div class=\"text-muted\">İçerik bulunamadı.</div>" });
        }

        return Json(new { title = preview.Value.Title, html = preview.Value.Html });
    }

    [HttpPost("sozlesmeler/pdf-yukle")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    [RequestSizeLimit(52428800)]
    public async Task<IActionResult> UploadContractPdf([FromForm] long contractId, IFormFile? pdfFile, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (contractId <= 0)
        {
            TempData["AdminError"] = "PDF yüklenecek sözleşme bulunamadı.";
            return RedirectToAction(nameof(Contracts));
        }

        if (pdfFile is null || pdfFile.Length <= 0)
        {
            TempData["AdminError"] = "Yüklenecek bir PDF seçmelisiniz.";
            return RedirectToAction(nameof(Contracts), new { contractId });
        }

        if (!string.Equals(pdfFile.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            && !Path.GetExtension(pdfFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminError"] = "Sadece PDF dosyası yükleyebilirsiniz.";
            return RedirectToAction(nameof(Contracts), new { contractId });
        }

        var safeFileName = $"{Guid.NewGuid():N}.pdf";
        var targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contracts", contractId.ToString());
        Directory.CreateDirectory(targetDirectory);
        var physicalPath = Path.Combine(targetDirectory, safeFileName);
        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await pdfFile.CopyToAsync(stream, cancellationToken);
        }

        // DB kaydı migration ile eklenecek tabloya yazılır. Şema yoksa yükleme yine de dosyayı saklar.
        var relativeUrl = $"/uploads/contracts/{contractId}/{safeFileName}";
        try
        {
            var connectionString = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    IF OBJECT_ID('dbo.sozlesme_dosyalari', 'U') IS NOT NULL
                    BEGIN
                        INSERT INTO sozlesme_dosyalari (sozlesme_id, dosya_tipi, dosya_adi, dosya_yolu, mime_tipi, olusturulma_tarihi)
                        VALUES (@contractId, 'pdf', @fileName, @fileUrl, 'application/pdf', SYSUTCDATETIME());
                    END", connection);
                command.Parameters.AddWithValue("@contractId", contractId);
                command.Parameters.AddWithValue("@fileName", Path.GetFileName(pdfFile.FileName));
                command.Parameters.AddWithValue("@fileUrl", relativeUrl);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch
        {
            // Migration henüz uygulanmamış olabilir; dosya yine de fiziksel olarak saklandı.
        }

        TempData["AdminMessage"] = "PDF yüklendi.";
        return RedirectToAction(nameof(Contracts), new { contractId });
    }

    [HttpPost("komisyonlar/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCommissionRule(AdminCommissionRuleForm request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminService.SaveCommissionRuleAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(Commissions), new { hotelId = request.HotelId });
    }

    [HttpGet("partner-basvurulari")]
    public async Task<IActionResult> PartnerApplications(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetPartnerApplicationsAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCss"] = "panel-admin-partner-applications";
        return View("~/Views/Paneller/Admin/PartnerApplications.cshtml", model);
    }

    [HttpPost("partner-basvurulari/durum")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePartnerApplicationStatus(AdminPartnerApplicationDecisionRequest request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminService.ReviewPartnerApplicationAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(PartnerApplications));
    }

    [HttpGet("firma-basvurulari")]
    public Task<IActionResult> CompanyApplications(CancellationToken cancellationToken) => RenderSectionAsync("company-applications", "CompanyApplications", cancellationToken);

    [HttpGet("gelistirme-talepleri")]
    public async Task<IActionResult> DevelopmentRequests([FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] long? developerUserId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _developmentRequestService.GetAdminPageAsync(GetFullName(), GetEmail(), GetUserRole(), q, status, priority, developerUserId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCss"] = "panel-admin-development";
        return View("~/Views/Paneller/Admin/DevelopmentRequests.cshtml", model);
    }

    [HttpPost("gelistirme-talepleri/kaydet")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    [RequestSizeLimit(52428800)]
    public async Task<IActionResult> SaveDevelopmentRequest(AdminDevelopmentRequestUpdateForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        string? imageUrl = null;
        if (form.VisualFile is not null && form.VisualFile.Length > 0)
        {
            var fileName = $"{Guid.NewGuid():N}-{Path.GetFileName(form.VisualFile.FileName)}";
            var targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "developer", "admin");
            Directory.CreateDirectory(targetDirectory);
            var physicalPath = Path.Combine(targetDirectory, fileName);
            await using var stream = System.IO.File.Create(physicalPath);
            await form.VisualFile.CopyToAsync(stream, cancellationToken);
            imageUrl = $"/uploads/developer/admin/{fileName}";
        }

        var result = await _developmentRequestService.SaveAdminRequestAsync(GetUserId(), form, imageUrl, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(DevelopmentRequests));
    }

    [HttpPost("gelistirme-talepleri/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDevelopmentRequest(long requestId, string? note, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _developmentRequestService.DeleteRequestAsync(GetUserId(), requestId, note, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(DevelopmentRequests));
    }

    [HttpGet("platform-yetkilileri")]
    public Task<IActionResult> PlatformOfficials(CancellationToken cancellationToken) => RenderSectionAsync("platform-officials", "PlatformOfficials", cancellationToken);

    [HttpGet("acik-oteller")]
    public Task<IActionResult> ActiveHotels(CancellationToken cancellationToken) => RenderSectionAsync("active-hotels", "ActiveHotels", cancellationToken);

    [HttpGet("bekleyen-oteller")]
    public Task<IActionResult> PendingHotels(CancellationToken cancellationToken) => RenderSectionAsync("pending-hotels", "PendingHotels", cancellationToken);

    [HttpGet("degerlendirmeler")]
    public Task<IActionResult> Reviews(CancellationToken cancellationToken) => RenderSectionAsync("reviews", "Reviews", cancellationToken);

    [HttpGet("raporlar")]
    public Task<IActionResult> Reports(CancellationToken cancellationToken) => RenderSectionAsync("reports", "Reports", cancellationToken);

    [HttpGet("kampanyalar")]
    public Task<IActionResult> Campaigns(CancellationToken cancellationToken) => RenderSectionAsync("campaigns", "Campaigns", cancellationToken);

    [HttpGet("bildirimler")]
    public Task<IActionResult> Notifications(CancellationToken cancellationToken) => RenderSectionAsync("notifications", "Notifications", cancellationToken);

    [HttpGet("ayarlar")]
    public Task<IActionResult> Settings(CancellationToken cancellationToken) => RenderSectionAsync("settings", "Settings", cancellationToken);

    [HttpGet("whatsapp-cloud-api")]
    public async Task<IActionResult> WhatsAppCloudApi(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var shell = await _adminService.GetSectionPageAsync("settings", GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        var model = await _phoneVerificationService.GetAdminSettingsPageAsync(shell.Shell, cancellationToken);
        ViewData["Title"] = "WhatsApp Cloud API";
        return View("~/Views/Paneller/Admin/WhatsAppCloudApi.cshtml", model);
    }

    [HttpPost("whatsapp-cloud-api/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWhatsAppCloudApi(AdminWhatsAppCloudApiSettingsForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _phoneVerificationService.SaveAdminSettingsAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(WhatsAppCloudApi));
    }

    [HttpPost("whatsapp-cloud-api/test-gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendWhatsAppCloudApiTest(string phoneNumber, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _phoneVerificationService.SendAdminTestMessageAsync(GetUserId(), phoneNumber, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(WhatsAppCloudApi));
    }

    [HttpGet("guvenlik")]
    public Task<IActionResult> Security(CancellationToken cancellationToken) => RenderSectionAsync("security", "Security", cancellationToken);

    [HttpGet("blog")]
    public Task<IActionResult> Blog(CancellationToken cancellationToken) => RenderSectionAsync("blog", "Blog", cancellationToken);

    [HttpGet("eposta-sablonlari")]
    public Task<IActionResult> EmailTemplates(CancellationToken cancellationToken) => RenderSectionAsync("email-templates", "EmailTemplates", cancellationToken);

    [HttpGet("sss")]
    public Task<IActionResult> Faq(CancellationToken cancellationToken) => RenderSectionAsync("faq", "Faq", cancellationToken);

    [HttpGet("sikayetler")]
    public Task<IActionResult> Complaints(CancellationToken cancellationToken) => RenderSectionAsync("complaints", "Complaints", cancellationToken);

    [HttpGet("log-kayitlari")]
    public Task<IActionResult> Logs(CancellationToken cancellationToken) => RenderSectionAsync("logs", "Logs", cancellationToken);

    [HttpGet("yedekleme")]
    public Task<IActionResult> Backups(CancellationToken cancellationToken) => RenderSectionAsync("backups", "Backups", cancellationToken);

    private async Task<IActionResult> RenderSectionAsync(string sectionKey, string viewName, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetSectionPageAsync(sectionKey, GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCss"] = string.Equals(sectionKey, "users", StringComparison.OrdinalIgnoreCase)
            ? "panel-admin-users"
            : "panel-admin-section";
        return View($"~/Views/Paneller/Admin/{viewName}.cshtml", model);
    }

    private bool CanAccessAdminPanel()
    {
        var accountType = User.FindFirstValue(AuthClaimTypes.AccountType);
        var userRole = User.FindFirstValue(AuthClaimTypes.UserRole);
        return string.Equals(accountType, "admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase)
            || User.IsInRole("superadmin")
            || User.IsInRole("admin");
    }

    private bool CanPerformCriticalAdminActions()
    {
        var userRole = User.FindFirstValue(AuthClaimTypes.UserRole);
        return string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(userRole, "superadmin", StringComparison.OrdinalIgnoreCase)
            || User.IsInRole("admin")
            || User.IsInRole("superadmin");
    }

    private string GetFullName()
    {
        return User.FindFirstValue(AuthClaimTypes.FullName) ?? User.Identity?.Name ?? "Admin Kullanici";
    }

    private string GetEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(AuthClaimTypes.Email) ?? "-";
    }

    private string GetUserRole()
    {
        return User.FindFirstValue(AuthClaimTypes.UserRole) ?? "admin";
    }

    private long GetUserId()
    {
        var rawValue = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(rawValue, out var userId) ? userId : 0;
    }
}
