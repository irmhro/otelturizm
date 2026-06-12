using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class DevelopmentAccessService : IDevelopmentAccessService
{
    public const string AccessCookieName = "Otelturizm.Gelisim.Access";
    private const string DefaultAccessCode = "908155";
    private static readonly TimeSpan AccessDuration = TimeSpan.FromHours(12);
    private readonly IDataProtector _protector;
    private readonly string _accessCode;

    public DevelopmentAccessService(IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
    {
        _protector = dataProtectionProvider.CreateProtector("Otelturizm.DevelopmentAccess.v1");
        _accessCode = configuration["DevelopmentGate:AccessCode"]?.Trim();
        if (string.IsNullOrWhiteSpace(_accessCode))
        {
            _accessCode = DefaultAccessCode;
        }
    }

    public bool TryUnlock(HttpContext context, string? code, out DateTimeOffset expiresAt)
    {
        expiresAt = default;
        if (!string.Equals(code?.Trim(), _accessCode, StringComparison.Ordinal))
        {
            return false;
        }

        expiresAt = DateTimeOffset.UtcNow.Add(AccessDuration);
        var payload = $"{expiresAt.ToUnixTimeSeconds()}|development-access";
        var protectedPayload = _protector.Protect(payload);

        context.Response.Cookies.Append(
            AccessCookieName,
            protectedPayload,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Path = "/",
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            });

        return true;
    }

    public bool TryGetAccessExpiration(HttpContext context, out DateTimeOffset expiresAt)
    {
        expiresAt = default;
        if (!context.Request.Cookies.TryGetValue(AccessCookieName, out var cookieValue) || string.IsNullOrWhiteSpace(cookieValue))
        {
            return false;
        }

        try
        {
            var payload = _protector.Unprotect(cookieValue);
            var parts = payload.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !string.Equals(parts[1], "development-access", StringComparison.Ordinal))
            {
                return false;
            }

            if (!long.TryParse(parts[0], out var unixTime))
            {
                return false;
            }

            expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixTime);
            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void RevokeAccess(HttpContext context)
    {
        context.Response.Cookies.Delete(AccessCookieName, new CookieOptions { Path = "/" });
        context.Response.Cookies.Delete(AccessCookieName, new CookieOptions { Path = "/gelisim" });
    }
}
