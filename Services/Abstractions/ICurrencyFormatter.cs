namespace otelturizmnew.Services.Abstractions;

public interface ICurrencyFormatter
{
    string Format(decimal amount, string currencyCode);
}

