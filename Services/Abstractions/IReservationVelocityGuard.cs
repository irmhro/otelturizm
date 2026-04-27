using Microsoft.AspNetCore.Http;

namespace otelturizmnew.Services.Abstractions;

public interface IReservationVelocityGuard
{
    bool TryAllowReservationAttempt(HttpContext httpContext, out string? blockReason);

    /// <summary>0–100 basit risk skoru (paket 227).</summary>
    int ComputeRiskScore01(HttpContext httpContext);
}
