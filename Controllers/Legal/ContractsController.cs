using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Legal;

[Route("sozlesmeler")]
public class ContractsController : Controller
{
    private readonly IContractContentService _contractContentService;

    public ContractsController(IContractContentService contractContentService)
    {
        _contractContentService = contractContentService;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        var model = await _contractContentService.GetPublicContractBySlugAsync(slug, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        ViewData["Title"] = model.Title;
        ViewData["PageCss"] = "sozlesme";
        ViewData["PageCssMobile"] = "sozlesme";
        return View("~/Views/Sozlesmeler/Sozlesme.cshtml", model);
    }

    [HttpGet("{slug}/pdf")]
    public async Task<IActionResult> Pdf(string slug, CancellationToken cancellationToken)
    {
        var pdfUrl = await _contractContentService.GetPublicContractPdfUrlBySlugAsync(slug, cancellationToken);
        if (!string.IsNullOrWhiteSpace(pdfUrl))
        {
            return Redirect(pdfUrl);
        }

        return RedirectToAction(nameof(Detail), new { slug });
    }
}
