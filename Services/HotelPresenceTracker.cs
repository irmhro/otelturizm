using System.Collections.Concurrent;

namespace otelturizmnew.Services;

/// <summary>Otel detay sayfası canlı oturum sayacı (paket 234).</summary>
public sealed class HotelPresenceTracker
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, DateTimeOffset>> _beats = new();

    public int Beat(long hotelId, string tabId)
    {
        if (hotelId <= 0 || string.IsNullOrWhiteSpace(tabId))
        {
            return GetActiveCount(hotelId);
        }

        var inner = _beats.GetOrAdd(hotelId, _ => new ConcurrentDictionary<string, DateTimeOffset>(StringComparer.Ordinal));
        inner[tabId.Trim()] = DateTimeOffset.UtcNow;
        Prune(inner);
        return inner.Count;
    }

    public int GetActiveCount(long hotelId)
    {
        if (!_beats.TryGetValue(hotelId, out var inner))
        {
            return 0;
        }

        Prune(inner);
        return inner.Count;
    }

    private void Prune(ConcurrentDictionary<string, DateTimeOffset> inner)
    {
        var cutoff = DateTimeOffset.UtcNow - Ttl;
        foreach (var kv in inner)
        {
            if (kv.Value < cutoff)
            {
                inner.TryRemove(kv.Key, out _);
            }
        }
    }
}
