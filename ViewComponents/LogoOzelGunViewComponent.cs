using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.ViewComponents;

public sealed class LogoOzelGunViewComponent : ViewComponent
{
    private readonly IOzelGunService _ozelGunService;

    public LogoOzelGunViewComponent(IOzelGunService ozelGunService)
    {
        _ozelGunService = ozelGunService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _ozelGunService.GetTodayAsync(HttpContext.RequestAborted);
        return View(model);
    }
}
