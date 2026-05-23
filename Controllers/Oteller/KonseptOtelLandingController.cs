using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace otelturizmnew.Controllers.Oteller;

/// <summary>
/// Proje şablonlarından eksik konsept landing URL'leri — liste <c>?etiket=</c> filtresine kalıcı yönlendirme.
/// </summary>
public class KonseptOtelLandingController : Controller
{
    [HttpGet("/havuzlu-oteller")]
    [OutputCache(PolicyName = "public-short")]
    public IActionResult HavuzluOteller()
        => RedirectPermanent("/oteller?etiket=havuzlu-oteller");

    [HttpGet("/hafta-sonu-firsatlari")]
    [OutputCache(PolicyName = "public-short")]
    public IActionResult HaftaSonuFirsatlari()
        => RedirectPermanent("/oteller?etiket=hafta-sonu-firsatlari");

    [HttpGet("/evcil-hayvan-dostu-oteller")]
    [OutputCache(PolicyName = "public-short")]
    public IActionResult EvcilHayvanDostuOteller()
        => RedirectPermanent("/oteller?etiket=evcil-hayvan-dostu");
}
