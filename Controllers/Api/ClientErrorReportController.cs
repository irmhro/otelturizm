using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace otelturizmnew.Controllers.Api;

/// <summary>
/// Tarayıcı JS hatalarını örneklemli ve sıkı rate-limit ile loglar (Serilog dosyalarına düşer).
/// Güvenlik olayları ekranı SECURITY_EVENT satırlarını okuyabilir.
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[EnableRateLimiting("client-error-ingest")]
public sealed class ClientErrorReportController : ControllerBase
{
    private readonly ILogger<ClientErrorReportController> _logger;

    public ClientErrorReportController(ILogger<ClientErrorReportController> logger)
    {
        _logger = logger;
    }

    [HttpPost("/diagnostics/client-error")]
    public IActionResult Ingest([FromBody] ClientErrorPayload? payload)
    {
        if (payload is null)
        {
            return BadRequest(new { ok = false });
        }

        var safe = ClientErrorPayload.Normalize(payload);
        if (string.IsNullOrWhiteSpace(safe.Message))
        {
            return BadRequest(new { ok = false });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "-";
        var ua = Request.Headers.UserAgent.ToString();
        if (ua.Length > 256)
        {
            ua = ua[..256];
        }

        _logger.LogWarning(
            "SECURITY_EVENT kind=CLIENT_JS_ERROR route={Route} msg={Msg} stack={Stack} line={Line} col={Col} ip={Ip} ua={Ua}",
            safe.Route,
            safe.Message,
            safe.Stack,
            safe.Line,
            safe.Column,
            ip,
            ua);

        return Ok(new { ok = true });
    }
}

public sealed class ClientErrorPayload
{
    public string? Message { get; set; }
    public string? Stack { get; set; }
    public string? Source { get; set; }
    public int? Line { get; set; }
    public int? Column { get; set; }
    public string? Route { get; set; }
    public string? Page { get; set; }

    internal static ClientErrorPayload Normalize(ClientErrorPayload raw)
    {
        static string Clip(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            var t = s.Trim();
            return t.Length <= max ? t : t[..max];
        }

        return new ClientErrorPayload
        {
            Message = Clip(raw.Message, 500),
            Stack = Clip(raw.Stack, 1500),
            Source = Clip(raw.Source, 400),
            Line = raw.Line is >= 0 and < 1_000_000 ? raw.Line : null,
            Column = raw.Column is >= 0 and < 1_000_000 ? raw.Column : null,
            Route = Clip(raw.Route, 256),
            Page = Clip(raw.Page, 512)
        };
    }
}
