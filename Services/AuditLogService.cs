using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Data;
using Microsoft.Data.SqlClient;
using otelturizmnew.Constants;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AuditLogService : IAuditLogService
{
    private readonly string _connectionString;

    public AuditLogService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task TryLogApiRequestAsync(HttpContext context, int statusCode, long elapsedMs, CancellationToken cancellationToken = default)
    {
        // Statik dosya ve gorsel taleplerini loglamayalim.
        if (context.Request.Path.StartsWithSegments("/assets", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/uploads", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/favicon", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "api_loglari", cancellationToken))
        {
            return;
        }

        var requestId = context.TraceIdentifier;
        var endpoint = $"{context.Request.Path}{context.Request.QueryString}";
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var ua = context.Request.Headers.UserAgent.ToString();
        var userId = TryGetUserId(context.User);
        var partnerId = TryGetPartnerId(context.User);
        var ok = statusCode is >= 200 and < 400;

        // En minimal ortak payda: endpoint + method + status + sure + request_id
        // Kolonlar farklı şemalarda eksik olabilir; insert'i kolon varlığına göre kuruyoruz.
        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new List<SqlParameter>();

        AddIfColumnExists(connection, "api_loglari", "request_id", requestId, SqlDbType.VarChar, 64);
        AddIfColumnExists(connection, "api_loglari", "endpoint", endpoint, SqlDbType.NVarChar, 500);
        AddIfColumnExists(connection, "api_loglari", "http_method", method, SqlDbType.VarChar, 16);
        AddIfColumnExists(connection, "api_loglari", "request_ip", ip, SqlDbType.VarChar, 64);
        AddIfColumnExists(connection, "api_loglari", "user_agent", ua, SqlDbType.NVarChar, -1);
        AddIfColumnExists(connection, "api_loglari", "response_status", statusCode, SqlDbType.SmallInt);
        AddIfColumnExists(connection, "api_loglari", "islem_suresi_ms", (int)Math.Min(int.MaxValue, elapsedMs), SqlDbType.Int);
        AddIfColumnExists(connection, "api_loglari", "basarili_mi", ok ? 1 : 0, SqlDbType.Bit);
        AddIfColumnExists(connection, "api_loglari", "kullanici_id", userId, SqlDbType.BigInt);
        AddIfColumnExists(connection, "api_loglari", "partner_id", partnerId, SqlDbType.BigInt);
        AddIfColumnExists(connection, "api_loglari", "baslangic_tarihi", DateTime.UtcNow, SqlDbType.DateTime2);

        if (columns.Count == 0)
        {
            return;
        }

        var sql = $"INSERT INTO api_loglari ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)});";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddRange(parameters.ToArray());
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return;

        void AddIfColumnExists(SqlConnection conn, string table, string column, object? value, SqlDbType type, int size = 0)
        {
            if (!ColumnExistsAsync(conn, table, column, cancellationToken).GetAwaiter().GetResult())
            {
                return;
            }

            var name = "@p" + columns.Count.ToString(CultureInfo.InvariantCulture);
            columns.Add(column);
            values.Add(name);

            var p = new SqlParameter(name, type);
            if (size != 0) p.Size = size;
            p.Value = value ?? DBNull.Value;
            parameters.Add(p);
        }
    }

    public async Task TryLogExceptionAsync(HttpContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "sistem_hata_loglari", cancellationToken))
        {
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString();
        var path = $"{context.Request.Path}{context.Request.QueryString}";
        var ua = context.Request.Headers.UserAgent.ToString();
        var userId = TryGetUserId(context.User);

        // Basit, güvenli bir exception logu: message + stack + path (payload yok)
        var sql = @"
            INSERT INTO sistem_hata_loglari
            (hata_seviyesi, hata_mesaji, stack_trace, endpoint, ip_adresi, user_agent, kullanici_id, cozuldu_mu, olusturulma_tarihi)
            VALUES
            (@level, @msg, @stack, @endpoint, @ip, @ua, @userId, 0, SYSUTCDATETIME());";

        try
        {
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@level", "ERROR");
            cmd.Parameters.AddWithValue("@msg", exception.Message);
            cmd.Parameters.AddWithValue("@stack", exception.ToString());
            cmd.Parameters.AddWithValue("@endpoint", path);
            cmd.Parameters.AddWithValue("@ip", (object?)ip ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ua", (object?)ua ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@userId", userId.HasValue && userId.Value > 0 ? userId.Value : (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Log yazımı, uygulamayı asla bozmasın.
        }
    }

    public async Task TryLogAdminActionAsync(long adminUserId, string actionType, string targetTable, string? targetId, string? note, string? ipAddress, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "admin_islem_loglari", cancellationToken))
        {
            return;
        }

        const string sql = @"
            INSERT INTO admin_islem_loglari
            (admin_kullanici_id, islem_turu, hedef_tablo, hedef_kayit_id, aciklama, ip_adresi, islem_tarihi)
            VALUES
            (@adminId, @type, @table, @targetId, @note, @ip, SYSUTCDATETIME());";

        try
        {
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@adminId", adminUserId);
            cmd.Parameters.AddWithValue("@type", actionType);
            cmd.Parameters.AddWithValue("@table", targetTable);
            cmd.Parameters.AddWithValue("@targetId", string.IsNullOrWhiteSpace(targetId) ? (object)DBNull.Value : targetId.Trim());
            cmd.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(note) ? (object)DBNull.Value : note.Trim());
            cmd.Parameters.AddWithValue("@ip", string.IsNullOrWhiteSpace(ipAddress) ? (object)DBNull.Value : ipAddress.Trim());
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Fail-safe
        }
    }

    private static long? TryGetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(AuthClaimTypes.UserId) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    private static long? TryGetPartnerId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(AuthClaimTypes.PartnerId);
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @name;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@name", tableName);
        var raw = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(raw, CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @t AND COLUMN_NAME = @c;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@t", tableName);
        cmd.Parameters.AddWithValue("@c", columnName);
        var raw = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(raw, CultureInfo.InvariantCulture) > 0;
    }
}

