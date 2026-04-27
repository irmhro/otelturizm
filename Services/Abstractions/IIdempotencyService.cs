namespace otelturizmnew.Services.Abstractions;

public interface IIdempotencyService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken = default);
}

