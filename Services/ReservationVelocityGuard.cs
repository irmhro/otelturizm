using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

/// <summary>Çoklu rezervasyon denemesi / ödeme benzeri istekler için hız sınırı (paket 226–227).</summary>
public sealed class ReservationVelocityGuard : IReservationVelocityGuard
{
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _windows = new(StringComparer.OrdinalIgnoreCase);

    private static string Key(HttpContext ctx, string bucket)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"{bucket}:{ip}";
    }

    public bool TryAllowReservationAttempt(HttpContext httpContext, out string? blockReason)
    {
        blockReason = null;
        var k = Key(httpContext, "res");
        var now = DateTimeOffset.UtcNow;
        var window = TimeSpan.FromMinutes(15);
        var maxPerWindow = 24;

        var q = _windows.GetOrAdd(k, _ => new Queue<DateTimeOffset>());
        lock (q)
        {
            while (q.Count > 0 && now - q.Peek() > window)
            {
                q.Dequeue();
            }

            if (q.Count >= maxPerWindow)
            {
                blockReason = "Bu IP üzerinden çok sık rezervasyon denemesi yapıldı. Lütfen bir süre sonra tekrar deneyin.";
                return false;
            }

            q.Enqueue(now);
        }

        return true;
    }

    public int ComputeRiskScore01(HttpContext httpContext)
    {
        var k = Key(httpContext, "res");
        if (!_windows.TryGetValue(k, out var q))
        {
            return 10;
        }

        lock (q)
        {
            var recent = q.Count(x => DateTimeOffset.UtcNow - x < TimeSpan.FromMinutes(15));
            return Math.Clamp(recent * 12, 0, 100);
        }
    }
}
