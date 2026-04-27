using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class CacheSingleFlight : ICacheSingleFlight
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public CacheSingleFlight(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var existing) && existing is T typed)
        {
            return typed;
        }

        var gate = _locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(key, out existing) && existing is T typed2)
            {
                return typed2;
            }

            var value = await factory(cancellationToken);
            var options = new MemoryCacheEntryOptions();
            if (absoluteExpirationRelativeToNow.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            }
            if (slidingExpiration.HasValue)
            {
                options.SlidingExpiration = slidingExpiration;
            }

            _cache.Set(key, value!, options);
            return value!;
        }
        finally
        {
            gate.Release();
            _locks.TryRemove(key, out _);
        }
    }
}

