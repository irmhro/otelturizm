using System.Diagnostics;
using Microsoft.Data.SqlClient;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Utils;

public static class SqlTiming
{
    public static async Task<SqlDataReader> ExecuteReaderAsync(
        SqlCommand command,
        ISlowSqlTracker tracker,
        ILogger logger,
        string scope,
        int slowMsThreshold,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await command.ExecuteReaderAsync(cancellationToken);
        }
        finally
        {
            sw.Stop();
            var ms = (long)sw.ElapsedMilliseconds;
            if (ms >= slowMsThreshold)
            {
                tracker.Record(command.CommandText, ms, scope);
                logger.LogWarning("SLOW_SQL {Scope} {Ms}ms sql={Sql}", scope, ms, Trunc(command.CommandText, 1200));
            }
        }
    }

    public static async Task<object?> ExecuteScalarAsync(
        SqlCommand command,
        ISlowSqlTracker tracker,
        ILogger logger,
        string scope,
        int slowMsThreshold,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await command.ExecuteScalarAsync(cancellationToken);
        }
        finally
        {
            sw.Stop();
            var ms = (long)sw.ElapsedMilliseconds;
            if (ms >= slowMsThreshold)
            {
                tracker.Record(command.CommandText, ms, scope);
                logger.LogWarning("SLOW_SQL {Scope} {Ms}ms sql={Sql}", scope, ms, Trunc(command.CommandText, 1200));
            }
        }
    }

    public static async Task<int> ExecuteNonQueryAsync(
        SqlCommand command,
        ISlowSqlTracker tracker,
        ILogger logger,
        string scope,
        int slowMsThreshold,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            sw.Stop();
            var ms = (long)sw.ElapsedMilliseconds;
            if (ms >= slowMsThreshold)
            {
                tracker.Record(command.CommandText, ms, scope);
                logger.LogWarning("SLOW_SQL {Scope} {Ms}ms sql={Sql}", scope, ms, Trunc(command.CommandText, 1200));
            }
        }
    }

    private static string Trunc(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLen ? value : value[..maxLen];
    }
}

