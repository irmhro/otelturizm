namespace otelturizmnew.Services.Abstractions;

public interface IUserPreferenceService
{
    string GetCurrency(HttpContext httpContext);
    string GetLocale(HttpContext httpContext);
    Task TryPersistCurrencyAsync(long userId, string currencyCode, CancellationToken cancellationToken = default);
    Task TryPersistLocaleAsync(long userId, string locale, CancellationToken cancellationToken = default);
}

