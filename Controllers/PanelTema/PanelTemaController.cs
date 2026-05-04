using Microsoft.AspNetCore.Mvc;

namespace otelturizmnew.Controllers.PanelTema;

[Route("paneltema")]
public sealed class PanelTemaController : Controller
{
    private static bool IsLoopbackHost(string host)
        => string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
           || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
           || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
           || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);

    [HttpGet("")]
    [HttpGet("index")]
    [HttpGet("index.html")]
    public IActionResult Index()
    {
        var host = Request.Host.Host ?? string.Empty;
        if (!IsLoopbackHost(host))
        {
            return NotFound();
        }

        // Tabler preview static dosyaları wwwroot/paneltema altında durur.
        return Redirect("/paneltema/pages/index.html");
    }
}

