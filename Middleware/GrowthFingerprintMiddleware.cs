namespace otelturizmnew.Middleware;

/// <summary>
/// İstemci için kalıcı ama kişisel veri içermeyen parmak izi çerezi (rate limit partition + growth örnekleme).
/// </summary>
public sealed class GrowthFingerprintMiddleware
{
    private const string CookieName = "Otelturizm.ClientFp";
    private readonly RequestDelegate _next;

    public GrowthFingerprintMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Cookies.ContainsKey(CookieName))
        {
            var token = Guid.NewGuid().ToString("N");
            context.Response.Cookies.Append(CookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !context.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase),
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(365),
                Path = "/"
            });
        }

        return _next(context);
    }
}
