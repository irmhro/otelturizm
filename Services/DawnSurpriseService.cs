using System.Globalization;

using System.Text.Json;

using Microsoft.AspNetCore.Http;

using otelturizmnew.Services.Abstractions;



namespace otelturizmnew.Services;



public sealed class DawnSurpriseService : IDawnSurpriseService

{

    public const string CookieName = "Otelturizm.DawnSurprise";

    private static readonly TimeSpan RewardLifetime = TimeSpan.FromHours(1);



    public bool IsEligible(HttpContext httpContext)

    {

        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        if (string.Equals(culture, "tr", StringComparison.OrdinalIgnoreCase))

        {

            return true;

        }



        var path = httpContext.Request.Path.Value ?? "/";

        return !InternationalSeoPaths.HasLocalePathPrefix(path);

    }



    public DawnSurpriseState? GetActive(HttpContext httpContext)

    {

        if (!IsEligible(httpContext))

        {

            return null;

        }



        var payload = ReadPayload(httpContext);

        if (payload is null)

        {

            return null;

        }



        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.ExpiresUnix);

        if (expiresAt <= DateTimeOffset.UtcNow)

        {

            Clear(httpContext);

            return null;

        }



        return new DawnSurpriseState

        {

            Percent = Math.Min(6, Math.Max(1, payload.Percent)),

            ExpiresAt = expiresAt

        };

    }



    public DawnSurpriseOpenResult? Open(HttpContext httpContext)

    {

        if (!IsEligible(httpContext))

        {

            return null;

        }



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



        var percent = Random.Shared.Next(1, 7);

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



    public void Clear(HttpContext httpContext)

    {

        ClearCookie(httpContext);

    }



    private static DawnSurpriseCookiePayload? ReadPayload(HttpContext httpContext)

    {

        if (!httpContext.Request.Cookies.TryGetValue(CookieName, out var raw) || string.IsNullOrWhiteSpace(raw))

        {

            return null;

        }



        var pipeParts = raw.Split('|', StringSplitOptions.TrimEntries);

        if (pipeParts.Length == 2

            && int.TryParse(pipeParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var percentFromPipe)

            && long.TryParse(pipeParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var expiresFromPipe))

        {

            return new DawnSurpriseCookiePayload

            {

                Percent = percentFromPipe,

                ExpiresUnix = expiresFromPipe

            };

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

        var value = $"{payload.Percent}|{payload.ExpiresUnix}";

        httpContext.Response.Cookies.Append(CookieName, value, new CookieOptions

        {

            HttpOnly = true,

            Secure = httpContext.Request.IsHttps,

            SameSite = SameSiteMode.Lax,

            Path = "/",

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

            SameSite = SameSiteMode.Lax,

            Path = "/"

        });

    }



    private sealed class DawnSurpriseCookiePayload

    {

        public int Percent { get; set; }

        public long ExpiresUnix { get; set; }

    }

}


