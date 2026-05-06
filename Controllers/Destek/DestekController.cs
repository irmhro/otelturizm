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
        ViewData["Title"] = "Yardım Merkezi";
        ViewData["PageCss"] = "yardim-merkezi";
        var model = await _supportService.GetHelpContentPageAsync(type, slug, cancellationToken);
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
        ViewData["PageCss"] = "sss";
        var model = await _supportService.GetFaqPageAsync(kategori, ara, cancellationToken);
        return View("~/Views/Destek/Sss.cshtml", model);
    }
}
