using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Departman;

[Authorize]
[Route("panel/departman")]
public sealed class DepartmentPanelController : Controller
{
    private readonly IDepartmentPanelService _departmentPanelService;
    private readonly IPanelThemeService _panelThemeService;

    public DepartmentPanelController(IDepartmentPanelService departmentPanelService, IPanelThemeService panelThemeService)
    {
        _departmentPanelService = departmentPanelService;
        _panelThemeService = panelThemeService;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        return Department("dashboard", cancellationToken);
    }

    [HttpGet("{departmentKey}")]
    [HttpGet("{departmentKey}/dashboard")]
    public async Task<IActionResult> Department(string departmentKey, CancellationToken cancellationToken)
    {
        if (!CanAccessDepartment(departmentKey))
        {
            return Redirect("/kullanici-giris");
        }

        var model = await _departmentPanelService.GetDashboardAsync(departmentKey, GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["DepartmentShell"] = model.Shell;
        ViewData["PageCssPath"] = "paneller/departman/dashboard";
        return View("~/Views/Paneller/Departman/Dashboard.cshtml", model);
    }

    private bool CanAccessDepartment(string? requestedDepartment)
    {
        var role = GetUserRole();
        if (User.IsInRole("admin") || User.IsInRole("superadmin") || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (role.StartsWith("departman_", StringComparison.OrdinalIgnoreCase) || role.StartsWith("department_", StringComparison.OrdinalIgnoreCase))
        {
            var roleDepartment = NormalizeDepartment(role);
            var requested = NormalizeDepartment(requestedDepartment);
            return requested == "dashboard" || string.Equals(roleDepartment, requested, StringComparison.OrdinalIgnoreCase);
        }

        return User.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == AuthClaimTypes.RoleCodes)
            && (c.Value.StartsWith("departman_", StringComparison.OrdinalIgnoreCase) || c.Value.StartsWith("department_", StringComparison.OrdinalIgnoreCase)));
    }

    private static string NormalizeDepartment(string? value)
    {
        var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(raw)
            ? "dashboard"
            : raw.Replace("departman_", string.Empty).Replace("department_", string.Empty);
    }

    private string GetFullName() => User.FindFirstValue(AuthClaimTypes.FullName) ?? User.Identity?.Name ?? "Departman Kullanıcısı";
    private string GetEmail() => User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(AuthClaimTypes.Email) ?? "-";
    private string GetUserRole() => User.FindFirstValue(AuthClaimTypes.UserRole) ?? "departman_kullanici";

    private long GetUserId()
        => long.TryParse(User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpPost("tema/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTheme(otelturizmnew.Models.Paneller.Partner.PanelThemeViewModel theme, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!CanAccessDepartment("dashboard"))
        {
            return Redirect("/kullanici-giris");
        }

        var userId = GetUserId();
        var result = await _panelThemeService.SaveAsync("departman", userId, theme, cancellationToken);
        TempData[result.Success ? "DepartmentSuccess" : "DepartmentError"] = result.Message;
        return Redirect(!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/panel/departman/dashboard");
    }
}
