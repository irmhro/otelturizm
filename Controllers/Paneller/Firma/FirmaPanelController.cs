using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Paneller.Firma;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Firma;

[Authorize]
[Route("panel/firma")]
public class FirmaPanelController : Controller
{
    private readonly IFirmaService _firmaService;
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly IAuthService _authService;

    public FirmaPanelController(IFirmaService firmaService, IPhoneVerificationService phoneVerificationService, IAuthService authService)
    {
        _firmaService = firmaService;
        _phoneVerificationService = phoneVerificationService;
        _authService = authService;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index([FromQuery] bool openPhoneVerification = false, [FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var model = await _firmaService.GetDashboardAsync(GetUserId(), cancellationToken);
        model.PhoneVerification = await _phoneVerificationService.GetUserStatusAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        if (openPhoneVerification && !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            TempData["FirmaSuccess"] = "Telefon doğrulamanızı tamamladıktan sonra rezervasyon akışına dönebilirsiniz.";
        }
        ViewData["Title"] = "Firma Dashboard";
        ViewData["PageCssPath"] = "paneller/firma/dashboard";
        return View("~/Views/Paneller/Firma/Dashboard.cshtml", model);
    }

    [HttpGet("guvenlik")]
    public async Task<IActionResult> Security(CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var dashboard = await _firmaService.GetDashboardAsync(GetUserId(), cancellationToken);
        ApplyFeedback(dashboard.Shell);
        ViewData["FirmaShell"] = dashboard.Shell;
        var model = await _authService.GetTwoFactorSecurityAsync(GetUserId(), "firma", cancellationToken);
        ViewData["Title"] = "Güvenlik";
        ViewData["PageCssPath"] = "panel-user-security";
        return View("~/Views/Paneller/Firma/Security.cshtml", model);
    }

    [HttpPost("guvenlik/iki-asamali")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTwoFactor(otelturizmnew.Models.Paneller.User.UserTwoFactorForm form, CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var result = await _authService.SaveTwoFactorSecurityAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "FirmaSuccess" : "FirmaError"] = result.Message;
        return Redirect("/panel/firma/guvenlik");
    }

    [HttpGet("firma-fiyatlari")]
    public async Task<IActionResult> Deals(string? city = null, int? minRoomCount = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var model = await _firmaService.GetDealsAsync(GetUserId(), city, minRoomCount, search, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Firma Fiyatları";
        ViewData["PageCssPath"] = "paneller/firma/deals";
        return View("~/Views/Paneller/Firma/Deals.cshtml", model);
    }

    [HttpGet("rezervasyonlar")]
    public async Task<IActionResult> Reservations(CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var model = await _firmaService.GetReservationsAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Firma Rezervasyonları";
        ViewData["PageCssPath"] = "paneller/firma/reservations";
        return View("~/Views/Paneller/Firma/Reservations.cshtml", model);
    }

    [HttpGet("mesajlar")]
    public async Task<IActionResult> Messages([FromQuery] long? conversationId, CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var model = await _firmaService.GetMessagesAsync(GetUserId(), conversationId, cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Firma Mesajları";
        ViewData["PageCssPath"] = "panel-user-messages";
        return View("~/Views/Paneller/Firma/Messages.cshtml", model);
    }

    [HttpGet("calisanlar")]
    public async Task<IActionResult> Employees(CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var model = await _firmaService.GetEmployeesAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Çalışanlar";
        ViewData["PageCssPath"] = "paneller/firma/employees";
        return View("~/Views/Paneller/Firma/Employees.cshtml", model);
    }

    [HttpGet("limitler-onaylar")]
    public async Task<IActionResult> Limits(CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var model = await _firmaService.GetLimitsAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Limitler & Onaylar";
        ViewData["PageCssPath"] = "paneller/firma/limits";
        return View("~/Views/Paneller/Firma/Limits.cshtml", model);
    }

    [HttpGet("faturalar")]
    public async Task<IActionResult> Invoices(CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var model = await _firmaService.GetInvoicesAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Faturalar";
        ViewData["PageCssPath"] = "paneller/firma/invoices";
        return View("~/Views/Paneller/Firma/Invoices.cshtml", model);
    }

    [HttpGet("harcama-raporlari")]
    public async Task<IActionResult> Spending(CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var model = await _firmaService.GetSpendingReportsAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Harcama Raporları";
        ViewData["PageCssPath"] = "paneller/firma/spending";
        return View("~/Views/Paneller/Firma/Spending.cshtml", model);
    }

    [HttpGet("otel-bazli-rapor")]
    public async Task<IActionResult> Hotels(CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var model = await _firmaService.GetHotelReportsAsync(GetUserId(), cancellationToken);
        ApplyFeedback(model.Shell);
        ViewData["Title"] = "Otel Bazlı Rapor";
        ViewData["PageCssPath"] = "paneller/firma/hotels";
        return View("~/Views/Paneller/Firma/Hotels.cshtml", model);
    }

    [HttpPost("calisan-ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(FirmaEmployeeCreateModel model, CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var result = await _firmaService.CreateEmployeeAsync(GetUserId(), model, cancellationToken);
        SetFeedback(result.Success, result.Message);
        return Redirect("/panel/firma/calisanlar");
    }

    [HttpPost("mesajlar/gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(MessageSendRequest form, List<IFormFile>? attachments, CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var result = await _firmaService.SendMessageAsync(GetUserId(), form, attachments, HttpContext, cancellationToken);
        SetFeedback(result.Success, result.Message);
        return Redirect($"/panel/firma/mesajlar?conversationId={form.ConversationId}");
    }

    [HttpPost("mesajlar/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(MessageDeleteRequest form, CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var result = await _firmaService.DeleteMessageAsync(GetUserId(), form, cancellationToken);
        SetFeedback(result.Success, result.Message);
        return Redirect($"/panel/firma/mesajlar?conversationId={form.ConversationId}");
    }

    [HttpPost("limit-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLimit(FirmaLimitUpsertModel model, CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var result = await _firmaService.UpsertLimitAsync(GetUserId(), model, cancellationToken);
        SetFeedback(result.Success, result.Message);
        return Redirect("/panel/firma/limitler-onaylar");
    }

    [HttpPost("rezervasyon-onay")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReservationApproval(FirmaReservationDecisionModel model, CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var result = await _firmaService.UpdateReservationApprovalAsync(GetUserId(), model, cancellationToken);
        SetFeedback(result.Success, result.Message);
        return Redirect("/panel/firma/limitler-onaylar");
    }

    [HttpPost("telefon-kodu-gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPhoneVerificationCode(string? phoneNumber, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var result = await _phoneVerificationService.SendVerificationCodeAsync(
            GetUserId(),
            phoneNumber,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        SetFeedback(result.Success, result.Message);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect("/panel/firma/dashboard?openPhoneVerification=1");
    }

    [HttpPost("telefon-kodu-dogrula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPhoneVerificationCode(string verificationCode, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!IsFirmaUser()) return Redirect("/kullanici-giris");
        var result = await _phoneVerificationService.VerifyCodeAsync(
            GetUserId(),
            verificationCode,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        SetFeedback(result.Success, result.Message);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect("/panel/firma/dashboard?openPhoneVerification=1");
    }

    private bool IsFirmaUser()
    {
        var accountType = User.FindFirstValue(AuthClaimTypes.AccountType);
        var userRole = User.FindFirstValue(AuthClaimTypes.UserRole);
        return string.Equals(accountType, "firma", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(userRole) && userRole.StartsWith("firma_", StringComparison.OrdinalIgnoreCase));
    }

    private long GetUserId()
        => long.TryParse(User.FindFirstValue(AuthClaimTypes.UserId), out var userId) ? userId : 0;

    private void SetFeedback(bool success, string message)
    {
        if (success)
        {
            TempData["FirmaSuccess"] = message;
            TempData.Remove("FirmaError");
            return;
        }

        TempData["FirmaError"] = message;
        TempData.Remove("FirmaSuccess");
    }

    private void ApplyFeedback(otelturizmnew.Models.Paneller.Firma.FirmaPanelShellViewModel shell)
    {
        shell.SuccessMessage = TempData["FirmaSuccess"] as string;
        shell.ErrorMessage = TempData["FirmaError"] as string;
    }
}
