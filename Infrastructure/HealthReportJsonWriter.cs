using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace otelturizmnew.Infrastructure;

/// <summary>
/// /health/platform için JSON: üretimde açıklama/istisna sızıntısını azaltır.
/// </summary>
public static class HealthReportJsonWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var env = context.RequestServices.GetRequiredService<IHostEnvironment>();
        var cfg = context.RequestServices.GetRequiredService<IConfiguration>();
        var expose = cfg.GetValue("Security:HealthChecksExposeDetails", false);

        context.Response.ContentType = "application/json; charset=utf-8";

        if (env.IsDevelopment() || expose)
        {
            var checks = new Dictionary<string, object>();
            foreach (var kv in report.Entries)
            {
                checks[kv.Key] = new
                {
                    status = kv.Value.Status.ToString(),
                    description = kv.Value.Description,
                    durationMs = Math.Round(kv.Value.Duration.TotalMilliseconds, 2)
                };
            }

            var payload = new
            {
                status = report.Status.ToString(),
                totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
                checks
            };
            return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }

        var minimal = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(static e => e.Key, static e => e.Value.Status.ToString())
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(minimal, JsonOptions));
    }
}
