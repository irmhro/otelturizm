using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class IdempotencyService : IIdempotencyService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public IdempotencyService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return await factory(cancellationToken);
        }

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
            _cache.Set(key, value!, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });
            return value!;
        }
        finally
        {
            gate.Release();
            _locks.TryRemove(key, out _);
        }
    }
}

