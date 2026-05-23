using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Kurumsal;

public class KurumsalController : Controller
{
    private readonly ISupportService _supportService;

    public KurumsalController(ISupportService supportService)
    {
        _supportService = supportService;
    }

    [HttpGet("/kurumsal")]
    public IActionResult Index()
    {
        ViewData["PageCss"] = "kurumsal";
        return View("~/Views/Kurumsal/Kurumsal.cshtml");
    }

    [HttpGet("/hakkimizda")]
    public IActionResult Hakkimizda()
    {
        ViewData["PageCss"] = "kurumsal-hakkimizda";
        ViewData["IncludeFeWorldTokens"] = true;
        return View("~/Views/Kurumsal/Hakkimizda.cshtml");
    }

    [HttpGet("/kariyer")]
    public IActionResult Kariyer()
    {
        ViewData["PageCss"] = "kurumsal-kariyer";
        ViewData["IncludeFeWorldTokens"] = true;
        return View("~/Views/Kurumsal/Kariyer.cshtml");
    }

    [HttpGet("/basin-odasi")]
    public IActionResult BasinOdasi()
    {
        ViewData["PageCss"] = "kurumsal-basin-odasi";
        ViewData["IncludeFeWorldTokens"] = true;
        return View("~/Views/Kurumsal/BasinOdasi.cshtml");
    }

    [HttpGet("/blog")]
    public async Task<IActionResult> Blog(CancellationToken cancellationToken)
    {
        ViewData["PageCss"] = "kurumsal-blog";
        ViewData["IncludeFeWorldTokens"] = true;
        var model = await _supportService.GetCompanyBlogListingAsync(cancellationToken);
        return View("~/Views/Kurumsal/Blog.cshtml", model);
    }

    [HttpGet("/blog/{slug}")]
    public async Task<IActionResult> BlogDetay(string slug, CancellationToken cancellationToken)
    {
        var model = await _supportService.GetHelpContentPageAsync("blog", slug, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        ViewData["Title"] = model.Title;
        ViewData["PageCss"] = "kurumsal-blog";
        ViewData["IncludeFeWorldTokens"] = true;
        ViewData["MetaDescription"] = model.Summary;
        return View("~/Views/Kurumsal/BlogDetay.cshtml", model);
    }

}
