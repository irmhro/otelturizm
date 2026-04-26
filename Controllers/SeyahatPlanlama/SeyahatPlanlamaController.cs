using Microsoft.AspNetCore.Mvc;

namespace otelturizmnew.Controllers.SeyahatPlanlama;

[Route("seyahat-planlama")]
public class SeyahatPlanlamaController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Seyahat Planlama";
        ViewData["PageCss"] = "seyahat-planlama";
        return View("~/Views/SeyahatPlanlama/Index.cshtml");
    }
}

