using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace otelturizmnew.Controllers.SeyahatPlanlama;

[Route("seyahat-planlama")]
public class SeyahatPlanlamaController : Controller
{
    [HttpGet("")]
    [OutputCache(PolicyName = "public-medium")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Seyahat Planlama";
        ViewData["PageCss"] = "seyahat-planlama";
        return View("~/Views/SeyahatPlanlama/Index.cshtml");
    }
}

