using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using otelturizmnew.Services;

namespace otelturizmnew.Controllers.Api;

/// <summary>Otel detay canlı görüntüleyici sayacı (heartbeat).</summary>
[ApiController]
[IgnoreAntiforgeryToken]
[EnableRateLimiting("presence-beat")]
public sealed class HotelPresenceController : ControllerBase
{
    private readonly HotelPresenceTracker _tracker;

    public HotelPresenceController(HotelPresenceTracker tracker)
    {
        _tracker = tracker;
    }

    [HttpPost("/api/hotel-presence/beat")]
    public IActionResult Beat([FromBody] HotelPresenceBeatRequest? body)
    {
        if (body is null || body.HotelId <= 0 || string.IsNullOrWhiteSpace(body.TabId))
        {
            return BadRequest(new { ok = false, error = "invalid_payload" });
        }

        var active = _tracker.Beat(body.HotelId, body.TabId);
        return Ok(new { ok = true, active });
    }
}

public sealed class HotelPresenceBeatRequest
{
    public long HotelId { get; set; }
    public string? TabId { get; set; }
}
