namespace otelturizmnew.Services.Abstractions;

public interface ISessionSecurityService
{
    Task TrackAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
