using System.Text.Json;
using Microsoft.AspNetCore.Http;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class DawnSurpriseService : IDawnSurpriseService
{
    public const string CookieName = "Otelturizm.DawnSurprise";
    private static readonly TimeSpan RewardLifetime = TimeSpan.FromMinutes(15);

    public DawnSurpriseState? GetActive(HttpContext httpContext)
    {
        var payload = ReadPayload(httpContext);
        if (payload is null)
        {
            return null;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.ExpiresUnix);
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            ClearCookie(httpContext);
            return null;
        }

        return new DawnSurpriseState
        {
            Percent = payload.Percent,
            ExpiresAt = expiresAt
        };
    }

    public DawnSurpriseOpenResult Open(HttpContext httpContext)
    {
        var existing = GetActive(httpContext);
        if (existing is not null)
        {
            return new DawnSurpriseOpenResult
            {
                Percent = existing.Percent,
                IsNew = false,
                ExpiresAt = existing.ExpiresAt
            };
        }

        var percent = Random.Shared.Next(3, 11);
        var expiresAt = DateTimeOffset.UtcNow.Add(RewardLifetime);
        WriteCookie(httpContext, new DawnSurpriseCookiePayload
        {
            Percent = percent,
            ExpiresUnix = expiresAt.ToUnixTimeSeconds()
        });

        return new DawnSurpriseOpenResult
        {
            Percent = percent,
            IsNew = true,
            ExpiresAt = expiresAt
        };
    }

    private static DawnSurpriseCookiePayload? ReadPayload(HttpContext httpContext)
    {
        if (!httpContext.Request.Cookies.TryGetValue(CookieName, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DawnSurpriseCookiePayload>(raw);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteCookie(HttpContext httpContext, DawnSurpriseCookiePayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        httpContext.Response.Cookies.Append(CookieName, json, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = RewardLifetime
        });
    }

    private static void ClearCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax
        });
    }

    private sealed class DawnSurpriseCookiePayload
    {
        public int Percent { get; set; }
        public long ExpiresUnix { get; set; }
    }
}
