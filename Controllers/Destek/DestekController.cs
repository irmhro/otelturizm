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

    [HttpGet("/sss")]
    public async Task<IActionResult> Sss(string? kategori, string? ara, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Sıkça Sorulan Sorular";
        ViewData["PageCss"] = "sss";
        var model = await _supportService.GetFaqPageAsync(kategori, ara, cancellationToken);
        return View("~/Views/Destek/Sss.cshtml", model);
    }
}
