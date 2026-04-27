using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.OutputCaching;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Models.Paneller.Developer;
using otelturizmnew.Models.TelefonDogrulama;
using otelturizmnew.Models.Messages;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Models.Email;

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
    private readonly IAuditLogService _auditLogService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ISitemapService _sitemapService;
    private readonly IAdminSupportArticleService _adminSupportArticleService;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly ISecureFileService _secureFileService;
    private readonly IGrowthGovernanceService _growthGovernance;

    public AdminPanelController(IAdminService adminService, IAdminHotelManagementService adminHotelManagementService, IContractContentService contractContentService, IDevelopmentRequestService developmentRequestService, IPhoneVerificationService phoneVerificationService, IAuditLogService auditLogService, IImageStorageService imageStorageService, ISitemapService sitemapService, IAdminSupportArticleService adminSupportArticleService, IWebHostEnvironment environment, IHttpClientFactory httpClientFactory, IOutputCacheStore outputCacheStore, ISecureFileService secureFileService, IGrowthGovernanceService growthGovernance)
    {
        _adminService = adminService;
        _adminHotelManagementService = adminHotelManagementService;
        _contractContentService = contractContentService;
        _developmentRequestService = developmentRequestService;
        _phoneVerificationService = phoneVerificationService;
        _auditLogService = auditLogService;
        _imageStorageService = imageStorageService;
        _sitemapService = sitemapService;
        _adminSupportArticleService = adminSupportArticleService;
        _environment = environment;
        _httpClientFactory = httpClientFactory;
        _outputCacheStore = outputCacheStore;
        _secureFileService = secureFileService;
        _growthGovernance = growthGovernance;
    }

    private async Task EvictPublicOutputCacheAsync(CancellationToken cancellationToken)
    {
        // OutputCache policy'leri Program.cs içinde tag'li (public/public-short/public-medium).
        // Otel/kampanya gibi içerikler güncellendiğinde public cache'i hızlı şekilde temizliyoruz.
        await _outputCacheStore.EvictByTagAsync("public", cancellationToken);
        await _outputCacheStore.EvictByTagAsync("public-short", cancellationToken);
        await _outputCacheStore.EvictByTagAsync("public-medium", cancellationToken);
    }

    // p181: Kritik admin aksiyonlarında gerekçe zorunlu
    private bool TryValidateCriticalReason(string? reason, out string error)
    {
        if (!CanPerformCriticalAdminActions())
        {
            error = "Bu işlem için yetkiniz yok.";
            return false;
        }

        var r = (reason ?? string.Empty).Trim();
        if (r.Length < 5 || r.Length > 240)
        {
            error = "Gerekçe 5-240 karakter olmalı.";
            return false;
        }

        error = string.Empty;
        return true;
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
        ViewData["PageCssPath"] = "panel-admin-dashboard";
        return View("~/Views/Paneller/Admin/Dashboard.cshtml", model);
    }

    [HttpGet("sistem-sagligi")]
    public async Task<IActionResult> SystemHealth(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetSystemHealthAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        model.LinkCheck.BaseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        return View("~/Views/Paneller/Admin/SystemHealth.cshtml", model);
    }

    [HttpPost("sistem-sagligi/link-kontrol")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunInternalLinkCheck(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetSystemHealthAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        model.LinkCheck.BaseUrl = baseUrl;
        model.LinkCheck.CheckedAtUtc = DateTimeOffset.UtcNow;

        var viewsRoot = Path.Combine(_environment.ContentRootPath, "Views");
        if (!Directory.Exists(viewsRoot))
        {
            model.LinkCheck.Warning = $"Views klasörü bulunamadı: {viewsRoot}";
            ViewData["Title"] = model.Shell.PanelTitle;
            ViewData["PageCssPath"] = "panel-admin-section";
            return View("~/Views/Paneller/Admin/SystemHealth.cshtml", model);
        }

        var routes = ExtractInternalRoutesFromViews(viewsRoot);
        model.LinkCheck.Total = routes.Count;

        // Bu kontrol aynı oturum/cookie ile koşmalı ki auth'lu paneller de 200 dönebilsin.
        var cookieHeader = Request.Headers.Cookie.ToString();
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        client.Timeout = TimeSpan.FromSeconds(12);
        client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookieHeader);
        }
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Otelturizm-SystemHealth-LinkCheck/1.0");

        var semaphore = new SemaphoreSlim(8);
        var tasks = routes.Select(async route =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, route);
                    using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    sw.Stop();
                    return new AdminInternalLinkCheckRowViewModel
                    {
                        Route = route,
                        Status = (int)resp.StatusCode,
                        Ms = (int)sw.ElapsedMilliseconds
                    };
                }
                catch
                {
                    sw.Stop();
                    return new AdminInternalLinkCheckRowViewModel { Route = route, Status = -1, Ms = -1 };
                }
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        var rows = await Task.WhenAll(tasks);
        model.LinkCheck.Rows = rows
            .OrderBy(r => r.IsOk ? 1 : 0)
            .ThenBy(r => r.Status)
            .ThenBy(r => r.Route, StringComparer.OrdinalIgnoreCase)
            .ToList();
        model.LinkCheck.Ok = model.LinkCheck.Rows.Count(r => r.IsOk);
        model.LinkCheck.Bad = model.LinkCheck.Rows.Count - model.LinkCheck.Ok;

        if (model.LinkCheck.Bad > 0)
        {
            await QueueBrokenLinkReportEmailAsync(model, cancellationToken);
        }

        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        return View("~/Views/Paneller/Admin/SystemHealth.cshtml", model);
    }

    private async Task QueueBrokenLinkReportEmailAsync(AdminSystemHealthPageViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // admin hedef listesi (doğrulanmış e-posta)
            var adminEmails = new List<(long Id, string Email)>();
            await using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("""
                SELECT TOP (25) id, eposta
                FROM dbo.users
                WHERE rol = N'admin'
                  AND eposta IS NOT NULL
                  AND LTRIM(RTRIM(eposta)) <> N''
                  AND email_dogrulama_tarihi IS NOT NULL
                ORDER BY id ASC;
                """, connection))
            await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    adminEmails.Add((reader.GetInt64(0), reader.GetString(1)));
                }
            }

            if (adminEmails.Count == 0)
            {
                return;
            }

            var badLines = model.LinkCheck.Rows
                .Where(r => !r.IsOk)
                .Take(50)
                .Select(r => $"{r.StatusText}\t{r.Route}")
                .ToList();

            var badList = badLines.Count == 0 ? "-" : string.Join("\n", badLines);
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["base_url"] = model.LinkCheck.BaseUrl ?? string.Empty,
                ["checked_at"] = (model.LinkCheck.CheckedAtUtc?.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") ?? "-"),
                ["ok_count"] = model.LinkCheck.Ok.ToString(),
                ["bad_count"] = model.LinkCheck.Bad.ToString(),
                ["total_count"] = model.LinkCheck.Total.ToString(),
                ["bad_list"] = badList
            };

            var emailQueue = HttpContext.RequestServices.GetRequiredService<IEmailQueueService>();
            foreach (var target in adminEmails)
            {
                await emailQueue.QueueTemplateAsync(connection, null, new QueuedEmailTemplateRequest
                {
                    UserId = target.Id,
                    RecipientEmail = target.Email,
                    TemplateCode = "system_health_link_report",
                    RelatedTable = "users",
                    RelatedRecordId = target.Id,
                    Tokens = tokens
                }, cancellationToken);
            }
        }
        catch
        {
            // health sayfasini bozmayalim
        }
    }

    [HttpPost("sistem-sagligi/email-test-modu")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmailTestMode([FromForm] bool enabled, [FromForm] string? reason, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!TryValidateCriticalReason(reason, out var reasonError))
        {
            TempData["AdminError"] = reasonError;
            return RedirectToAction(nameof(SystemHealth));
        }

        var connectionString = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            TempData["AdminMessage"] = "DB bağlantısı bulunamadı.";
            return RedirectToAction(nameof(SystemHealth));
        }

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new Microsoft.Data.SqlClient.SqlCommand("""
            UPDATE email_services
            SET test_modu = @enabled,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE aktif_mi = 1;
            """, connection);
        cmd.Parameters.AddWithValue("@enabled", enabled ? 1 : 0);
        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);

        TempData["AdminMessage"] = affected > 0
            ? $"E-posta servisi test modu {(enabled ? "AÇILDI" : "KAPATILDI")}."
            : "Aktif e-posta servisi bulunamadı (email_services.aktif_mi=1).";

        await _auditLogService.TryLogAdminActionAsync(
            GetUserId(),
            "email_test_mode",
            "email_services",
            enabled ? "1" : "0",
            $"Gerekçe: {reason}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return RedirectToAction(nameof(SystemHealth));
    }

    [HttpGet("sistem-sagligi/slow-sql")]
    public IActionResult SlowSql([FromServices] otelturizmnew.Services.Abstractions.ISlowSqlTracker slowSqlTracker, [FromQuery] int take = 20)
    {
        if (!CanAccessAdminPanel())
        {
            return Unauthorized(new { ok = false });
        }

        var rows = slowSqlTracker.GetTop(take);
        return Ok(new { ok = true, rows });
    }

    // p182: Admin işlem logları
    [HttpGet("islem-loglari")]
    public async Task<IActionResult> AdminActionLogs([FromQuery] long? adminUserId, [FromQuery] string? actionType, [FromQuery] string? targetTable, [FromQuery] string? q, [FromQuery] string? sort, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var filter = new AdminActionLogFilter
        {
            AdminUserId = adminUserId,
            ActionType = actionType,
            TargetTable = targetTable,
            Query = q,
            Sort = sort,
            Page = page <= 0 ? 1 : page,
            PageSize = pageSize <= 0 ? 50 : pageSize
        };

        var model = await _adminService.GetAdminActionLogsAsync(GetFullName(), GetEmail(), GetUserRole(), filter, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/AdminActionLogs.cshtml", model);
    }

    [HttpGet("islem-loglari/csv")]
    public async Task<IActionResult> ExportAdminActionLogsCsv([FromQuery] long? adminUserId, [FromQuery] string? actionType, [FromQuery] string? targetTable, [FromQuery] string? q, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var filter = new AdminActionLogFilter
        {
            AdminUserId = adminUserId,
            ActionType = actionType,
            TargetTable = targetTable,
            Query = q,
            Sort = "date_desc",
            Page = 1,
            PageSize = 5000
        };

        var csv = await _adminService.ExportAdminActionLogsCsvAsync(filter, cancellationToken);
        var bytes = Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv; charset=utf-8", $"admin-islem-loglari-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    // p183: Rezervasyonlar tek liste
    [HttpGet("rezervasyonlar-tek-liste")]
    public async Task<IActionResult> UnifiedReservations([FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetUnifiedReservationsAsync(GetFullName(), GetEmail(), GetUserRole(), q, status, page, pageSize, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/UnifiedReservations.cshtml", model);
    }

    // p184: Email kuyruk yönetimi
    [HttpGet("email-kuyruk")]
    public async Task<IActionResult> EmailQueue([FromQuery] string? status, [FromQuery] string? q, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var filter = new AdminEmailQueueFilter { Status = status, Query = q, Page = page <= 0 ? 1 : page, PageSize = pageSize <= 0 ? 50 : pageSize };
        var model = await _adminService.GetEmailQueueAsync(GetFullName(), GetEmail(), GetUserRole(), filter, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/EmailQueue.cshtml", model);
    }

    [HttpPost("email-kuyruk/retry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmailQueueForceRetry([FromForm] long id, [FromForm] string? reason, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!TryValidateCriticalReason(reason, out var err))
        {
            TempData["AdminError"] = err;
            return RedirectToAction(nameof(EmailQueue));
        }

        var result = await _adminService.ForceRetryEmailAsync(GetUserId(), id, reason!, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        await _auditLogService.TryLogAdminActionAsync(GetUserId(), "email_force_retry", "bildirim_loglari", id.ToString(), $"Gerekçe: {reason}", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return RedirectToAction(nameof(EmailQueue));
    }

    [HttpPost("email-kuyruk/fail")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmailQueueMarkFailed([FromForm] long id, [FromForm] string? reason, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!TryValidateCriticalReason(reason, out var err))
        {
            TempData["AdminError"] = err;
            return RedirectToAction(nameof(EmailQueue));
        }

        var result = await _adminService.MarkEmailFailedAsync(GetUserId(), id, reason!, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        await _auditLogService.TryLogAdminActionAsync(GetUserId(), "email_mark_failed", "bildirim_loglari", id.ToString(), $"Gerekçe: {reason}", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return RedirectToAction(nameof(EmailQueue));
    }

    // p185: Cache evict manuel
    [HttpPost("cache/evict-public")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EvictPublicCache([FromForm] string? reason, [FromForm] string? returnTo, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!TryValidateCriticalReason(reason, out var err))
        {
            TempData["AdminError"] = err;
            return string.Equals(returnTo, "commerce", StringComparison.OrdinalIgnoreCase)
                ? RedirectToAction(nameof(CommerceInsight))
                : RedirectToAction(nameof(SystemHealth));
        }

        await EvictPublicOutputCacheAsync(cancellationToken);
        TempData["AdminMessage"] = "Public cache temizlendi.";
        await _auditLogService.TryLogAdminActionAsync(GetUserId(), "cache_evict_public", "output_cache", null, $"Gerekçe: {reason}", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return string.Equals(returnTo, "commerce", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction(nameof(CommerceInsight))
            : RedirectToAction(nameof(SystemHealth));
    }

    // p186: Sitemap refresh manuel
    [HttpPost("sitemap/refresh")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshSitemap([FromForm] string? reason, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!TryValidateCriticalReason(reason, out var err))
        {
            TempData["AdminError"] = err;
            return RedirectToAction(nameof(SystemHealth));
        }

        await _sitemapService.EnsureFreshSitemapAsync(force: true, cancellationToken);
        TempData["AdminMessage"] = "Sitemap refresh tetiklendi.";
        await _auditLogService.TryLogAdminActionAsync(GetUserId(), "sitemap_refresh", "sitemap", null, $"Gerekçe: {reason}", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return RedirectToAction(nameof(SystemHealth));
    }

    // p187: rate limit stats
    [HttpGet("rate-limit")]
    public async Task<IActionResult> RateLimitStats([FromQuery] int windowHours, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetRateLimitStatsAsync(GetFullName(), GetEmail(), GetUserRole(), windowHours <= 0 ? 24 : windowHours, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/RateLimitStats.cshtml", model);
    }

    // p188+p189: security/upload events (Serilog JSON dosyasından)
    [HttpGet("guvenlik-olaylari")]
    public async Task<IActionResult> SecurityEvents([FromQuery] int take, CancellationToken cancellationToken)
        => await RenderLogEventsAsync("SECURITY_EVENT", take, cancellationToken);

    [HttpGet("upload-gecmisi")]
    public async Task<IActionResult> UploadHistory([FromQuery] int take, CancellationToken cancellationToken)
        => await RenderLogEventsAsync("UPLOAD_AUDIT", take, cancellationToken);

    private async Task<IActionResult> RenderLogEventsAsync(string eventType, int take, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var shell = (await _adminService.GetSectionPageAsync("security", GetFullName(), GetEmail(), GetUserRole(), cancellationToken)).Shell;
        shell.PanelTitle = eventType == "UPLOAD_AUDIT" ? "Upload Geçmişi" : "Güvenlik Olayları";
        shell.PanelSubtitle = "Uygulama loglarından son event kayıtları (read-only).";

        var model = new AdminLogEventsPageViewModel { Shell = shell, EventType = eventType, Take = Math.Clamp(take <= 0 ? 200 : take, 50, 500) };
        try
        {
            var logRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "logs");
            if (!Directory.Exists(logRoot))
            {
                model.Warning = $"Log klasörü bulunamadı: {logRoot}";
            }
            else
            {
                var latest = new DirectoryInfo(logRoot)
                    .GetFiles("app-*.json")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (latest is null)
                {
                    model.Warning = "Log dosyası bulunamadı (app-*.json).";
                }
                else
                {
                    model.Rows = ReadCompactJsonEvents(latest.FullName, eventType, model.Take);
                }
            }
        }
        catch (Exception ex)
        {
            model.Warning = "Log okunurken hata oluştu: " + ex.Message;
        }

        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/LogEvents.cshtml", model);
    }

    private static List<AdminLogEventRowViewModel> ReadCompactJsonEvents(string filePath, string eventType, int take)
    {
        var results = new List<AdminLogEventRowViewModel>(take);
        foreach (var line in System.IO.File.ReadLines(filePath).Reverse())
        {
            if (results.Count >= take) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.Contains(eventType, StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var ts = root.TryGetProperty("@t", out var tEl) && DateTimeOffset.TryParse(tEl.GetString(), out var parsed)
                    ? parsed
                    : (DateTimeOffset?)null;
                var msg = root.TryGetProperty("@m", out var mEl) ? (mEl.GetString() ?? "") : "";
                results.Add(new AdminLogEventRowViewModel
                {
                    Timestamp = ts,
                    EventType = eventType,
                    Message = msg,
                    Raw = line.Length > 900 ? line[..900] + "…" : line
                });
            }
            catch
            {
                results.Add(new AdminLogEventRowViewModel
                {
                    Timestamp = null,
                    EventType = eventType,
                    Message = "-",
                    Raw = line.Length > 900 ? line[..900] + "…" : line
                });
            }
        }
        return results;
    }

    // p190: settings monitor
    [HttpGet("ayarlar-monitor")]
    public async Task<IActionResult> SettingsMonitor(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetSettingsMonitorAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/SettingsMonitor.cshtml", model);
    }

    [HttpGet("ticari-icgoru")]
    public async Task<IActionResult> CommerceInsight([FromQuery] long hotelId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetCommerceInsightPageAsync(GetFullName(), GetEmail(), GetUserRole(), hotelId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/CommerceInsight.cshtml", model);
    }

    [HttpPost("ticari-icgoru/growth-kill")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CommerceGrowthKill([FromForm] bool enableEmergencyKill, [FromForm] string? reason, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!TryValidateCriticalReason(reason, out var err))
        {
            TempData["AdminError"] = err;
            return RedirectToAction(nameof(CommerceInsight));
        }

        _growthGovernance.SetEmergencyKillSwitch(enableEmergencyKill);
        TempData["AdminMessage"] = enableEmergencyKill
            ? "Growth acil kill-switch acildi (tum yuzde rollout bayraklari kapali)."
            : "Growth acil kill-switch kapatildi.";
        await _auditLogService.TryLogAdminActionAsync(
            GetUserId(),
            "growth_emergency_kill_switch",
            "growth",
            enableEmergencyKill ? "1" : "0",
            $"Gerekce: {reason}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return RedirectToAction(nameof(CommerceInsight));
    }

    private static List<string> ExtractInternalRoutesFromViews(string viewsRoot)
    {
        var routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static void AddRoute(HashSet<string> set, string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            var route = raw.Trim();
            if (!route.StartsWith("/", StringComparison.Ordinal)) return;
            if (route.StartsWith("//", StringComparison.Ordinal)) return;
            if (route.Contains('@') || route.Contains('{') || route.Contains('}')) return;

            // statik dosyalar
            if (route.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase)
                || route.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
                || route.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                || route.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
                || route.StartsWith("/css/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            route = route.Split('#')[0].Split('?')[0].Trim();
            if (route.Length == 0) return;

            set.Add(route);
        }

        var files = Directory.EnumerateFiles(viewsRoot, "*.cshtml", SearchOption.AllDirectories);
        var hrefRegex = new Regex("<a[^>]+href\\s*=\\s*\"(?<u>/[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        foreach (var file in files)
        {
            string content;
            try { content = System.IO.File.ReadAllText(file, Encoding.UTF8); }
            catch { continue; }

            foreach (Match m in hrefRegex.Matches(content))
            {
                AddRoute(routes, m.Groups["u"].Value);
            }
        }

        return routes.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    [HttpGet("sitemap")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var section = await _adminService.GetSectionPageAsync("settings", GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        var model = await _sitemapService.GetDiagnosticsAsync(cancellationToken);
        ViewData["AdminShell"] = section.Shell;
        ViewData["Title"] = "Sitemap Yönetimi";
        ViewData["PageCssPath"] = "panel-admin-section";
        return View("~/Views/Paneller/Admin/Sitemap.cshtml", model);
    }

    [HttpPost("sitemap/yenile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshSitemap(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        await _sitemapService.EnsureFreshSitemapAsync(true, cancellationToken);
        TempData["AdminMessage"] = "Sitemap ve il/ilçe XML dosyaları güncellendi.";
        return RedirectToAction(nameof(Sitemap));
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
        ViewData["PageCssPath"] = "panel-admin-hotels";
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
        ViewData["PageCssPath"] = "panel-admin-hotels";
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
        }
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
        ViewData["PageCssPath"] = "panel-admin-commissions";
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
        ViewData["PageCssPath"] = "panel-admin-contracts";
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

        var adminUserId = GetUserId();
        var stored = await _secureFileService.SaveAsync(pdfFile, new SecureFileSaveRequest
        {
            ContextTable = "sozlesmeler",
            ContextId = contractId,
            OwnerUserId = adminUserId,
            Category = "contract-pdf",
            VisibilityScope = "private"
        }, cancellationToken);

        // DB kaydı migration ile eklenecek tabloya yazılır. Şema yoksa yükleme yine de dosyayı saklar.
        // dosya_yolu alanına fiziksel path yazıyoruz; e-posta ekinde worker direkt dosya sisteminden okuyabilir.
        var filePathOrUrl = stored.StoredPath;
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
                        INSERT INTO sozlesme_dosyalari (sozlesme_id, dosya_tipi, dosya_adi, dosya_yolu, mime_tipi, olusturan_kullanici_id, olusturulma_tarihi, guvenli_dosya_id)
                        VALUES (@contractId, 'pdf', @fileName, @fileUrl, 'application/pdf', @adminUserId, SYSUTCDATETIME(), @secureFileId);
                    END", connection);
                command.Parameters.AddWithValue("@contractId", contractId);
                command.Parameters.AddWithValue("@fileName", Path.GetFileName(pdfFile.FileName));
                command.Parameters.AddWithValue("@fileUrl", filePathOrUrl);
                command.Parameters.AddWithValue("@adminUserId", adminUserId);
                command.Parameters.AddWithValue("@secureFileId", stored.FileId > 0 ? stored.FileId : DBNull.Value);
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
        ViewData["PageCssPath"] = "panel-admin-partner-applications";
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
    public async Task<IActionResult> CompanyApplications(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetCompanyApplicationsAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        return View("~/Views/Paneller/Admin/CompanyApplications.cshtml", model);
    }

    [HttpPost("firma-basvurulari/guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCompanyApplicationStatus(AdminCompanyApplicationDecisionRequest request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminService.ReviewCompanyApplicationAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminCompanyMessage" : "AdminCompanyError"] = result.Message;
        return RedirectToAction(nameof(CompanyApplications));
    }

    [HttpGet("otel-liste-abonelikleri")]
    public async Task<IActionResult> ListingSubscriptions(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetListingSubscriptionsAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        return View("~/Views/Paneller/Admin/ListingSubscriptions.cshtml", model);
    }

    [HttpPost("otel-liste-abonelikleri/guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateListingSubscription(AdminListingSubscriptionDecisionRequest request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminService.ReviewListingSubscriptionAsync(GetUserId(), request, cancellationToken);
        if (result.Success)
        {
            try
            {
                await _auditLogService.TryLogAdminActionAsync(
                    GetUserId(),
                    $"listing_subscription_{request.Action}",
                    "otel_liste_abonelikleri",
                    request.SubscriptionId.ToString(),
                    string.IsNullOrWhiteSpace(request.AdminNote) ? result.Message : request.AdminNote.Trim(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    cancellationToken);
            }
            catch
            {
                // audit fail-safe
            }
        }
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(ListingSubscriptions));
    }

    [HttpGet("gelistirme-talepleri")]
    public async Task<IActionResult> DevelopmentRequests([FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] long? developerUserId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _developmentRequestService.GetAdminPageAsync(GetFullName(), GetEmail(), GetUserRole(), q, status, priority, developerUserId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-development";
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
            var adminUserId = GetUserId();
            var targetDir = Path.Combine(_environment.WebRootPath, "uploads", "developer", "admin", adminUserId.ToString());
            var saved = await _imageStorageService.SaveAsWebpAsync(form.VisualFile, targetDir, "admin-request", cancellationToken);
            imageUrl = $"/uploads/developer/admin/{adminUserId}/{saved.FileName}";
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

    [HttpGet("destek-makaleleri")]
    public async Task<IActionResult> SupportArticles([FromQuery] string? q, [FromQuery] long? kategoriId, [FromQuery] string? durum, [FromQuery] long? editId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var section = await _adminService.GetSectionPageAsync("faq", GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        section.Shell.PanelTitle = "Destek Makaleleri";
        section.Shell.PanelSubtitle = "Yardım merkezi içeriklerini tek yerden ekleyin, güncelleyin ve kaldırın.";

        var model = await _adminSupportArticleService.GetPageAsync(section.Shell, q, kategoriId, durum, editId, cancellationToken);
        ViewData["Title"] = section.Shell.PanelTitle;
        ViewData["PageCssPath"] = "panel-admin-section";
        ViewData["AdminShell"] = section.Shell;
        return View("~/Views/Paneller/Admin/SupportArticles.cshtml", model);
    }

    [HttpPost("destek-makaleleri/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSupportArticle(AdminSupportArticleForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminSupportArticleService.SaveAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(SupportArticles), new { editId = result.ArticleId ?? form.ArticleId });
    }

    [HttpPost("destek-makaleleri/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSupportArticle(long articleId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminSupportArticleService.DeleteAsync(GetUserId(), articleId, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(SupportArticles));
    }

    [HttpGet("sikayetler")]
    public Task<IActionResult> Complaints(CancellationToken cancellationToken) => RenderSectionAsync("complaints", "Complaints", cancellationToken);

    [HttpGet("log-kayitlari")]
    public Task<IActionResult> Logs(CancellationToken cancellationToken) => RenderSectionAsync("logs", "Logs", cancellationToken);

    [HttpGet("konum-arama-loglari")]
    public Task<IActionResult> GeoSearchLogs(CancellationToken cancellationToken) => RenderSectionAsync("geo-search-logs", "GeoSearchLogs", cancellationToken);

    [HttpGet("otel-koordinat-degisimleri")]
    public Task<IActionResult> HotelCoordinateChanges(CancellationToken cancellationToken) => RenderSectionAsync("hotel-coordinate-changes", "HotelCoordinateChanges", cancellationToken);

    [HttpGet("firma-rezervasyonlari")]
    public Task<IActionResult> CompanyReservations(CancellationToken cancellationToken) => RenderSectionAsync("company-reservations", "CompanyReservations", cancellationToken);

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
        ViewData["PageCssPath"] = string.Equals(sectionKey, "users", StringComparison.OrdinalIgnoreCase)
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
