using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class SlowSqlTracker : ISlowSqlTracker
{
    private sealed class Entry
    {
        public long Count;
        public long TotalMs;
        public long MaxMs;
        public DateTimeOffset LastSeenUtc;
        public string SampleSql = string.Empty;
        public string Scope = string.Empty;
    }

    private readonly ConcurrentDictionary<string, Entry> _stats = new(StringComparer.Ordinal);
    private readonly int _sampleSqlMaxLen;
    private readonly int _maxKeys;

    public SlowSqlTracker()
    {
        _sampleSqlMaxLen = 1200;
        _maxKeys = 600;
    }

    public void Record(string sql, long elapsedMs, string? scope = null)
    {
        if (string.IsNullOrWhiteSpace(sql) || elapsedMs <= 0)
        {
            return;
        }

        var normalized = NormalizeSql(sql);
        var key = HashKey(normalized);
        var now = DateTimeOffset.UtcNow;
        var safeScope = string.IsNullOrWhiteSpace(scope) ? "sql" : scope.Trim();

        // Basit bellek koruması: çok fazla key birikirse eskiyi buda.
        if (_stats.Count > _maxKeys)
        {
            foreach (var victim in _stats
                         .OrderBy(kv => kv.Value.LastSeenUtc)
                         .Take(Math.Max(20, _stats.Count - _maxKeys)))
            {
                _stats.TryRemove(victim.Key, out _);
            }
        }

        var entry = _stats.GetOrAdd(key, _ => new Entry
        {
            SampleSql = Trunc(normalized, _sampleSqlMaxLen),
            Scope = safeScope,
            LastSeenUtc = now
        });

        lock (entry)
        {
            entry.Count++;
            entry.TotalMs += elapsedMs;
            if (elapsedMs > entry.MaxMs) entry.MaxMs = elapsedMs;
            entry.LastSeenUtc = now;
            entry.Scope = safeScope;
            if (string.IsNullOrWhiteSpace(entry.SampleSql))
            {
                entry.SampleSql = Trunc(normalized, _sampleSqlMaxLen);
            }
        }
    }

    public IReadOnlyList<SlowSqlStat> GetTop(int take = 20)
    {
        var safeTake = Math.Clamp(take, 1, 200);
        return _stats
            .Select(kv =>
            {
                var e = kv.Value;
                long count;
                long max;
                long total;
                DateTimeOffset last;
                string sample;
                string scope;
                lock (e)
                {
                    count = e.Count;
                    max = e.MaxMs;
                    total = e.TotalMs;
                    last = e.LastSeenUtc;
                    sample = e.SampleSql;
                    scope = e.Scope;
                }

                var avg = count > 0 ? (double)total / count : 0d;
                return new SlowSqlStat(kv.Key, scope, count, max, avg, last, sample);
            })
            .OrderByDescending(x => x.MaxMs)
            .ThenByDescending(x => x.Count)
            .Take(safeTake)
            .ToList();
    }

    private static string NormalizeSql(string sql)
    {
        var s = sql.Trim();
        var sb = new StringBuilder(s.Length);
        var lastWasWs = false;
        foreach (var ch in s)
        {
            var isWs = char.IsWhiteSpace(ch);
            if (isWs)
            {
                if (lastWasWs) continue;
                sb.Append(' ');
                lastWasWs = true;
                continue;
            }

            sb.Append(ch);
            lastWasWs = false;
        }

        return sb.ToString();
    }

    private static string HashKey(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        // kısa key yeterli
        return Convert.ToHexString(hash[..8]);
    }

    private static string Trunc(string value, int maxLen)
        => value.Length <= maxLen ? value : value[..maxLen];
}

