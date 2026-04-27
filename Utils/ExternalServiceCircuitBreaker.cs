using System.Collections.Concurrent;

namespace otelturizmnew.Utils;

public sealed class ExternalServiceCircuitBreaker
{
    private sealed class State
    {
        public int Failures;
        public DateTimeOffset? OpenUntilUtc;
        public DateTimeOffset LastFailureUtc;
    }

    private readonly ConcurrentDictionary<string, State> _states = new(StringComparer.OrdinalIgnoreCase);

    public bool IsOpen(string name)
    {
        var s = _states.GetOrAdd(name, static _ => new State());
        var openUntil = s.OpenUntilUtc;
        return openUntil.HasValue && openUntil.Value > DateTimeOffset.UtcNow;
    }

    public async Task<T?> ExecuteAsync<T>(
        string name,
        Func<CancellationToken, Task<T>> action,
        int failureThreshold = 5,
        TimeSpan? openDuration = null,
        CancellationToken cancellationToken = default)
    {
        var s = _states.GetOrAdd(name, static _ => new State());
        var now = DateTimeOffset.UtcNow;

        if (s.OpenUntilUtc is { } until && until > now)
        {
            return default;
        }

        try
        {
            var result = await action(cancellationToken);
            s.Failures = 0;
            s.OpenUntilUtc = null;
            return result;
        }
        catch
        {
            s.Failures++;
            s.LastFailureUtc = DateTimeOffset.UtcNow;
            if (s.Failures >= Math.Max(1, failureThreshold))
            {
                s.OpenUntilUtc = DateTimeOffset.UtcNow.Add(openDuration ?? TimeSpan.FromMinutes(2));
            }
            throw;
        }
    }
}

