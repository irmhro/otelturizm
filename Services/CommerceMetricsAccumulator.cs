using System.Collections.Concurrent;

namespace otelturizmnew.Services;

/// <summary>RUM ve growth istemci olayları için bellek içi halka tampon (admin vitrin).</summary>
public sealed class CommerceMetricsAccumulator
{
    private readonly ConcurrentDictionary<string, RumMetricAgg> _rumByRouteMetric = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _growthKindCounts = new(StringComparer.OrdinalIgnoreCase);

    public void RecordRum(string route, string metric, double value)
    {
        var key = $"{metric}|{route}";
        _rumByRouteMetric.AddOrUpdate(
            key,
            _ => new RumMetricAgg { Count = 1, Sum = value, Min = value, Max = value },
            (_, a) =>
            {
                a.Count++;
                a.Sum += value;
                a.Min = Math.Min(a.Min, value);
                a.Max = Math.Max(a.Max, value);
                return a;
            });
    }

    public void RecordGrowthKind(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return;
        }

        _growthKindCounts.AddOrUpdate(kind.Trim(), 1, (_, n) => n + 1);
    }

    public IReadOnlyList<(string RouteMetric, double Avg, int Count, double Min, double Max)> SnapshotRum(int maxRows)
    {
        return _rumByRouteMetric
            .OrderByDescending(kv => kv.Value.Count)
            .Take(maxRows)
            .Select(kv =>
            {
                var a = kv.Value;
                var avg = a.Count > 0 ? a.Sum / a.Count : 0;
                return (kv.Key, avg, a.Count, a.Min, a.Max);
            })
            .ToList();
    }

    public IReadOnlyDictionary<string, int> SnapshotGrowthKinds()
    {
        return new Dictionary<string, int>(_growthKindCounts, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class RumMetricAgg
    {
        public int Count;
        public double Sum;
        public double Min;
        public double Max;
    }
}
