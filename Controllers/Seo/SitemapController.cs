using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Seo;

[AllowAnonymous]
public class SitemapController : Controller
{
    private readonly ISitemapService _sitemapService;

    public SitemapController(ISitemapService sitemapService)
    {
        _sitemapService = sitemapService;
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> Xml(CancellationToken cancellationToken)
    {
        var xml = await _sitemapService.GetSitemapXmlAsync(cancellationToken);
        return Content(xml, "application/xml; charset=utf-8");
    }

    [HttpGet("/seo/sitemap-refresh")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Refresh([FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        await _sitemapService.EnsureFreshSitemapAsync(force, cancellationToken);
        return NoContent();
    }
}
