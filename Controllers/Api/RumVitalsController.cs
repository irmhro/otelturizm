using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using otelturizmnew.Services;

namespace otelturizmnew.Controllers.Api;

[ApiController]
[EnableRateLimiting("growth-ingest")]
public sealed class RumVitalsController : ControllerBase
{
    private readonly ILogger<RumVitalsController> _logger;
    private readonly CommerceMetricsAccumulator _metrics;

    public RumVitalsController(ILogger<RumVitalsController> logger, CommerceMetricsAccumulator metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    [HttpPost("/rum/vitals")]
    public IActionResult Ingest([FromBody] RumVitalsPayload payload)
    {
        // p60: Gerçek kullanıcı ölçümleri (RUM) — ilk aşamada structured log.
        // Sonraki aşama: örnekleme + DB/metric backend (Grafana/Prometheus/OTel).
        if (payload is null)
        {
            return BadRequest(new { ok = false, error = "payload_required" });
        }

        var safe = payload.Normalize(HttpContext);
        _logger.LogInformation("RUM_VITALS {Metric}={Value} {Unit} route={Route} page={Page} nav={NavType} ua={Ua}",
            safe.Metric, safe.Value, safe.Unit, safe.Route, safe.Page, safe.NavType, safe.Ua);

        if (!string.IsNullOrWhiteSpace(safe.Metric) && safe.Value is { } v && double.IsFinite(v))
        {
            var routeKey = string.IsNullOrWhiteSpace(safe.Route) ? "/" : safe.Route;
            _metrics.RecordRum(routeKey, safe.Metric.Trim(), v);
        }

        return Ok(new { ok = true });
    }
}

public sealed record RumVitalsPayload(
    string? Metric,
    double? Value,
    string? Unit,
    string? Route,
    string? Page,
    string? NavType,
    string? Referrer,
    int? SampleRate,
    string? Dpr,
    int? Vw,
    int? Vh,
    string? AppVersion)
{
    public RumVitalsPayload Normalize(HttpContext httpContext)
    {
        string SafeStr(string? s, int max) => string.IsNullOrWhiteSpace(s) ? string.Empty : (s.Length <= max ? s : s[..max]);
        var ua = SafeStr(httpContext.Request.Headers.UserAgent.ToString(), 256);

        return this with
        {
            Metric = SafeStr(Metric, 32),
            Unit = SafeStr(Unit, 16),
            Route = SafeStr(Route, 256),
            Page = SafeStr(Page, 512),
            NavType = SafeStr(NavType, 32),
            Referrer = SafeStr(Referrer, 512),
            AppVersion = SafeStr(AppVersion, 64),
            SampleRate = SampleRate is { } sr ? Math.Clamp(sr, 1, 100) : null,
            Vw = Vw is { } vw ? Math.Clamp(vw, 0, 20000) : null,
            Vh = Vh is { } vh ? Math.Clamp(vh, 0, 20000) : null,
            Ua = ua
        };
    }

    public string Ua { get; init; } = string.Empty;
}

