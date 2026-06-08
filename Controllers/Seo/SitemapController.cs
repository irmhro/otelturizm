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

    [HttpGet("/sitemaps/{fileName}.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> SubSitemapXml(string fileName, CancellationToken cancellationToken)
    {
        var xml = await _sitemapService.GetSubSitemapXmlAsync(fileName, cancellationToken);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return NotFound();
        }

        return Content(xml, "application/xml; charset=utf-8");
    }

    [HttpGet("/xml/{fileName}.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> RegionalXml(string fileName, CancellationToken cancellationToken)
    {
        var xml = await _sitemapService.GetRegionalSitemapXmlAsync(fileName, cancellationToken);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return NotFound();
        }

        return Content(xml, "application/xml; charset=utf-8");
    }

    [HttpGet("/feeds/hotel-offers.json")]
    [ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> HotelOffersFeed(CancellationToken cancellationToken)
    {
        var json = await _sitemapService.GetHotelOffersFeedJsonAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return NotFound();
        }

        return Content(json, "application/json; charset=utf-8");
    }

    [HttpGet("/seo/sitemap-refresh")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Refresh([FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        await _sitemapService.EnsureFreshSitemapAsync(force, cancellationToken);
        return NoContent();
    }
}
