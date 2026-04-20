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
        ViewData["PageCss"] = "legal-contract";
        return View("~/Views/Legal/ContractDetail.cshtml", model);
    }
}
