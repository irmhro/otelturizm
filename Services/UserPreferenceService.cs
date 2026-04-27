using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.SqlClient;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class UserPreferenceService : IUserPreferenceService
{
    public const string CurrencyCookieName = "ot_currency";

    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "TRY", "USD", "EUR", "GBP"
    };

    private readonly IConfiguration _configuration;

    public UserPreferenceService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetCurrency(HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.TryGetValue(CurrencyCookieName, out var v))
        {
            var normalized = NormalizeCurrency(v);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "TRY";
    }

    public string GetLocale(HttpContext httpContext)
    {
        // Öncelik: request localization’ın seçtiği UI culture
        var ui = CultureInfo.CurrentUICulture?.Name;
        if (!string.IsNullOrWhiteSpace(ui))
        {
            return ui;
        }

        // fallback cookie
        if (httpContext.Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out var cookieValue))
        {
            try
            {
                var parsed = CookieRequestCultureProvider.ParseCookieValue(cookieValue);
                var culture = parsed?.UICultures.FirstOrDefault().Value;
                if (!string.IsNullOrWhiteSpace(culture))
                {
                    return culture;
                }
            }
            catch { }
        }

        return "tr-TR";
    }

    public async Task TryPersistCurrencyAsync(long userId, string currencyCode, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) return;
        var normalized = NormalizeCurrency(currencyCode);
        if (string.IsNullOrWhiteSpace(normalized)) return;

        var cs = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs)) return;

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync(cancellationToken);

        if (!await ColumnExistsAsync(conn, "dbo.users", "tercih_para_birimi", cancellationToken))
        {
            return;
        }

        await using var cmd = new SqlCommand("""
            UPDATE users
            SET tercih_para_birimi = @cur
            WHERE id = @id;
            """, conn);
        cmd.Parameters.AddWithValue("@cur", normalized);
        cmd.Parameters.AddWithValue("@id", userId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task TryPersistLocaleAsync(long userId, string locale, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) return;
        var normalized = NormalizeLocale(locale);
        if (string.IsNullOrWhiteSpace(normalized)) return;

        var cs = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs)) return;

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync(cancellationToken);

        if (!await ColumnExistsAsync(conn, "dbo.users", "tercih_locale", cancellationToken))
        {
            return;
        }

        await using var cmd = new SqlCommand("""
            UPDATE users
            SET tercih_locale = @loc
            WHERE id = @id;
            """, conn);
        cmd.Parameters.AddWithValue("@loc", normalized);
        cmd.Parameters.AddWithValue("@id", userId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public static string NormalizeCurrency(string? value)
    {
        var v = (value ?? string.Empty).Trim().ToUpperInvariant();
        return SupportedCurrencies.Contains(v) ? v : string.Empty;
    }

    public static string NormalizeLocale(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length == 0) return string.Empty;
        v = v.ToLowerInvariant() switch
        {
            "tr" => "tr-TR",
            "en" => "en-US",
            "fr" => "fr-FR",
            "de" => "de-DE",
            "es" => "es-ES",
            _ => v
        };

        return v switch
        {
            "tr-tr" => "tr-TR",
            "en-us" => "en-US",
            "en-gb" => "en-GB",
            "de-de" => "de-DE",
            "fr-fr" => "fr-FR",
            "es-es" => "es-ES",
            _ => "tr-TR"
        };
    }

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COL_LENGTH(@tableName, @columnName);", connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && scalar != DBNull.Value;
    }
}

