using Microsoft.Extensions.Caching.Memory;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class PublicGrowthSignalsService : IPublicGrowthSignalsService
{
    private readonly IMemoryCache _cache;

    public PublicGrowthSignalsService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public int GetActiveViewerBand(long hotelId)
    {
        var key = $"growth:social-proof:{hotelId}";
        return _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(55);
            return Random.Shared.Next(5, 33);
        });
    }
}
