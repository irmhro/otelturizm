using System.Text.RegularExpressions;

namespace otelturizmnew.Services;

public static class ReviewTextFilter
{
    public static string MaskBlockedWords(string? input, IReadOnlyList<string> blockedWords)
    {
        if (string.IsNullOrWhiteSpace(input) || blockedWords.Count == 0)
        {
            return input ?? string.Empty;
        }

        var text = input;
        foreach (var raw in blockedWords)
        {
            var word = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(word)) continue;

            // Letter boundary (Turkish-safe) so "aq" doesn't hit "aqua" etc.
            var pattern = $@"(?<!\p{{L}}){Regex.Escape(word)}(?!\p{{L}})";
            text = Regex.Replace(text, pattern, m => new string('*', Math.Min(8, Math.Max(3, m.Value.Length))), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return text;
    }
}

