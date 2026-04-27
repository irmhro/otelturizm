using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller.Developer;
using otelturizmnew.Models.Paneller.User;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Developer;

[Authorize]
[Route("panel/developer")]
public class DeveloperPanelController : Controller
{
    private readonly IDevelopmentRequestService _developmentRequestService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IUserPanelService _userPanelService;
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public DeveloperPanelController(
        IDevelopmentRequestService developmentRequestService,
        IImageStorageService imageStorageService,
        IUserPanelService userPanelService,
        IAuthService authService,
        IWebHostEnvironment environment)
    {
        _developmentRequestService = developmentRequestService;
        _imageStorageService = imageStorageService;
        _userPanelService = userPanelService;
        _authService = authService;
        _environment = environment;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        if (!CanAccessDeveloperPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _developmentRequestService.GetDeveloperDashboardAsync(GetCurrentUserId(), GetFullName(), GetEmail(), q, status, cancellationToken);
        ViewData["PageCssPath"] = "panel-developer-dashboard";
        ViewData["PanelTitle"] = "Developer Paneli";
        ViewData["PanelSubtitle"] = "Proje gelistirme taleplerini topla, cevaplari yonet ve kontrol surecini takip et.";
        return View("~/Views/Paneller/Developer/Index.cshtml", model);
    }

    [HttpPost("talep-olustur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(DeveloperRequestCreateForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessDeveloperPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var imageUrl = await SaveVisualAsync(form.VisualFile, cancellationToken);
        var result = await _developmentRequestService.CreateRequestAsync(GetCurrentUserId(), form, imageUrl, cancellationToken);
        TempData[result.Success ? "DeveloperSuccess" : "DeveloperError"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("talep-yaniti")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReply(DeveloperRequestReplyForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessDeveloperPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var imageUrl = await SaveVisualAsync(form.VisualFile, cancellationToken);
        var result = await _developmentRequestService.AddDeveloperReplyAsync(GetCurrentUserId(), form, imageUrl, cancellationToken);
        TempData[result.Success ? "DeveloperSuccess" : "DeveloperError"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("guvenlik")]
    public async Task<IActionResult> Security(CancellationToken cancellationToken)
    {
        if (!CanAccessDeveloperPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _authService.GetTwoFactorSecurityAsync(GetCurrentUserId(), "developer", cancellationToken);
        ViewData["PageCssPath"] = "panel-user-security";
        ViewData["PanelTitle"] = "Guvenlik ve Giris";
        ViewData["PanelSubtitle"] = "Developer hesabi icin sifre ve iki asamali dogrulama ayarlarini yonet.";
        return View("~/Views/Paneller/Developer/Security.cshtml", model);
    }

    [HttpPost("guvenlik/sifre")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(UserChangePasswordForm form, CancellationToken cancellationToken)
    {
        if (CanAccessDeveloperPanel())
        {
            var result = await _userPanelService.ChangePasswordAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "DeveloperSuccess" : "DeveloperError"] = result.Message;
        }

        return RedirectToAction(nameof(Security));
    }

    [HttpPost("guvenlik/iki-asamali")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTwoFactor(UserTwoFactorForm form, CancellationToken cancellationToken)
    {
        if (CanAccessDeveloperPanel())
        {
            var result = await _authService.SaveTwoFactorSecurityAsync(GetCurrentUserId(), form, cancellationToken);
            TempData[result.Success ? "DeveloperSuccess" : "DeveloperError"] = result.Message;
        }

        return RedirectToAction(nameof(Security));
    }

    private bool CanAccessDeveloperPanel()
    {
        var accountType = User.FindFirstValue(AuthClaimTypes.AccountType);
        var role = User.FindFirstValue(AuthClaimTypes.UserRole);
        return string.Equals(accountType, "developer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "developer", StringComparison.OrdinalIgnoreCase)
            || User.IsInRole("developer");
    }

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }

    private string GetFullName() => User.FindFirstValue(AuthClaimTypes.FullName) ?? User.Identity?.Name ?? "Developer";
    private string GetEmail() => User.FindFirstValue(AuthClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Email) ?? "-";

    private async Task<string?> SaveVisualAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return null;
        }

        var userId = GetCurrentUserId();
        var targetDir = Path.Combine(_environment.WebRootPath, "uploads", "developer", "requests", userId.ToString());
        var saved = await _imageStorageService.SaveAsWebpAsync(file, new otelturizmnew.Services.Abstractions.ImageSaveRequest(
            TargetDirectory: targetDir,
            FilePrefix: "request",
            Category: "developer-request-visual",
            OwnerUserId: userId,
            QualityProfile: otelturizmnew.Services.Abstractions.ImageQualityProfile.RequestVisual,
            GenerateThumbnails: true
        ), cancellationToken);
        return $"/uploads/developer/requests/{userId}/{saved.FileName}";
    }
}
