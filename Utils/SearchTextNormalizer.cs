using System.Globalization;
using System.Text;

namespace otelturizmnew.Utils;

public static class SearchTextNormalizer
{
    public static string Normalize(string? input)
    {
        var v = (input ?? string.Empty).Trim();
        if (v.Length == 0) return string.Empty;

        // Unicode normalize (compatibility) + diacritic temizliği
        v = v.Normalize(NormalizationForm.FormKC);

        var sb = new StringBuilder(v.Length);
        foreach (var ch in v.Normalize(NormalizationForm.FormD))
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch);
        }

        var collapsed = sb
            .ToString()
            .Normalize(NormalizationForm.FormKC)
            .ToLowerInvariant();

        // whitespace collapse
        collapsed = string.Join(' ', collapsed.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return collapsed;
    }
}

