namespace otelturizmnew.Services.Abstractions;

public interface IDevelopmentAccessService
{
    bool TryUnlock(HttpContext context, string? code, out DateTimeOffset expiresAt);
    bool TryGetAccessExpiration(HttpContext context, out DateTimeOffset expiresAt);
    void RevokeAccess(HttpContext context);
}
