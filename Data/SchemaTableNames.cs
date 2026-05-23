using System.Globalization;
using Microsoft.Data.SqlClient;

namespace otelturizmnew.Data;

/// <summary>
/// Legacy (snake_case) ve MSSQL BÜYÜK HARF tablo adları arasında çözümleme.
/// Canlıda yalnızca yeni adlar varken eski TableExistsAsync kontrollerinin kırılmaması için.
/// </summary>
public static class SchemaTableNames
{
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["users"] = ["KULLANICILAR", "users"],
        ["users_partner"] = ["KULLANICI_PARTNERLERI", "users_partner"],
        ["user_favori_oteller"] = ["KULLANICI_FAVORI_OTELLER", "user_favori_oteller"],
        ["otel_kullanici_sahiplikleri"] = ["OTEL_KULLANICI_SAHIPLIKLERI", "otel_kullanici_sahiplikleri"],
        ["iller"] = ["ILLER", "iller"],
        ["ilceler"] = ["ILCELER", "ilceler"],
        ["mahalleler"] = ["MAHALLELER", "mahalleler"],
        ["rezervasyonlar"] = ["REZERVASYONLAR", "rezervasyonlar"],
    };

    public static IReadOnlyList<string> GetCandidates(string tableName)
    {
        if (Aliases.TryGetValue(tableName, out var list))
        {
            return list;
        }

        if (tableName.Equals(tableName.ToUpperInvariant(), StringComparison.Ordinal))
        {
            return new[] { tableName };
        }

        return new[] { tableName.ToUpperInvariant(), tableName };
    }

    public static async Task<string?> ResolveExistingTableAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken cancellationToken = default)
        => await ResolveExistingTableAsync(connection, tableName, null, cancellationToken);

    public static async Task<string?> ResolveExistingTableAsync(
        SqlConnection connection,
        string tableName,
        SqlTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        foreach (var candidate in GetCandidates(tableName))
        {
            if (await TableExistsExactAsync(connection, candidate, transaction, cancellationToken))
            {
                return candidate;
            }
        }

        return null;
    }

    public static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken cancellationToken = default)
        => await ResolveExistingTableAsync(connection, tableName, null, cancellationToken) is not null;

    public static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string tableName,
        SqlTransaction? transaction,
        CancellationToken cancellationToken = default)
        => await ResolveExistingTableAsync(connection, tableName, transaction, cancellationToken) is not null;

    public static async Task<HashSet<string>> GetColumnsAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var physical = await ResolveExistingTableAsync(connection, tableName, cancellationToken);
        if (physical is null)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        const string sql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = @tableName;
            """;

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", physical);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private static async Task<bool> TableExistsExactAsync(
        SqlConnection connection,
        string tableName,
        SqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = @tableName;
            """;

        await using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }
}
