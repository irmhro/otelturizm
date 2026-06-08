using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Deneyimler;

[Route("deneyimler")]
public class DeneyimlerController : Controller
{
    private readonly IDeneyimlerService _deneyimlerService;

    public DeneyimlerController(IDeneyimlerService deneyimlerService)
    {
        _deneyimlerService = deneyimlerService;
    }

    [HttpGet("")]
    [OutputCache(PolicyName = "public-medium")]
    public IActionResult Index()
    {
        var model = _deneyimlerService.GetPage();
        var baseUrl = (HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration)?["App:PublicBaseUrl"]?.TrimEnd('/')
                      ?? $"{Request.Scheme}://{Request.Host}";

        ViewData["Title"] = "Deneyimler";
        ViewData["BodyClass"] = "deneyimler-page anasayfa-page";
        ViewData["PageCss"] = "deneyimler";
        ViewData["PageCssMobile"] = "deneyimler";
        ViewData["IncludeAnasayfaStyles"] = true;
        ViewData["IncludeFeWorldTokens"] = true;
        ViewData["MetaDescription"] = "Otelturizm Deneyimler: şehir rotaları, sahil kaçamakları, spa ritüelleri ve aile dostu maceralar. Konaklamanızı anıya dönüştüren koleksiyonlar.";
        ViewData["Canonical"] = $"{baseUrl}/deneyimler";

        return View("~/Views/Deneyimler/Index.cshtml", model);
    }
}
