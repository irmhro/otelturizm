namespace otelturizmnew.Services.Abstractions;

public interface ITimeZoneService
{
    /// <summary>Uygulama varsayılan timezone bilgisi.</summary>
    TimeZoneInfo GetDefaultTimeZone();

    /// <summary>UTC zamanı varsayılan timezone'a çevirir.</summary>
    DateTime ToLocal(DateTime utcDateTime);

    /// <summary>Yerel zamanı UTC'ye çevirir (varsayılan timezone).</summary>
    DateTime ToUtc(DateTime localDateTime);
}

