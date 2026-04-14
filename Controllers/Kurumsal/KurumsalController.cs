using Microsoft.AspNetCore.Mvc;

namespace otelturizmnew.Controllers.Kurumsal;

public class KurumsalController : Controller
{
    [HttpGet("/kurumsal")]
    public IActionResult Index()
    {
        ViewData["PageCss"] = "kurumsal";
        return View("~/Views/Kurumsal/Kurumsal.cshtml");
    }
}
