using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace otelturizmnew.Controllers.Security;

[ApiController]
[IgnoreAntiforgeryToken]
[EnableRateLimiting("csp-ingest")]
public sealed class CspReportController : ControllerBase
{
    private readonly ILogger<CspReportController> _logger;

    public CspReportController(ILogger<CspReportController> logger)
    {
        _logger = logger;
    }

    [HttpPost("/csp/report")]
    public async Task<IActionResult> Report(CancellationToken cancellationToken)
    {
        // p71: CSP report endpoint (report-uri) - log only.
        // Not: modern "report-to" payloadları farklı formatta gelebilir; ham gövdeyi logluyoruz.
        string body;
        try
        {
            using var reader = new StreamReader(Request.Body);
            body = await reader.ReadToEndAsync(cancellationToken);
        }
        catch
        {
            return Ok(new { ok = true });
        }

        var correlationId = HttpContext.Items.TryGetValue("CorrelationId", out var cidObj) ? cidObj as string : null;
        var ua = Request.Headers.UserAgent.ToString();
        var referer = Request.Headers.Referer.ToString();

        // JSON ise minify ederek log boyutunu sabit tutmaya çalış.
        var normalizedBody = NormalizeJson(body, 12_000);
        _logger.LogWarning("CSP_REPORT cid={CorrelationId} ua={Ua} ref={Ref} body={Body}",
            correlationId ?? string.Empty,
            Trunc(ua, 256),
            Trunc(referer, 512),
            normalizedBody);

        return Ok(new { ok = true });
    }

    private static string NormalizeJson(string? input, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim();
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var compact = JsonSerializer.Serialize(doc.RootElement);
            return Trunc(compact, maxLen);
        }
        catch
        {
            return Trunc(trimmed, maxLen);
        }
    }

    private static string Trunc(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLen ? value : value[..maxLen];
    }
}

