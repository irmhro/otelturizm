using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class TimeZoneService : ITimeZoneService
{
    private readonly IConfiguration _configuration;
    private TimeZoneInfo? _cached;

    public TimeZoneService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TimeZoneInfo GetDefaultTimeZone()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        // Global-ready: Prod’da config’ten yönetilir.
        // Örn:
        // - Windows: "Turkey Standard Time", "GMT Standard Time", "Pacific Standard Time"
        // - Linux:   "Europe/Istanbul", "Europe/London", "America/Los_Angeles"
        var configured = (_configuration["App:DefaultTimeZone"] ?? string.Empty).Trim();
        _cached = ResolveTimeZone(configured);
        return _cached;
    }

    public DateTime ToLocal(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utc, GetDefaultTimeZone());
    }

    public DateTime ToUtc(DateTime localDateTime)
    {
        var unspecified = localDateTime.Kind == DateTimeKind.Unspecified
            ? localDateTime
            : DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, GetDefaultTimeZone());
    }

    private static TimeZoneInfo ResolveTimeZone(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            // Önce direkt deneyelim
            try { return TimeZoneInfo.FindSystemTimeZoneById(configured); } catch { }

            // Windows<->IANA en yaygın köprüler (küçük set; gerektiğinde genişletilir)
            var mapped = configured switch
            {
                "Europe/Istanbul" => "Turkey Standard Time",
                "Europe/London" => "GMT Standard Time",
                "America/New_York" => "Eastern Standard Time",
                "America/Los_Angeles" => "Pacific Standard Time",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(mapped))
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(mapped); } catch { }
            }
        }

        return TimeZoneInfo.Utc;
    }
}

