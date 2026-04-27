using System.Globalization;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class CurrencyFormatter : ICurrencyFormatter
{
    public string Format(decimal amount, string currencyCode)
    {
        var code = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            code = "TRY";
        }

        var culture = CultureInfo.CurrentCulture;
        var symbol = code switch
        {
            "TRY" => "₺",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => code
        };

        // Not: Dünya geneli için gerçek FX/locale mapping daha sonra genişletilecek.
        return string.Format(culture, "{0}{1:N0}", symbol, amount);
    }
}

