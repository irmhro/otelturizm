using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Firma;

public class FirmaController : Controller
{
    private readonly IFirmaService _firmaService;

    public FirmaController(IFirmaService firmaService)
    {
        _firmaService = firmaService;
    }

    [HttpGet("/firma")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _firmaService.GetLandingPageAsync(cancellationToken);
        ViewData["Title"] = "Firma";
        ViewData["IncludeAnasayfaStyles"] = true;
        return View("~/Views/Firma/Firma.cshtml", model);
    }
}
