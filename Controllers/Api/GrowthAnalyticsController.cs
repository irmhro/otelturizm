using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using otelturizmnew.Services;

namespace otelturizmnew.Controllers.Api;

[ApiController]
[IgnoreAntiforgeryToken]
[EnableRateLimiting("growth-ingest")]
public sealed class GrowthAnalyticsController : ControllerBase
{
    private readonly ILogger<GrowthAnalyticsController> _logger;
    private readonly CommerceMetricsAccumulator _metrics;

    public GrowthAnalyticsController(ILogger<GrowthAnalyticsController> logger, CommerceMetricsAccumulator metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    [HttpPost("/growth/events")]
    public IActionResult Ingest([FromBody] GrowthEventPayload? payload)
    {
        if (payload is null || string.IsNullOrWhiteSpace(payload.Kind))
        {
            return BadRequest(new { ok = false, error = "payload_required" });
        }

        var safe = GrowthEventPayload.Normalize(payload, HttpContext);
        _logger.LogInformation(
            "GROWTH_EVENT kind={Kind} step={Step} route={Route} meta={Meta} vw={Vw} vh={Vh} conn={Conn} ua={Ua}",
            safe.Kind,
            safe.Step,
            safe.Route,
            safe.Meta,
            safe.Vw,
            safe.Vh,
            safe.Connection,
            safe.Ua);

        _metrics.RecordGrowthKind(safe.Kind ?? string.Empty);

        return Ok(new { ok = true });
    }
}

public sealed class GrowthEventPayload
{
    public string? Kind { get; set; }
    public string? Step { get; set; }
    public string? Route { get; set; }
    public string? Page { get; set; }
    public string? Meta { get; set; }
    public int? Vw { get; set; }
    public int? Vh { get; set; }
    public string? Connection { get; set; }
    public string? NavType { get; set; }
    public string Ua { get; set; } = string.Empty;

    public static GrowthEventPayload Normalize(GrowthEventPayload p, HttpContext httpContext)
    {
        static string Safe(string? s, int max) => string.IsNullOrWhiteSpace(s) ? string.Empty : (s.Length <= max ? s : s[..max]);
        var ua = Safe(httpContext.Request.Headers.UserAgent.ToString(), 220);

        return new GrowthEventPayload
        {
            Kind = Safe(p.Kind, 48),
            Step = Safe(p.Step, 64),
            Route = Safe(p.Route, 240),
            Page = Safe(p.Page, 512),
            Meta = Safe(p.Meta, 320),
            Connection = Safe(p.Connection, 32),
            NavType = Safe(p.NavType, 32),
            Vw = p.Vw is { } w ? Math.Clamp(w, 0, 20000) : null,
            Vh = p.Vh is { } h ? Math.Clamp(h, 0, 20000) : null,
            Ua = ua
        };
    }
}
