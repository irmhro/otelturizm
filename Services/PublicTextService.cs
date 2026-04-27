using System.Globalization;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class PublicTextService : IPublicTextService
{
    private static readonly Dictionary<string, (string Tr, string En)> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["safe_payment"] = ("Güvenli Ödeme", "Secure Payment"),
        ["support_24_7"] = ("7/24 Destek", "24/7 Support"),
        ["best_price"] = ("En İyi Fiyat", "Best Price"),
        ["search_placeholder"] = ("Otel adı, il, ilçe, mahalle veya bölge ara", "Search hotel, city, district or area"),
        ["all_rights_reserved"] = ("Tüm hakları saklıdır.", "All rights reserved.")
    };

    public string Get(string key)
    {
        var lang = CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName?.ToLowerInvariant() ?? "tr";
        if (!Map.TryGetValue(key ?? string.Empty, out var v))
        {
            return key ?? string.Empty;
        }

        return lang == "en" ? v.En : v.Tr;
    }
}

