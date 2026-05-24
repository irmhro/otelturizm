using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.SeyahatPlanlama;

[Route("seyahat-planlama")]
public class SeyahatPlanlamaController : Controller
{
    private readonly ISeyahatPlanlamaService _seyahatPlanlamaService;

    public SeyahatPlanlamaController(ISeyahatPlanlamaService seyahatPlanlamaService)
    {
        _seyahatPlanlamaService = seyahatPlanlamaService;
    }

    [HttpGet("")]
    [OutputCache(PolicyName = "public-medium")]
    public async Task<IActionResult> Index(
        [FromQuery(Name = "dest")] string? destinationKey,
        [FromQuery(Name = "gece")] int? nights,
        [FromQuery(Name = "butce")] decimal? budgetTry,
        [FromQuery(Name = "tahmin")] bool estimate,
        CancellationToken cancellationToken)
    {
        var model = await _seyahatPlanlamaService.BuildPageAsync(
            destinationKey,
            nights,
            budgetTry,
            estimate,
            User?.Identity?.IsAuthenticated == true,
            cancellationToken);

        ViewData["Title"] = "Seyahat Planlama";
        ViewData["PageCss"] = "seyahat-planlama";
        ViewData["PageCssMobile"] = "seyahat-planlama.mobile";
        ViewData["IncludeFeWorldTokens"] = true;
        ViewData["MetaDescription"] = model.MetaDescription;

        return View("~/Views/SeyahatPlanlama/Index.cshtml", model);
    }
}
