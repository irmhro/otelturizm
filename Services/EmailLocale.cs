using System.Globalization;
using System.Linq;

namespace otelturizmnew.Services;

/// <summary>E-posta şablonları ve kuyruk için iki harfli dil kodları.</summary>
public static class EmailLocale
{
    public static readonly string[] SupportedTwoLetter = ["tr", "en", "fr", "de", "es", "ru", "ar"];

    private static readonly HashSet<string> Supported = new(SupportedTwoLetter, StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return "tr";
        }

        var first = trimmed.Split('-', '_', ' ').FirstOrDefault() ?? trimmed;
        return Supported.Contains(first) ? first.ToLowerInvariant() : "tr";
    }

    public static CultureInfo GetFormatCulture(string lang)
    {
        return Normalize(lang).ToLowerInvariant() switch
        {
            "en" => CultureInfo.GetCultureInfo("en-US"),
            "fr" => CultureInfo.GetCultureInfo("fr-FR"),
            "de" => CultureInfo.GetCultureInfo("de-DE"),
            "es" => CultureInfo.GetCultureInfo("es-ES"),
            "ru" => CultureInfo.GetCultureInfo("ru-RU"),
            "ar" => CultureInfo.GetCultureInfo("ar-SA"),
            _ => CultureInfo.GetCultureInfo("tr-TR"),
        };
    }
}
