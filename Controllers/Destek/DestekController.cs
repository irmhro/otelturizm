using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Destek;

public class DestekController : Controller
{
    private readonly ISupportService _supportService;

    public DestekController(ISupportService supportService)
    {
        _supportService = supportService;
    }

    [HttpGet("/yardim-merkezi")]
    public async Task<IActionResult> YardimMerkezi(string? ara, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Yardım Merkezi";
        ViewData["PageCss"] = "yardim-merkezi";
        var model = await _supportService.GetHelpCenterAsync(ara, cancellationToken);
        return View("~/Views/Destek/YardimMerkezi.cshtml", model);
    }

    [HttpGet("/yardim-merkezi/kategori/{slug}")]
    public async Task<IActionResult> Kategori(string slug, string? ara, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Yardım Merkezi";
        ViewData["PageCss"] = "yardim-merkezi";
        var model = await _supportService.GetHelpCategoryAsync(slug, ara, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }
        return View("~/Views/Destek/YardimKategori.cshtml", model);
    }

    [HttpGet("/yardim-merkezi/sayfa/{type}/{slug}")]
    public async Task<IActionResult> Sayfa(string type, string slug, CancellationToken cancellationToken)
    {
        var t = (type ?? string.Empty).Trim().ToLowerInvariant();
        var s = (slug ?? string.Empty).Trim().ToLowerInvariant();
        if (t == "about" && s == "hakkimizda")
        {
            return RedirectPermanent("/hakkimizda");
        }

        if (t == "career" && s == "kariyer")
        {
            return RedirectPermanent("/kariyer");
        }

        if (t == "press" && s == "basin-odasi")
        {
            return RedirectPermanent("/basin-odasi");
        }

        if (t == "blog" && s == "blog")
        {
            return RedirectPermanent("/blog");
        }

        if (t == "blog" && !string.IsNullOrWhiteSpace(s) && !string.IsNullOrWhiteSpace(slug))
        {
            return RedirectPermanent($"/blog/{Uri.EscapeDataString(slug.Trim())}");
        }

        ViewData["Title"] = "Yardım Merkezi";
        ViewData["PageCss"] = "yardim-merkezi";
        var model = await _supportService.GetHelpContentPageAsync(t, s, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }
        ViewData["Title"] = model.Title;
        return View("~/Views/Destek/YardimSayfa.cshtml", model);
    }

    [HttpGet("/sss")]
    public async Task<IActionResult> Sss(string? kategori, string? ara, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Sıkça Sorulan Sorular";
        ViewData["PageCssBase"] = "yardim-merkezi";
        ViewData["PageCss"] = "sss";
        var model = await _supportService.GetFaqPageAsync(kategori, ara, cancellationToken);
        return View("~/Views/Destek/Sss.cshtml", model);
    }
}
