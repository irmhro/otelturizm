using Microsoft.Extensions.Configuration;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class DeadLinkRedirectService : IDeadLinkRedirectService
{
    private readonly IConfiguration _configuration;

    public DeadLinkRedirectService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? TryResolvePermanentRedirect(string originalPath)
    {
        if (string.IsNullOrWhiteSpace(originalPath))
        {
            return null;
        }

        var path = originalPath.Trim();
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        var normalized = path.TrimEnd('/');
        if (normalized.Length == 0)
        {
            normalized = "/";
        }

        var section = _configuration.GetSection("DeadLinks:Map");
        foreach (var kv in section.GetChildren())
        {
            var from = kv.Key.Trim();
            if (string.IsNullOrEmpty(from))
            {
                continue;
            }

            if (!from.StartsWith('/'))
            {
                from = "/" + from;
            }

            var fromNorm = from.TrimEnd('/');
            if (string.Equals(normalized, fromNorm, StringComparison.OrdinalIgnoreCase))
            {
                var to = (kv.Value ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(to))
                {
                    continue;
                }

                return to.StartsWith('/') ? to : "/" + to;
            }
        }

        // Legacy Turkish hotel paths → canonical /hotel
        if (normalized.StartsWith("/oteller", StringComparison.OrdinalIgnoreCase))
        {
            return "/hotel" + normalized.Substring("/oteller".Length);
        }

        if (string.Equals(normalized, "/hotels", StringComparison.OrdinalIgnoreCase))
        {
            return "/hotel";
        }

        return null;
    }
}
