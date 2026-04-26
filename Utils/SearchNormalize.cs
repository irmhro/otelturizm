using System.Text;
using System.Globalization;

namespace otelturizmnew.Utils;

public static class SearchNormalize
{
    public static string Keyword(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalizedRoute = RouteSegment(value);
        return normalizedRoute.Replace('-', ' ').Trim();
    }

    public static string RouteSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var prepared = value.Trim()
            .Replace("ı", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("İ", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("ğ", "g", StringComparison.OrdinalIgnoreCase)
            .Replace("Ğ", "g", StringComparison.OrdinalIgnoreCase)
            .Replace("ü", "u", StringComparison.OrdinalIgnoreCase)
            .Replace("Ü", "u", StringComparison.OrdinalIgnoreCase)
            .Replace("ş", "s", StringComparison.OrdinalIgnoreCase)
            .Replace("Ş", "s", StringComparison.OrdinalIgnoreCase)
            .Replace("ö", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("Ö", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ç", "c", StringComparison.OrdinalIgnoreCase)
            .Replace("Ç", "c", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder();
        foreach (var c in prepared)
        {
            var unicode = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicode == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (char.IsWhiteSpace(c) || c is '-' or '_' or '.' or '/')
            {
                sb.Append('-');
            }
        }

        var collapsed = sb.ToString()
            .Replace("--", "-", StringComparison.Ordinal);
        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }

        return collapsed.Trim('-').Normalize(NormalizationForm.FormC);
    }
}

