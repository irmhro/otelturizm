namespace otelturizmnew.Utils;

/// <summary>PII-safe logging helpers (T107).</summary>
public static class LogRedaction
{
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 0)
        {
            return "***";
        }

        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];
        var maskedLocal = local.Length <= 2
            ? new string('*', local.Length)
            : $"{local[0]}***{local[^1]}";
        return $"{maskedLocal}@{domain}";
    }

    public static string MaskIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return string.Empty;
        }

        var trimmed = ip.Trim();
        if (trimmed.Contains(':', StringComparison.Ordinal))
        {
            return "ipv6:***";
        }

        var parts = trimmed.Split('.');
        if (parts.Length == 4)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}.*";
        }

        return "***";
    }

    public static string TruncateUserAgent(string? userAgent, int maxLen = 48)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return string.Empty;
        }

        var trimmed = userAgent.Trim();
        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen] + "…";
    }

    public static string RedactStoredPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFileName(path.Trim());
    }
}
