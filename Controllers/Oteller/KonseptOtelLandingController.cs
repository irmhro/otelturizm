using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace otelturizmnew.Controllers.Oteller;

/// <summary>
/// Proje şablonlarından eksik konsept landing URL'leri — liste <c>?etiket=</c> filtresine kalıcı yönlendirme.
/// </summary>
[Route("havuzlu-oteller")]
public class KonseptOtelLandingController : Controller
{
    [HttpGet("")]
    [OutputCache(PolicyName = "public-short")]
    public IActionResult HavuzluOteller()
        => RedirectPermanent("/oteller?etiket=havuzlu-oteller");
}
