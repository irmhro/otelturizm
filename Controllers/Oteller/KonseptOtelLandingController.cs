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
        => RedirectPermanent("/hotel?etiket=havuzlu-oteller");

    [HttpGet("/hafta-sonu-firsatlari")]
    [OutputCache(PolicyName = "public-short")]
    public IActionResult HaftaSonuFirsatlari()
        => RedirectPermanent("/hotel?etiket=hafta-sonu-firsatlari");

    [HttpGet("/evcil-hayvan-dostu-oteller")]
    [OutputCache(PolicyName = "public-short")]
    public IActionResult EvcilHayvanDostuOteller()
        => RedirectPermanent("/hotel?etiket=evcil-hayvan-dostu");
}
