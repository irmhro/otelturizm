namespace otelturizmnew.Services.Abstractions;

public interface ISlowSqlTracker
{
    void Record(string sql, long elapsedMs, string? scope = null);
    IReadOnlyList<SlowSqlStat> GetTop(int take = 20);
}

public sealed record SlowSqlStat(
    string Key,
    string Scope,
    long Count,
    long MaxMs,
    double AvgMs,
    DateTimeOffset LastSeenUtc,
    string SampleSql);

