using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.ViewComponents;

public sealed class UtilityPulseBarViewComponent : ViewComponent
{
    private readonly IUtilityPulseService _utilityPulseService;

    public UtilityPulseBarViewComponent(IUtilityPulseService utilityPulseService)
    {
        _utilityPulseService = utilityPulseService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _utilityPulseService.GetBarAsync(HttpContext.RequestAborted);
        return View(model);
    }
}
