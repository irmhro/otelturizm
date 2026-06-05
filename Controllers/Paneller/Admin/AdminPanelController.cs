using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.OutputCaching;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller;
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
    private readonly IAdminEmailRoutingService _adminEmailRoutingService;
    private readonly IAdminRbacService _adminRbacService;
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
    private readonly IPanelThemeService _panelThemeService;
    private readonly IPlatformPackageService _platformPackageService;

    public AdminPanelController(IAdminService adminService, IAdminEmailRoutingService adminEmailRoutingService, IAdminRbacService adminRbacService, IAdminHotelManagementService adminHotelManagementService, IContractContentService contractContentService, IDevelopmentRequestService developmentRequestService, IPhoneVerificationService phoneVerificationService, IAuditLogService auditLogService, IImageStorageService imageStorageService, ISitemapService sitemapService, IAdminSupportArticleService adminSupportArticleService, IWebHostEnvironment environment, IHttpClientFactory httpClientFactory, IOutputCacheStore outputCacheStore, ISecureFileService secureFileService, IGrowthGovernanceService growthGovernance, IPanelThemeService panelThemeService, IPlatformPackageService platformPackageService)
    {
        _adminService = adminService;
        _adminEmailRoutingService = adminEmailRoutingService;
        _adminRbacService = adminRbacService;
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
        _panelThemeService = panelThemeService;
        _platformPackageService = platformPackageService;
    }

    [HttpGet("ekibimiz")]
    public async Task<IActionResult> Team([FromQuery] long? editId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.notifications", cancellationToken) is { } deniedTeam)
        {
            return deniedTeam;
        }

        var model = await _adminService.GetPlatformTeamAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        if (editId.HasValue && editId.Value > 0)
        {
            var row = model.Members.FirstOrDefault(item => item.Id == editId.Value);
            if (row is not null)
            {
                model.Form = new AdminPlatformTeamForm
                {
                    Id = row.Id,
                    Name = row.Name,
                    Title = row.Title,
                    Email = row.Email,
                    Description = row.Description,
                    OrderNo = row.OrderNo,
                    IsActive = row.IsActive
                };
            }
        }
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
        return View("~/Views/Paneller/Admin/Team.cshtml", model);
    }

    [HttpPost("ekibimiz/kaydet")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 10485760)]
    [RequestSizeLimit(10485760)]
    public async Task<IActionResult> SaveTeam([FromForm] AdminPlatformTeamForm form, IFormFile? avatarFile, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.notifications", cancellationToken) is { } deniedSave)
        {
            return deniedSave;
        }

        string? avatarUrl = null;
        try
        {
            if (avatarFile is not null && avatarFile.Length > 0)
            {
                var result = await _imageStorageService.SaveAsWebpAsync(avatarFile, new otelturizmnew.Services.Abstractions.ImageSaveRequest(
                    TargetDirectory: Path.Combine(_environment.WebRootPath, "uploads", "team"),
                    FilePrefix: $"team-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Category: "platform-team",
                    OwnerUserId: GetUserId(),
                    QualityProfile: otelturizmnew.Services.Abstractions.ImageQualityProfile.Avatar,
                    GenerateThumbnails: true
                ), cancellationToken);

                avatarUrl = "/uploads/team/" + result.FileName;
            }
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = "Avatar kaydedilemedi: " + ex.Message;
            return RedirectToAction(nameof(Team));
        }

        var save = await _adminService.SavePlatformTeamMemberAsync(GetUserId(), form, avatarUrl, cancellationToken);
        TempData[save.Success ? "AdminMessage" : "AdminError"] = save.Message;
        return RedirectToAction(nameof(Team));
    }

    [HttpPost("ekibimiz/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeam(long id, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.notifications", cancellationToken) is { } deniedDelete)
        {
            return deniedDelete;
        }

        var result = await _adminService.DeletePlatformTeamMemberAsync(GetUserId(), id, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(Team));
    }

    [HttpGet("yardim-merkezi-yonetim")]
    public async Task<IActionResult> HelpCenter([FromQuery] string? tab, [FromQuery] long? editCategoryId, [FromQuery] long? editFaqId, [FromQuery] long? editContentId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.support_articles", cancellationToken) is { } deniedHelp)
        {
            return deniedHelp;
        }

        var section = await _adminService.GetSectionPageAsync("faq", GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        section.Shell.PanelTitle = "Yardım Merkezi Yönetimi";
        section.Shell.PanelSubtitle = "Kategoriler, kategori detayları, SSS ve Hakkımızda/Kariyer/Basın/Blog içeriklerini tek yerden yönetin.";

        var model = new AdminHelpCenterPageViewModel { Shell = section.Shell };
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);

        // Categories + details
        const string catSql = @"
            SELECT k.[ID], k.[KATEGORI_ADI], k.[SEO_SLUG], COALESCE(k.[KATEGORI_IKON], N''), COALESCE(k.[KISA_ACIKLAMA], N''),
                   COALESCE(d.[HERO_BASLIK], N''), COALESCE(d.[HERO_ALT_BASLIK], N''), COALESCE(d.[HERO_GORSEL_URL], N''), COALESCE(d.[TAM_ACIKLAMA], N'')
            FROM [dbo].[DESTEK_KATEGORILERI] k
            LEFT JOIN [dbo].[YARDIM_MERKEZI_KATEGORI_DETAYLARI] d ON d.[DESTEK_KATEGORI_ID] = k.[ID]
            WHERE k.[DURUM] = 1
            ORDER BY k.[SIRALAMA], k.[ID];";
        try
        {
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(catSql, connection);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                model.Categories.Add(new AdminHelpCenterCategoryRowViewModel
                {
                    CategoryId = r.GetInt64(0),
                    Name = r.GetString(1),
                    Slug = r.GetString(2),
                    IconClass = r.IsDBNull(3) ? string.Empty : r.GetString(3),
                    ShortDescription = r.IsDBNull(4) ? string.Empty : r.GetString(4),
                    HeroTitle = r.IsDBNull(5) ? null : r.GetString(5),
                    HeroSubtitle = r.IsDBNull(6) ? null : r.GetString(6),
                    HeroImageUrl = r.IsDBNull(7) ? null : r.GetString(7),
                    FullHtml = r.IsDBNull(8) ? null : r.GetString(8)
                });
            }
        }
        catch
        {
            // ignore
        }

        // FAQ
        try
        {
            const string faqSql = @"
                SELECT TOP (200) f.[ID], f.[DESTEK_KATEGORI_ID], k.[KATEGORI_ADI], COALESCE(f.[SIRALAMA], 0), COALESCE(f.[AKTIF_MI], 1), f.[SORU], f.[CEVAP]
                FROM [dbo].[YARDIM_MERKEZI_KATEGORI_SSS] f
                INNER JOIN [dbo].[DESTEK_KATEGORILERI] k ON k.[ID] = f.[DESTEK_KATEGORI_ID]
                ORDER BY k.[SIRALAMA], f.[SIRALAMA], f.[ID] DESC;";
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(faqSql, connection);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                model.FaqItems.Add(new AdminHelpCenterFaqRowViewModel
                {
                    Id = r.GetInt64(0),
                    CategoryId = r.GetInt64(1),
                    CategoryName = r.GetString(2),
                    OrderNo = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3)),
                    IsActive = !r.IsDBNull(4) && Convert.ToInt32(r.GetValue(4)) == 1,
                    Question = r.GetString(5),
                    AnswerHtml = r.GetString(6)
                });
            }
        }
        catch
        {
            // ignore
        }

        // Contents
        try
        {
            const string cSql = @"
                SELECT TOP (200) [ID], [ICERIK_TURU], [BASLIK], [SEO_SLUG], COALESCE([AKTIF_MI],1), COALESCE([SIRALAMA],0), COALESCE([ONE_CIKAN_MI],0)
                FROM [dbo].[YARDIM_MERKEZI_ICERIKLER]
                ORDER BY [ICERIK_TURU], [ONE_CIKAN_MI] DESC, [SIRALAMA], [ID] DESC;";
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(cSql, connection);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                model.Contents.Add(new AdminHelpCenterContentRowViewModel
                {
                    Id = r.GetInt64(0),
                    Type = r.GetString(1),
                    Title = r.GetString(2),
                    Slug = r.GetString(3),
                    IsActive = !r.IsDBNull(4) && Convert.ToInt32(r.GetValue(4)) == 1,
                    OrderNo = r.IsDBNull(5) ? 0 : Convert.ToInt32(r.GetValue(5)),
                    IsFeatured = !r.IsDBNull(6) && Convert.ToInt32(r.GetValue(6)) == 1
                });
            }
        }
        catch
        {
            // ignore
        }

        // Prefill forms for editing
        if (editCategoryId.HasValue)
        {
            var row = model.Categories.FirstOrDefault(x => x.CategoryId == editCategoryId.Value);
            if (row is not null)
            {
                model.CategoryForm = new AdminHelpCenterCategoryForm
                {
                    CategoryId = row.CategoryId,
                    HeroTitle = row.HeroTitle,
                    HeroSubtitle = row.HeroSubtitle,
                    HeroImageUrl = row.HeroImageUrl,
                    FullHtml = row.FullHtml
                };
            }
        }
        if (editFaqId.HasValue)
        {
            var row = model.FaqItems.FirstOrDefault(x => x.Id == editFaqId.Value);
            if (row is not null)
            {
                model.FaqForm = new AdminHelpCenterFaqForm
                {
                    Id = row.Id,
                    CategoryId = row.CategoryId,
                    Question = row.Question,
                    AnswerHtml = row.AnswerHtml,
                    OrderNo = row.OrderNo,
                    IsActive = row.IsActive
                };
            }
        }
        if (editContentId.HasValue)
        {
            try
            {
                const string oneSql = @"
                    SELECT TOP (1) [ID], [ICERIK_TURU], [BASLIK], [SEO_SLUG], COALESCE([OZET],N''), COALESCE([HERO_BASLIK],N''), COALESCE([HERO_ALT_BASLIK],N''), COALESCE([HERO_GORSEL_URL],N''), [ICERIK],
                                   COALESCE([SIRALAMA],0), COALESCE([ONE_CIKAN_MI],0), COALESCE([AKTIF_MI],1)
                    FROM [dbo].[YARDIM_MERKEZI_ICERIKLER] WHERE [ID]=@id;";
                await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(oneSql, connection);
                cmd.Parameters.AddWithValue("@id", editContentId.Value);
                await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await r.ReadAsync(cancellationToken))
                {
                    model.ContentForm = new AdminHelpCenterContentForm
                    {
                        Id = r.GetInt64(0),
                        Type = r.GetString(1),
                        Title = r.GetString(2),
                        Slug = r.GetString(3),
                        Summary = r.IsDBNull(4) ? null : r.GetString(4),
                        HeroTitle = r.IsDBNull(5) ? null : r.GetString(5),
                        HeroSubtitle = r.IsDBNull(6) ? null : r.GetString(6),
                        HeroImageUrl = r.IsDBNull(7) ? null : r.GetString(7),
                        Html = r.GetString(8),
                        OrderNo = r.IsDBNull(9) ? 0 : Convert.ToInt32(r.GetValue(9)),
                        IsFeatured = !r.IsDBNull(10) && Convert.ToInt32(r.GetValue(10)) == 1,
                        IsActive = !r.IsDBNull(11) && Convert.ToInt32(r.GetValue(11)) == 1
                    };
                }
            }
            catch
            {
                // ignore
            }
        }

        ViewData["Title"] = section.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
        ViewData["AdminShell"] = section.Shell;
        ViewData["HelpCenterTab"] = string.IsNullOrWhiteSpace(tab) ? "categories" : tab;
        return View("~/Views/Paneller/Admin/HelpCenter.cshtml", model);
    }

    [HttpPost("yardim-merkezi-yonetim/kategori-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHelpCategory(AdminHelpCenterCategoryForm form, IFormFile? heroFile, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel()) return RedirectToAction("UserLogin", "Auth");
        if (await RequirePermissionOrForbidAsync("admin.support_articles", cancellationToken) is { } denied) return denied;

        string? heroUrl = null;
        if (heroFile is not null && heroFile.Length > 0)
        {
            var saved = await _imageStorageService.SaveAsWebpAsync(heroFile, new otelturizmnew.Services.Abstractions.ImageSaveRequest(
                TargetDirectory: Path.Combine(_environment.WebRootPath, "uploads", "helpcenter"),
                FilePrefix: $"hc-hero-{form.CategoryId}",
                Category: "helpcenter-hero",
                OwnerUserId: GetUserId(),
                QualityProfile: otelturizmnew.Services.Abstractions.ImageQualityProfile.RequestVisual,
                GenerateThumbnails: true
            ), cancellationToken);
            heroUrl = "/uploads/helpcenter/" + saved.FileName;
        }

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        var sql = @"
            IF OBJECT_ID(N'dbo.YARDIM_MERKEZI_KATEGORI_DETAYLARI', N'U') IS NULL
            BEGIN
                RAISERROR('YARDIM_MERKEZI_KATEGORI_DETAYLARI tablosu yok.', 16, 1);
                RETURN;
            END

            IF EXISTS (SELECT 1 FROM [dbo].[YARDIM_MERKEZI_KATEGORI_DETAYLARI] WHERE [DESTEK_KATEGORI_ID]=@cid)
            BEGIN
                UPDATE [dbo].[YARDIM_MERKEZI_KATEGORI_DETAYLARI]
                SET [HERO_BASLIK]=@ht,
                    [HERO_ALT_BASLIK]=@hs,
                    [HERO_GORSEL_URL]=COALESCE(NULLIF(@hu,N''), [HERO_GORSEL_URL]),
                    [TAM_ACIKLAMA]=@html,
                    [AKTIF_MI]=1,
                    [GUNCELLENME_TARIHI]=SYSUTCDATETIME()
                WHERE [DESTEK_KATEGORI_ID]=@cid;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[YARDIM_MERKEZI_KATEGORI_DETAYLARI]([DESTEK_KATEGORI_ID], [HERO_BASLIK], [HERO_ALT_BASLIK], [HERO_GORSEL_URL], [TAM_ACIKLAMA], [AKTIF_MI], [GUNCELLENME_TARIHI])
                VALUES(@cid,@ht,@hs,@hu,@html,1,SYSUTCDATETIME());
            END";
        try
        {
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@cid", form.CategoryId);
            cmd.Parameters.AddWithValue("@ht", string.IsNullOrWhiteSpace(form.HeroTitle) ? DBNull.Value : form.HeroTitle.Trim());
            cmd.Parameters.AddWithValue("@hs", string.IsNullOrWhiteSpace(form.HeroSubtitle) ? DBNull.Value : form.HeroSubtitle.Trim());
            cmd.Parameters.AddWithValue("@hu", string.IsNullOrWhiteSpace(heroUrl) ? (object)DBNull.Value : heroUrl);
            cmd.Parameters.AddWithValue("@html", string.IsNullOrWhiteSpace(form.FullHtml) ? DBNull.Value : form.FullHtml);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            TempData["AdminMessage"] = "Kategori detayları kaydedildi.";
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = "Kaydedilemedi: " + ex.Message;
        }
        return Redirect("/admin/yardim-merkezi-yonetim?tab=categories&editCategoryId=" + form.CategoryId);
    }

    [HttpPost("yardim-merkezi-yonetim/sss-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHelpFaq(AdminHelpCenterFaqForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel()) return RedirectToAction("UserLogin", "Auth");
        if (await RequirePermissionOrForbidAsync("admin.support_articles", cancellationToken) is { } denied) return denied;

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        var sql = @"
            IF OBJECT_ID(N'dbo.YARDIM_MERKEZI_KATEGORI_SSS', N'U') IS NULL
            BEGIN
                RAISERROR('YARDIM_MERKEZI_KATEGORI_SSS tablosu yok.', 16, 1);
                RETURN;
            END

            IF (@id IS NOT NULL AND EXISTS (SELECT 1 FROM [dbo].[YARDIM_MERKEZI_KATEGORI_SSS] WHERE [ID]=@id))
            BEGIN
                UPDATE [dbo].[YARDIM_MERKEZI_KATEGORI_SSS]
                SET [DESTEK_KATEGORI_ID]=@cid, [SORU]=@q, [CEVAP]=@a, [SIRALAMA]=@o, [AKTIF_MI]=@active
                WHERE [ID]=@id;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[YARDIM_MERKEZI_KATEGORI_SSS]([DESTEK_KATEGORI_ID], [SORU], [CEVAP], [SIRALAMA], [AKTIF_MI])
                VALUES(@cid,@q,@a,@o,@active);
            END";
        try
        {
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", form.Id.HasValue ? form.Id.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@cid", form.CategoryId);
            cmd.Parameters.AddWithValue("@q", form.Question.Trim());
            cmd.Parameters.AddWithValue("@a", form.AnswerHtml.Trim());
            cmd.Parameters.AddWithValue("@o", form.OrderNo);
            cmd.Parameters.AddWithValue("@active", form.IsActive ? 1 : 0);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            TempData["AdminMessage"] = "SSS kaydedildi.";
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = "Kaydedilemedi: " + ex.Message;
        }
        return Redirect("/admin/yardim-merkezi-yonetim?tab=faq");
    }

    [HttpPost("yardim-merkezi-yonetim/icerik-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHelpContent(AdminHelpCenterContentForm form, IFormFile? heroFile, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel()) return RedirectToAction("UserLogin", "Auth");
        if (await RequirePermissionOrForbidAsync("admin.support_articles", cancellationToken) is { } denied) return denied;

        string? heroUrl = null;
        if (heroFile is not null && heroFile.Length > 0)
        {
            var saved = await _imageStorageService.SaveAsWebpAsync(heroFile, new otelturizmnew.Services.Abstractions.ImageSaveRequest(
                TargetDirectory: Path.Combine(_environment.WebRootPath, "uploads", "helpcenter"),
                FilePrefix: $"hc-page-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Category: "helpcenter-hero",
                OwnerUserId: GetUserId(),
                QualityProfile: otelturizmnew.Services.Abstractions.ImageQualityProfile.RequestVisual,
                GenerateThumbnails: true
            ), cancellationToken);
            heroUrl = "/uploads/helpcenter/" + saved.FileName;
        }

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        var sql = @"
            IF OBJECT_ID(N'dbo.YARDIM_MERKEZI_ICERIKLER', N'U') IS NULL
            BEGIN
                RAISERROR('YARDIM_MERKEZI_ICERIKLER tablosu yok.', 16, 1);
                RETURN;
            END

            IF (@id IS NOT NULL AND EXISTS (SELECT 1 FROM [dbo].[YARDIM_MERKEZI_ICERIKLER] WHERE [ID]=@id))
            BEGIN
                UPDATE [dbo].[YARDIM_MERKEZI_ICERIKLER]
                SET [ICERIK_TURU]=@t, [BASLIK]=@title, [SEO_SLUG]=@slug, [OZET]=@sum,
                    [HERO_BASLIK]=@ht, [HERO_ALT_BASLIK]=@hs, [HERO_GORSEL_URL]=COALESCE(NULLIF(@hu,N''), [HERO_GORSEL_URL]),
                    [ICERIK]=@html, [SIRALAMA]=@o, [ONE_CIKAN_MI]=@f, [AKTIF_MI]=@active, [GUNCELLENME_TARIHI]=SYSUTCDATETIME()
                WHERE [ID]=@id;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[YARDIM_MERKEZI_ICERIKLER]([ICERIK_TURU], [BASLIK], [SEO_SLUG], [OZET], [HERO_BASLIK], [HERO_ALT_BASLIK], [HERO_GORSEL_URL], [ICERIK], [SIRALAMA], [ONE_CIKAN_MI], [AKTIF_MI])
                VALUES(@t,@title,@slug,@sum,@ht,@hs,@hu,@html,@o,@f,@active);
            END";
        try
        {
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", form.Id.HasValue ? form.Id.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@t", form.Type.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@title", form.Title.Trim());
            cmd.Parameters.AddWithValue("@slug", form.Slug.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@sum", string.IsNullOrWhiteSpace(form.Summary) ? DBNull.Value : form.Summary.Trim());
            cmd.Parameters.AddWithValue("@ht", string.IsNullOrWhiteSpace(form.HeroTitle) ? DBNull.Value : form.HeroTitle.Trim());
            cmd.Parameters.AddWithValue("@hs", string.IsNullOrWhiteSpace(form.HeroSubtitle) ? DBNull.Value : form.HeroSubtitle.Trim());
            cmd.Parameters.AddWithValue("@hu", string.IsNullOrWhiteSpace(heroUrl) ? (object)DBNull.Value : heroUrl);
            cmd.Parameters.AddWithValue("@html", form.Html);
            cmd.Parameters.AddWithValue("@o", form.OrderNo);
            cmd.Parameters.AddWithValue("@f", form.IsFeatured ? 1 : 0);
            cmd.Parameters.AddWithValue("@active", form.IsActive ? 1 : 0);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            TempData["AdminMessage"] = "İçerik kaydedildi.";
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = "Kaydedilemedi: " + ex.Message;
        }
        return Redirect("/admin/yardim-merkezi-yonetim?tab=pages");
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
        if (!await CanAccessAsync("admin.dashboard", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetDashboardAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = "Admin Dashboard";
        ViewData["PageCssPath"] = "admin_panel_dashboard_masaustu";
        ViewData["PageCssMobilePath"] = "admin_panel_dashboard_mobil";
        return View("~/Views/Paneller/Admin/Dashboard.cshtml", model);
    }

    [HttpPost("tema/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTheme(otelturizmnew.Models.Paneller.Partner.PanelThemeViewModel theme, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.settings", cancellationToken) is { } deniedTheme)
        {
            return deniedTheme;
        }

        var userId = GetUserId();
        var result = await _panelThemeService.SaveAsync("admin", userId, theme, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return Redirect(!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin/dashboard");
    }

    [HttpGet("sistem-sagligi")]
    public async Task<IActionResult> SystemHealth(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!await CanAccessAsync("admin.system_health", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetSystemHealthAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        model.LinkCheck.BaseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
        return View("~/Views/Paneller/Admin/SystemHealth.cshtml", model);
    }

    [HttpGet("platform-checkup")]
    public async Task<IActionResult> PlatformCheckup(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!await CanAccessAsync("admin.platform_checkup", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetPlatformCheckupAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_platform_checkup_masaustu";
        return View("~/Views/Paneller/Admin/PlatformCheckup.cshtml", model);
    }

    [HttpGet("onay-merkezi")]
    public async Task<IActionResult> ApprovalCenter(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!await CanAccessAsync("admin.approval_center", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetApprovalCenterAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_approval_center_masaustu";
        ViewData["PageCssMobilePath"] = "admin_panel_approval_center_mobil";
        return View("~/Views/Paneller/Admin/ApprovalCenter.cshtml", model);
    }

    [HttpPost("sistem-sagligi/link-kontrol")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunInternalLinkCheck(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.system_health", cancellationToken) is { } deniedLink)
        {
            return deniedLink;
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
            ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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
                SELECT TOP (25) id, [EPOSTA]
                FROM [dbo].[KULLANICILAR]
                WHERE rol = N'admin'
                  AND [EPOSTA] IS NOT NULL
                  AND LTRIM(RTRIM([EPOSTA])) <> N''
                  AND [EPOSTA_DOGRULAMA_TARIHI] IS NOT NULL
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
                    RelatedTable = "KULLANICILAR",
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
            UPDATE [dbo].[EPOSTA_SERVISLERI]
            SET [TEST_MODU] = @enabled,
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE [AKTIF_MI] = 1;
            """, connection);
        cmd.Parameters.AddWithValue("@enabled", enabled ? 1 : 0);
        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);

        TempData["AdminMessage"] = affected > 0
            ? $"E-posta servisi test modu {(enabled ? "AÇILDI" : "KAPATILDI")}."
            : "Aktif e-posta servisi bulunamadı (EPOSTA_SERVISLERI.AKTIF_MI=1).";

        await _auditLogService.TryLogAdminActionAsync(
            GetUserId(),
            "email_test_mode",
            "EPOSTA_SERVISLERI",
            enabled ? "1" : "0",
            $"Gerekçe: {reason}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return RedirectToAction(nameof(SystemHealth));
    }

    [HttpGet("sistem-sagligi/slow-sql")]
    public async Task<IActionResult> SlowSql([FromServices] otelturizmnew.Services.Abstractions.ISlowSqlTracker slowSqlTracker, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return Unauthorized(new { ok = false });
        }

        if (!await CanAccessAsync("admin.system_health", cancellationToken))
        {
            return Forbid();
        }

        var rows = slowSqlTracker.GetTop(take);
        return Ok(new { ok = true, rows });
    }

    [HttpGet("slow-sql")]
    public async Task<IActionResult> SlowSqlMonitor(
        [FromServices] otelturizmnew.Services.Abstractions.ISlowSqlTracker slowSqlTracker,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.system_health", cancellationToken))
        {
            return Forbid();
        }

        var shell = (await _adminService.GetSectionPageAsync("security", GetFullName(), GetEmail(), GetUserRole(), cancellationToken)).Shell;
        shell.PanelTitle = "Yavaş SQL";
        shell.PanelSubtitle = "Uygulama içi en yavaş sorgular (bellek içi tracker, yeniden başlatmada sıfırlanır).";

        var clampedTake = Math.Clamp(take <= 0 ? 50 : take, 10, 100);
        var model = new AdminSlowSqlPageViewModel
        {
            Shell = shell,
            Take = clampedTake,
            Rows = slowSqlTracker.GetTop(clampedTake)
                .Select(r => new AdminSlowSqlRowViewModel
                {
                    Key = r.Key,
                    Scope = r.Scope,
                    Count = r.Count,
                    MaxMs = r.MaxMs,
                    AvgMs = r.AvgMs,
                    LastSeenUtc = r.LastSeenUtc,
                    SampleSql = r.SampleSql
                })
                .ToList()
        };

        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_slow_sql_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/SlowSql.cshtml", model);
    }

    // p182: Admin işlem logları
    [HttpGet("islem-loglari")]
    public async Task<IActionResult> AdminActionLogs([FromQuery] long? adminUserId, [FromQuery] string? actionType, [FromQuery] string? targetTable, [FromQuery] string? q, [FromQuery] string? sort, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!await CanAccessAsync("admin.admin_action_logs", cancellationToken))
        {
            return Forbid();
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
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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

        if (await RequirePermissionOrForbidAsync("admin.admin_action_logs", cancellationToken) is { } deniedCsv)
        {
            return deniedCsv;
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
        if (!await CanAccessAsync("admin.unified_reservations", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetUnifiedReservationsAsync(GetFullName(), GetEmail(), GetUserRole(), q, status, page, pageSize, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_unified_reservations_masaustu";
        ViewData["PageCssMobilePath"] = "admin_panel_unified_reservations_mobil";
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
        if (!await CanAccessAsync("admin.email_queue", cancellationToken))
        {
            return Forbid();
        }

        var filter = new AdminEmailQueueFilter { Status = status, Query = q, Page = page <= 0 ? 1 : page, PageSize = pageSize <= 0 ? 50 : pageSize };
        var model = await _adminService.GetEmailQueueAsync(GetFullName(), GetEmail(), GetUserRole(), filter, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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

        if (await RequirePermissionOrForbidAsync("admin.email_queue", cancellationToken) is { } deniedRetry)
        {
            return deniedRetry;
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

        var evictPerm = string.Equals(returnTo, "commerce", StringComparison.OrdinalIgnoreCase) ? "admin.commerce_insight" : "admin.system_health";
        if (await RequirePermissionOrForbidAsync(evictPerm, cancellationToken) is { } deniedEvict)
        {
            return deniedEvict;
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

        if (await RequirePermissionOrForbidAsync("admin.sitemap", cancellationToken) is { } deniedSitemapRefresh)
        {
            return deniedSitemapRefresh;
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
        if (!await CanAccessAsync("admin.rate_limit", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetRateLimitStatsAsync(GetFullName(), GetEmail(), GetUserRole(), windowHours <= 0 ? 24 : windowHours, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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
        var requiredPermission = eventType == "UPLOAD_AUDIT" ? "admin.upload_history" : "admin.security_events";
        if (!await CanAccessAsync(requiredPermission, cancellationToken))
        {
            return Forbid();
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
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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
        if (!await CanAccessAsync("admin.settings_monitor", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetSettingsMonitorAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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
        if (!await CanAccessAsync("admin.commerce_insight", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetCommerceInsightPageAsync(GetFullName(), GetEmail(), GetUserRole(), hotelId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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
        if (!await CanAccessAsync("admin.commerce_insight", cancellationToken))
        {
            return Forbid();
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

        if (await RequirePermissionOrForbidAsync("admin.sitemap", cancellationToken) is { } deniedSitemap)
        {
            return deniedSitemap;
        }

        var section = await _adminService.GetSectionPageAsync("settings", GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        var model = await _sitemapService.GetDiagnosticsAsync(cancellationToken);
        ViewData["AdminShell"] = section.Shell;
        ViewData["Title"] = "Sitemap Yönetimi";
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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

        if (await RequirePermissionOrForbidAsync("admin.sitemap", cancellationToken) is { } deniedSitemapUpdate)
        {
            return deniedSitemapUpdate;
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
    public async Task<IActionResult> Hotels([FromQuery] string? q, [FromQuery] string? city, [FromQuery] string? district, [FromQuery] string? neighborhood, [FromQuery] string? publishStatus, [FromQuery] string? approvalStatus, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.hotels", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminHotelManagementService.GetHotelsPageAsync(GetFullName(), GetEmail(), GetUserRole(), q, city, district, neighborhood, publishStatus, approvalStatus, page, pageSize, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_hotels_masaustu";
        ViewData["PageCssMobilePath"] = "admin_panel_hotels_mobil";
        return View("~/Views/Paneller/Admin/Hotels.cshtml", model);
    }

    [HttpGet("otel-detay/{id:long?}")]
    public async Task<IActionResult> HotelDetail(long? id, [FromQuery] long? roomId, [FromQuery] long? hotelPhotoId, [FromQuery] long? roomPhotoId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.hotels", cancellationToken))
        {
            return Forbid();
        }

        if (!id.HasValue)
        {
            TempData["AdminHotelError"] = "Düzenlemek için bir otel seçmelisiniz.";
            return RedirectToAction(nameof(Hotels));
        }

        var model = await _adminHotelManagementService.GetHotelManagementPageAsync(id.Value, GetFullName(), GetEmail(), GetUserRole(), roomId, hotelPhotoId, roomPhotoId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_hotels_masaustu";
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
        if (!await CanAccessAsync("admin.hotels", cancellationToken))
        {
            return Forbid();
        }

        var result = await _adminHotelManagementService.SaveHotelAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
            await _auditLogService.TryLogAdminActionAsync(
                GetUserId(),
                "hotel_update",
                "oteller",
                request.HotelId.ToString(),
                result.Message,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
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

        if (await RequirePermissionOrForbidAsync("admin.hotels", cancellationToken) is { } deniedSaveRoom)
        {
            return deniedSaveRoom;
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

        if (await RequirePermissionOrForbidAsync("admin.hotels", cancellationToken) is { } deniedDeactRoom)
        {
            return deniedDeactRoom;
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
    public async Task<IActionResult> DeactivateHotel(long hotelId, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!await CanAccessAsync("admin.hotels", cancellationToken))
        {
            return Forbid();
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
            await _auditLogService.TryLogAdminActionAsync(
                GetUserId(),
                "hotel_deactivate",
                "oteller",
                hotelId.ToString(),
                result.Message,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
        }
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(HotelDetail), new { id = hotelId });
    }

    [HttpPost("oteller/aktive-et")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateHotel(long hotelId, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!await CanAccessAsync("admin.hotels", cancellationToken))
        {
            return Forbid();
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
            await _auditLogService.TryLogAdminActionAsync(
                GetUserId(),
                "hotel_activate",
                "oteller",
                hotelId.ToString(),
                result.Message,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
        }
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(HotelDetail), new { id = hotelId });
    }

    [HttpPost("oteller/toplu-yayin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdateHotelPublishStatus([FromForm] long[] hotelIds, [FromForm] bool publish, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }
        if (!await CanAccessAsync("admin.hotels", cancellationToken))
        {
            return Forbid();
        }

        if (!CanPerformCriticalAdminActions())
        {
            TempData["AdminHotelError"] = "Bu islem yalnizca admin yetkisi ile yapilabilir.";
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(Hotels));
        }

        var result = await _adminHotelManagementService.BulkUpdateHotelPublishStatusAsync(hotelIds, publish, GetUserId(), cancellationToken);
        TempData[result.Success ? "AdminHotelMessage" : "AdminHotelError"] = result.Message;
        if (result.Success)
        {
            await EvictPublicOutputCacheAsync(cancellationToken);
            var targetIds = (hotelIds ?? Array.Empty<long>()).Where(id => id > 0).Distinct().ToArray();
            await _auditLogService.TryLogAdminActionAsync(
                GetUserId(),
                publish ? "hotel_bulk_activate" : "hotel_bulk_deactivate",
                "oteller",
                string.Join(",", targetIds),
                result.Message,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
        }

        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Hotels));
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

        if (await RequirePermissionOrForbidAsync("admin.hotels", cancellationToken) is { } deniedHotelPh)
        {
            return deniedHotelPh;
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

        if (await RequirePermissionOrForbidAsync("admin.hotels", cancellationToken) is { } deniedHotelPhUp)
        {
            return deniedHotelPhUp;
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

        if (await RequirePermissionOrForbidAsync("admin.hotels", cancellationToken) is { } deniedHotelCover)
        {
            return deniedHotelCover;
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

        if (await RequirePermissionOrForbidAsync("admin.hotels", cancellationToken) is { } deniedHotelPhDel)
        {
            return deniedHotelPhDel;
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

        if (await RequirePermissionOrForbidAsync("admin.hotels", cancellationToken) is { } deniedRoomPh)
        {
            return deniedRoomPh;
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

        if (await RequirePermissionOrForbidAsync("admin.hotels", cancellationToken) is { } deniedRoomCover)
        {
            return deniedRoomCover;
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

        if (await RequirePermissionOrForbidAsync("admin.hotels", cancellationToken) is { } deniedRoomPhDel)
        {
            return deniedRoomPhDel;
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
    public async Task<IActionResult> Reservations([FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.reservations", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetUnifiedReservationsAsync(GetFullName(), GetEmail(), GetUserRole(), q, status, page, pageSize, cancellationToken);
        model.Shell.PanelTitle = "Rezervasyonlar";
        model.Shell.PanelSubtitle = "Bireysel, firma ve satış kaynaklı rezervasyonları tek operasyon tablosunda yönetin.";
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_unified_reservations_masaustu";
        ViewData["PageCssMobilePath"] = "admin_panel_unified_reservations_mobil";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/UnifiedReservations.cshtml", model);
    }

    [HttpGet("odemeler")]
    public async Task<IActionResult> Payments([FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? paymentType, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.payments", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetPaymentsAsync(GetFullName(), GetEmail(), GetUserRole(), q, status, paymentType, page, pageSize, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_payments_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/Payments.cshtml", model);
    }

    [HttpGet("faturalar")]
    public async Task<IActionResult> Invoices([FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? invoiceType, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.invoices", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetInvoicesAsync(GetFullName(), GetEmail(), GetUserRole(), q, status, invoiceType, page, pageSize, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_invoices_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/Invoices.cshtml", model);
    }

    [HttpGet("komisyonlar")]
    public async Task<IActionResult> Commissions([FromQuery] long? hotelId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] string? city, [FromQuery] string? district, [FromQuery] string? neighborhood, [FromQuery] string? paymentStatus, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var model = await _adminService.GetCommissionManagementAsync(GetFullName(), GetEmail(), GetUserRole(), hotelId, dateFrom, dateTo, city, district, neighborhood, paymentStatus, pageSize, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_commissions_page_masaustu";
        return View("~/Views/Paneller/Admin/Commissions.cshtml", model);
    }

    [HttpGet("komisyon-tahsilat")]
    public async Task<IActionResult> CommissionCollection(
        [FromQuery] string? donem,
        [FromQuery] string? city,
        [FromQuery] string? district,
        [FromQuery] string? neighborhood,
        [FromQuery] long? ilceId,
        [FromQuery] long? hotelId,
        [FromQuery] long? partnerId,
        [FromQuery] string? tahsilatStatus,
        [FromQuery] string? paymentStatus,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var filter = new AdminCommissionCollectionFilter
        {
            Donem = string.IsNullOrWhiteSpace(donem) ? DateTime.Today.ToString("yyyy-MM") : donem.Trim(),
            City = city ?? string.Empty,
            District = district ?? string.Empty,
            Neighborhood = neighborhood ?? string.Empty,
            IlceId = ilceId,
            HotelId = hotelId,
            PartnerId = partnerId,
            TahsilatStatus = tahsilatStatus ?? string.Empty,
            PaymentStatus = paymentStatus ?? string.Empty,
            SortBy = sortBy ?? "commission",
            SortDir = sortDir ?? "desc",
            Page = page,
            PageSize = pageSize
        };

        var model = await _adminService.GetCommissionCollectionLedgerAsync(GetFullName(), GetEmail(), GetUserRole(), filter, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_commission_collection_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/CommissionCollection.cshtml", model);
    }

    [HttpGet("komisyon-tahsilat/export.csv")]
    public async Task<IActionResult> ExportCommissionCollectionCsv(
        [FromQuery] string? donem,
        [FromQuery] string? city,
        [FromQuery] string? district,
        [FromQuery] string? neighborhood,
        [FromQuery] long? ilceId,
        [FromQuery] long? hotelId,
        [FromQuery] long? partnerId,
        [FromQuery] string? tahsilatStatus,
        [FromQuery] string? paymentStatus,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var filter = new AdminCommissionCollectionFilter
        {
            Donem = donem ?? string.Empty,
            City = city ?? string.Empty,
            District = district ?? string.Empty,
            Neighborhood = neighborhood ?? string.Empty,
            IlceId = ilceId,
            HotelId = hotelId,
            PartnerId = partnerId,
            TahsilatStatus = tahsilatStatus ?? string.Empty,
            PaymentStatus = paymentStatus ?? string.Empty,
            SortBy = sortBy ?? "commission",
            SortDir = sortDir ?? "desc"
        };

        var csv = await _adminService.ExportCommissionCollectionCsvAsync(filter, cancellationToken);
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(csv);
        var bytes = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
        var fileName = $"komisyon-tahsilat-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpPost("komisyon-tahsilat/tahsil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCommissionCollectionPaid(AdminCommissionCollectionMarkPaidForm request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminService.MarkCommissionCollectionPaidAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return Redirect("/admin/komisyon-tahsilat");
    }

    [HttpGet("sozlesmeler")]
    public async Task<IActionResult> Contracts([FromQuery] long? contractId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.contracts", cancellationToken) is { } deniedContract)
        {
            return deniedContract;
        }

        var model = await _contractContentService.GetAdminContractManagementAsync(GetFullName(), GetEmail(), GetUserRole(), contractId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_contracts_masaustu";
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

        if (await RequirePermissionOrForbidAsync("admin.contracts", cancellationToken) is { } deniedSaveContract)
        {
            return deniedSaveContract;
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

        if (await RequirePermissionOrForbidAsync("admin.contracts", cancellationToken) is { } deniedResend)
        {
            return deniedResend;
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

        if (await RequirePermissionOrForbidAsync("admin.contracts", cancellationToken) is { } deniedPdf)
        {
            return deniedPdf;
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
                    IF OBJECT_ID(N'[dbo].[SOZLESME_DOSYALARI]', N'U') IS NOT NULL
                    BEGIN
                        INSERT INTO [dbo].[SOZLESME_DOSYALARI] ([SOZLESME_ID], [DOSYA_TIPI], [DOSYA_ADI], [DOSYA_YOLU], [MIME_TIPI], [OLUSTURAN_KULLANICI_ID], [OLUSTURULMA_TARIHI])
                        VALUES (@contractId, N'pdf', @fileName, @fileUrl, N'application/pdf', @adminUserId, SYSUTCDATETIME());
                    END", connection);
                command.Parameters.AddWithValue("@contractId", contractId);
                command.Parameters.AddWithValue("@fileName", Path.GetFileName(pdfFile.FileName));
                command.Parameters.AddWithValue("@fileUrl", filePathOrUrl);
                command.Parameters.AddWithValue("@adminUserId", adminUserId);
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

        if (await RequirePermissionOrForbidAsync("admin.commissions", cancellationToken) is { } deniedRule)
        {
            return deniedRule;
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

        if (await RequirePermissionOrForbidAsync("admin.partner_applications", cancellationToken) is { } deniedPartner)
        {
            return deniedPartner;
        }

        var model = await _adminService.GetPartnerApplicationsAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_partner_applications_masaustu";
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

        if (await RequirePermissionOrForbidAsync("admin.partner_applications", cancellationToken) is { } deniedPartnerUp)
        {
            return deniedPartnerUp;
        }

        var result = await _adminService.ReviewPartnerApplicationAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(PartnerApplications));
    }

    [HttpGet("partner-evraklari")]
    public async Task<IActionResult> PartnerDocuments([FromQuery] string? status, [FromQuery] long? partnerId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.partner_applications", cancellationToken) is { } deniedDocs)
        {
            return deniedDocs;
        }

        var model = await _adminService.GetPartnerDocumentsReviewQueueAsync(GetFullName(), GetEmail(), GetUserRole(), status, GetUserId(), cancellationToken);
        if (partnerId is > 0)
        {
            model.Queue = model.Queue.Where(q => q.PartnerId == partnerId.Value).ToList();
        }

        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_partner_documents_masaustu";
        ViewData["PageCssMobilePath"] = "admin_panel_partner_documents_mobil";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/PartnerDocuments.cshtml", model);
    }

    [HttpPost("partner-evraklari/durum")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewPartnerDocument(AdminPartnerDocumentReviewRequest request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.partner_applications", cancellationToken) is { } deniedReview)
        {
            return deniedReview;
        }

        var result = await _adminService.ReviewPartnerDocumentAsync(GetUserId(), request, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(PartnerDocuments), new { partnerId = request.PartnerId });
    }

    [HttpPost("partner-basvurulari/eposta-giris-onayi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPartnerEmailLoginApproval(AdminPartnerEmailLoginApprovalRequest request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.partner_applications", cancellationToken) is { } deniedPartnerMail)
        {
            return deniedPartnerMail;
        }

        var result = await _adminService.SetPartnerEmailLoginApprovalAsync(GetUserId(), request, cancellationToken);
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
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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

        if (await RequirePermissionOrForbidAsync("admin.company_applications", cancellationToken) is { } deniedCompanyUp)
        {
            return deniedCompanyUp;
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

        if (await RequirePermissionOrForbidAsync("admin.listing_subscriptions", cancellationToken) is { } deniedListing)
        {
            return deniedListing;
        }

        var model = await _adminService.GetListingSubscriptionsAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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

        if (await RequirePermissionOrForbidAsync("admin.listing_subscriptions", cancellationToken) is { } deniedListingUp)
        {
            return deniedListingUp;
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

    [HttpGet("platform-paketleri")]
    public async Task<IActionResult> PlatformPackages(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.platform_packages", cancellationToken) is { } denied)
        {
            return denied;
        }

        var model = await _platformPackageService.GetAdminPageAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_platform_packages_masaustu";
        ViewData["PageCssMobilePath"] = "admin_panel_platform_packages_mobil";
        return View("~/Views/Paneller/Admin/PlatformPackages.cshtml", model);
    }

    [HttpGet("platform-paketleri/basvurular.csv")]
    public async Task<IActionResult> ExportPlatformPackageApplicationsCsv(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.platform_packages", cancellationToken) is { } deniedCsv)
        {
            return deniedCsv;
        }

        var csv = await _platformPackageService.ExportAdminApplicationsCsvAsync(cancellationToken);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var fileName = $"platform-paket-basvurulari-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpPost("platform-paketleri/basvuru-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePlatformPackageApplication(AdminPlatformPackageApplicationDecisionRequest request, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.platform_packages", cancellationToken) is { } denied)
        {
            return denied;
        }

        var result = await _platformPackageService.ReviewApplicationAsync(GetUserId(), request, cancellationToken);
        if (result.Success)
        {
            try
            {
                await _auditLogService.TryLogAdminActionAsync(
                    GetUserId(),
                    $"platform_package_{request.Action}",
                    "partner_paket_basvurulari",
                    request.ApplicationId.ToString(),
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
        return RedirectToAction(nameof(PlatformPackages));
    }

    [HttpGet("gelistirme-talepleri")]
    public async Task<IActionResult> DevelopmentRequests([FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] long? developerUserId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.development_requests", cancellationToken) is { } deniedDev)
        {
            return deniedDev;
        }

        var model = await _developmentRequestService.GetAdminPageAsync(GetFullName(), GetEmail(), GetUserRole(), q, status, priority, developerUserId, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_development_masaustu";
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

        if (await RequirePermissionOrForbidAsync("admin.development_requests", cancellationToken) is { } deniedDevSave)
        {
            return deniedDevSave;
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

        if (await RequirePermissionOrForbidAsync("admin.development_requests", cancellationToken) is { } deniedDevDel)
        {
            return deniedDevDel;
        }

        var result = await _developmentRequestService.DeleteRequestAsync(GetUserId(), requestId, note, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(DevelopmentRequests));
    }

    [HttpGet("platform-yetkilileri")]
    public Task<IActionResult> PlatformOfficials(CancellationToken cancellationToken) => RenderSectionAsync("platform-officials", "PlatformOfficials", cancellationToken);

    [HttpGet("acik-oteller")]
    public async Task<IActionResult> ActiveHotels([FromQuery] string? q, [FromQuery] string? city, [FromQuery] string? district, [FromQuery] string? neighborhood, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.hotels", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminHotelManagementService.GetHotelsPageAsync(GetFullName(), GetEmail(), GetUserRole(), q, city, district, neighborhood, "Yayında", "Onaylandı", page, pageSize, cancellationToken);
        model.Shell.PanelTitle = "Açık Oteller";
        model.Shell.PanelSubtitle = "Yayında ve admin onaylı tesisleri; yayın kapatma, detay ve komisyon takibi için yönetin.";
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_hotels_masaustu";
        return View("~/Views/Paneller/Admin/Hotels.cshtml", model);
    }

    [HttpGet("bekleyen-oteller")]
    public async Task<IActionResult> PendingHotels([FromQuery] string? q, [FromQuery] string? city, [FromQuery] string? district, [FromQuery] string? neighborhood, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.hotels", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminHotelManagementService.GetHotelsPageAsync(GetFullName(), GetEmail(), GetUserRole(), q, city, district, neighborhood, string.Empty, "Beklemede", page, pageSize, cancellationToken);
        model.Shell.PanelTitle = "Bekleyen Oteller";
        model.Shell.PanelSubtitle = "Evrak, otel bilgisi, komisyon ve yayın kararı bekleyen tesisleri tek tabloda inceleyin.";
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_hotels_masaustu";
        return View("~/Views/Paneller/Admin/Hotels.cshtml", model);
    }

    [HttpGet("degerlendirmeler")]
    public async Task<IActionResult> Reviews(string? q = null, string? city = null, string? hotel = null, int take = 20, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.reviews", cancellationToken) is { } deniedRev)
        {
            return deniedRev;
        }

        var model = await _adminService.GetReviewModerationPageAsync(GetFullName(), GetEmail(), GetUserRole(), q, city, hotel, take, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/ReviewsModeration.cshtml", model);
    }

    [HttpPost("degerlendirmeler/islem")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewModerationAction(AdminReviewModerationActionForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.reviews", cancellationToken) is { } deniedRevAct)
        {
            return deniedRevAct;
        }

        var result = await _adminService.ApplyReviewModerationActionAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "AdminSuccess" : "AdminError"] = result.Message;
        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }
        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost("degerlendirmeler/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(AdminReviewDeleteForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        var result = await _adminService.DeleteReviewAsAdminAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "AdminSuccess" : "AdminError"] = result.Message;
        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }
        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost("degerlendirmeler/ihlali-bildir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotifyReviewViolation(AdminReviewViolationNotifyForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.reviews", cancellationToken) is { } deniedRevNtf)
        {
            return deniedRevNtf;
        }

        var result = await _adminService.NotifyReviewViolationAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "AdminSuccess" : "AdminError"] = result.Message;
        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }
        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost("degerlendirmeler/yasakli-kelime/ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBlockedWord(AdminBlockedWordAddForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.reviews", cancellationToken) is { } deniedBwAdd)
        {
            return deniedBwAdd;
        }

        var result = await _adminService.AddBlockedWordAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "AdminSuccess" : "AdminError"] = result.Message;
        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }
        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost("degerlendirmeler/yasakli-kelime/durum")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlockedWord(AdminBlockedWordToggleForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.reviews", cancellationToken) is { } deniedBwTgl)
        {
            return deniedBwTgl;
        }

        var result = await _adminService.ToggleBlockedWordAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "AdminSuccess" : "AdminError"] = result.Message;
        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }
        return RedirectToAction(nameof(Reviews));
    }

    [HttpGet("gelir-merkezi")]
    [HttpGet("revenue-command-center")]
    public async Task<IActionResult> RevenueCommandCenter(CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.reports", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetRevenueCommandCenterAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_revenue_command_center_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/RevenueCommandCenter.cshtml", model);
    }

    [HttpGet("raporlar")]
    public async Task<IActionResult> Reports([FromQuery] long? hotelId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.reports", cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetReportsAsync(GetFullName(), GetEmail(), GetUserRole(), hotelId, dateFrom, dateTo, page, pageSize, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_reports_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/Reports.cshtml", model);
    }

    [HttpGet("raporlar/aylik-ciro-komisyon.csv")]
    public async Task<IActionResult> ExportMonthlyRevenueCommissionCsv(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!await CanAccessAsync("admin.reports", cancellationToken))
        {
            return Forbid();
        }

        var csv = await _adminService.ExportMonthlyHotelRevenueCommissionCsvAsync(cancellationToken);
        var preamble = System.Text.Encoding.UTF8.GetPreamble();
        var body = System.Text.Encoding.UTF8.GetBytes(csv);
        var bytes = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
        var fileName = $"aylik-otel-ciro-komisyon-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

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

        if (await RequirePermissionOrForbidAsync("admin.whatsapp", cancellationToken) is { } deniedWa)
        {
            return deniedWa;
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

        if (await RequirePermissionOrForbidAsync("admin.whatsapp", cancellationToken) is { } deniedWaSave)
        {
            return deniedWaSave;
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

        if (await RequirePermissionOrForbidAsync("admin.whatsapp", cancellationToken) is { } deniedWaTest)
        {
            return deniedWaTest;
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
    public async Task<IActionResult> EmailTemplates(CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.email_templates", cancellationToken) is { } deniedTpl)
        {
            return deniedTpl;
        }

        var model = await _adminService.GetEmailSettingsPageAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/EmailTemplates.cshtml", model);
    }

    [HttpPost("eposta-sablonlari/test-gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTemplateTestBatch([FromForm] string recipientEmail, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.email_templates", cancellationToken) is { } deniedTplTest)
        {
            return deniedTplTest;
        }

        var result = await _adminService.QueueTemplateTestBatchAsync(GetUserId(), recipientEmail, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(EmailTemplates));
    }

    [HttpGet("mail-merkezi")]
    public async Task<IActionResult> MailCenter([FromQuery] long? accountId, [FromQuery] bool sync = false, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.mail_center", cancellationToken) is { } deniedMailCenter)
        {
            return deniedMailCenter;
        }

        var model = await _adminService.GetMailCenterAsync(GetFullName(), GetEmail(), GetUserRole(), accountId, sync, cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_mail_center_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/MailCenter.cshtml", model);
    }

    [HttpGet("eposta-yonlendirmeleri")]
    public async Task<IActionResult> EmailRouting(CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.email_routing", cancellationToken) is { } deniedRoute)
        {
            return deniedRoute;
        }

        var model = await _adminEmailRoutingService.GetPageAsync(GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_email_routing_masaustu";
        ViewData["AdminShell"] = model.Shell;
        return View("~/Views/Paneller/Admin/EmailRouting.cshtml", model);
    }

    [HttpPost("eposta-yonlendirmeleri/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEmailRouting(AdminEmailRoutingSaveForm form, CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.email_routing", cancellationToken) is { } deniedRouteSave)
        {
            return deniedRouteSave;
        }

        var result = await _adminEmailRoutingService.SaveAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(EmailRouting));
    }

    [HttpPost("mail-merkezi/hesap-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMailAccount(AdminMailAccountForm form, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.mail_center", cancellationToken) is { } deniedMcSave)
        {
            return deniedMcSave;
        }

        var result = await _adminService.SaveMailAccountAsync(GetUserId(), form, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(MailCenter), new { accountId = form.Id });
    }

    [HttpPost("mail-merkezi/hesap-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMailAccount([FromForm] long id, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.mail_center", cancellationToken) is { } deniedMcDel)
        {
            return deniedMcDel;
        }

        var result = await _adminService.DeleteMailAccountAsync(GetUserId(), id, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(MailCenter));
    }

    [HttpPost("mail-merkezi/senkronize")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncMailAccount([FromForm] long accountId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.mail_center", cancellationToken) is { } deniedMcSync)
        {
            return deniedMcSync;
        }

        var result = await _adminService.SyncMailAccountAsync(GetUserId(), accountId, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Success
            ? $"{result.Message} İçeri alınan yeni mesaj: {result.ImportedCount}"
            : result.Message;
        return RedirectToAction(nameof(MailCenter), new { accountId });
    }

    [HttpPost("eposta-kuyrugu/retry-all-failed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryAllFailedEmails([FromForm] string? reason, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (!TryValidateCriticalReason(reason, out var err))
        {
            TempData["AdminError"] = err;
            return RedirectToAction(nameof(EmailTemplates));
        }

        var result = await _adminService.RetryAllFailedEmailsAsync(GetUserId(), reason!, cancellationToken);
        TempData[result.Success ? "AdminMessage" : "AdminError"] = result.Message;
        await _auditLogService.TryLogAdminActionAsync(GetUserId(), "email_retry_all_failed", "bildirim_loglari", result.RetriedCount.ToString(), $"Gerekçe: {reason}", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return RedirectToAction(nameof(EmailTemplates));
    }

    [HttpGet("sss")]
    public Task<IActionResult> Faq(CancellationToken cancellationToken) => RenderSectionAsync("faq", "Faq", cancellationToken);

    [HttpGet("destek-makaleleri")]
    public async Task<IActionResult> SupportArticles([FromQuery] string? q, [FromQuery] long? kategoriId, [FromQuery] string? durum, [FromQuery] long? editId, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return RedirectToAction("UserLogin", "Auth");
        }

        if (await RequirePermissionOrForbidAsync("admin.support_articles", cancellationToken) is { } deniedSa)
        {
            return deniedSa;
        }

        var section = await _adminService.GetSectionPageAsync("faq", GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        section.Shell.PanelTitle = "Destek Makaleleri";
        section.Shell.PanelSubtitle = "Yardım merkezi içeriklerini tek yerden ekleyin, güncelleyin ve kaldırın.";

        var model = await _adminSupportArticleService.GetPageAsync(section.Shell, q, kategoriId, durum, editId, cancellationToken);
        ViewData["Title"] = section.Shell.PanelTitle;
        ViewData["PageCssPath"] = "admin_panel_section_masaustu";
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

        if (await RequirePermissionOrForbidAsync("admin.support_articles", cancellationToken) is { } deniedSaDel)
        {
            return deniedSaDel;
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

        // RBAC (menü + endpoint): sectionKey -> permission mapping
        var permission = sectionKey switch
        {
            "users" => "admin.users",
            "managers" => "admin.managers",
            "platform-officials" => "admin.platform_officials",
            "reservations" => "admin.reservations",
            "payments" => "admin.payments",
            "invoices" => "admin.invoices",
            "active-hotels" => "admin.hotels",
            "pending-hotels" => "admin.hotels",
            "reports" => "admin.reports",
            "campaigns" => "admin.hotels",
            "notifications" => "admin.notifications",
            "settings" => "admin.settings",
            "security" => "admin.security",
            "blog" => "admin.blog",
            "faq" => "admin.faq",
            "complaints" => "admin.complaints",
            "logs" => "admin.logs",
            "geo-search-logs" => "admin.geo_search_logs",
            "hotel-coordinate-changes" => "admin.hotel_coord_changes",
            "company-reservations" => "admin.company_reservations",
            "backups" => "admin.backups",
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(permission))
        {
            return Forbid();
        }
        if (!await CanAccessAsync(permission, cancellationToken))
        {
            return Forbid();
        }

        var model = await _adminService.GetSectionPageAsync(sectionKey, GetFullName(), GetEmail(), GetUserRole(), cancellationToken);
        ViewData["Title"] = model.Shell.PanelTitle;
        ViewData["PageCssPath"] = string.Equals(sectionKey, "users", StringComparison.OrdinalIgnoreCase)
            ? "admin_panel_users_masaustu"
            : "admin_panel_section_masaustu";
        ViewData["PageCssMobile"] = string.Equals(sectionKey, "users", StringComparison.OrdinalIgnoreCase)
            ? "admin_panel_users_mobil"
            : "admin_panel_section_mobil";
        if (string.Equals(sectionKey, "reports", StringComparison.OrdinalIgnoreCase))
        {
            ViewData["MonthlyCsvExportUrl"] = Url.Action(nameof(ExportMonthlyRevenueCommissionCsv), "AdminPanel");
        }

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

    private async Task<bool> CanAccessAsync(string permissionCode, CancellationToken cancellationToken)
    {
        if (!CanAccessAdminPanel())
        {
            return false;
        }

        var uid = GetUserId();
        if (uid <= 0)
        {
            return false;
        }

        return await _adminRbacService.HasPermissionAsync(uid, GetUserRole(), permissionCode, cancellationToken);
    }

    private async Task<IActionResult?> RequirePermissionOrForbidAsync(string permissionCode, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(permissionCode, cancellationToken))
        {
            return Forbid();
        }

        return null;
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
        return User.FindFirstValue(AuthClaimTypes.FullName) ?? User.Identity?.Name ?? "Admin kullanıcı";
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
