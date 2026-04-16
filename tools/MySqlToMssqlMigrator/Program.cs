using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using MySqlConnector;

var options = ParseArgs(args);
var rootPath = options.TryGetValue("root", out var rootArg)
    ? Path.GetFullPath(rootArg)
    : ResolveProjectRoot(AppContext.BaseDirectory);

var mysqlConnectionString = options.TryGetValue("mysql", out var mysqlArg)
    ? mysqlArg
    : LoadConnectionString(rootPath, "appsettings.Development.json")
      ?? LoadConnectionString(rootPath, "appsettings.json")
      ?? throw new InvalidOperationException("MySQL baglanti bilgisi bulunamadi. --mysql ile verin.");

var sqlServerConnectionString = options.TryGetValue("mssql", out var mssqlArg)
    ? mssqlArg
    : "Server=(localdb)\\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;";

var mysqlBuilder = new MySqlConnectionStringBuilder(mysqlConnectionString);
var sourceDatabase = options.TryGetValue("source-db", out var sourceDbArg) && !string.IsNullOrWhiteSpace(sourceDbArg)
    ? sourceDbArg
    : mysqlBuilder.Database;
if (string.IsNullOrWhiteSpace(sourceDatabase))
{
    throw new InvalidOperationException("Kaynak MySQL veritabani bulunamadi.");
}

var sqlBuilder = new SqlConnectionStringBuilder(sqlServerConnectionString);
var targetDatabase = options.TryGetValue("target-db", out var targetDbArg) && !string.IsNullOrWhiteSpace(targetDbArg)
    ? targetDbArg
    : (string.IsNullOrWhiteSpace(sqlBuilder.InitialCatalog) ? "otelturizm_2026db" : sqlBuilder.InitialCatalog);
if (string.IsNullOrWhiteSpace(targetDatabase))
{
    targetDatabase = "otelturizm_2026db";
}
sqlBuilder.InitialCatalog = targetDatabase;
sqlBuilder.TrustServerCertificate = true;

var schemaOnly = options.ContainsKey("schema-only");
var truncateTarget = !options.ContainsKey("no-truncate");
var selectedTables = options.TryGetValue("tables", out var tablesArg) && !string.IsNullOrWhiteSpace(tablesArg)
    ? new HashSet<string>(
        tablesArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        StringComparer.OrdinalIgnoreCase)
    : null;

Console.WriteLine($"Root Path: {rootPath}");
Console.WriteLine($"Source MySQL DB: {sourceDatabase}");
Console.WriteLine($"Target SQL DB: {targetDatabase}");
Console.WriteLine($"Mode: {(schemaOnly ? "schema-only" : "schema+data")}");

await EnsureSqlDatabaseExistsAsync(sqlBuilder, targetDatabase);

await using var mysqlConnection = new MySqlConnection(mysqlConnectionString);
await using var sqlConnection = new SqlConnection(sqlBuilder.ConnectionString);
await mysqlConnection.OpenAsync();
await sqlConnection.OpenAsync();

var tables = await LoadTablesAsync(mysqlConnection, sourceDatabase);
if (selectedTables is not null)
{
    tables = tables.Where(selectedTables.Contains).ToList();
}

if (tables.Count == 0)
{
    Console.WriteLine("Tasima icin tablo bulunamadi.");
    return;
}

foreach (var table in tables)
{
    Console.WriteLine($"[{table}] isleniyor...");
    var columns = await LoadColumnsAsync(mysqlConnection, sourceDatabase, table);
    var primaryKey = await LoadPrimaryKeyColumnsAsync(mysqlConnection, sourceDatabase, table);
    var createSql = BuildCreateTableSql(table, columns, primaryKey);
    await using (var createCommand = new SqlCommand(createSql, sqlConnection))
    {
        await createCommand.ExecuteNonQueryAsync();
    }

    if (schemaOnly)
    {
        continue;
    }

    await CopyTableDataAsync(mysqlConnection, sqlConnection, table, columns, truncateTarget);
}

Console.WriteLine("Tasima islemi tamamlandi.");

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var token = args[i];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = token[2..];
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            result[key] = args[i + 1];
            i++;
        }
        else
        {
            result[key] = "true";
        }
    }

    return result;
}

static string? LoadConnectionString(string rootPath, string appSettingsFile)
{
    var path = Path.Combine(rootPath, appSettingsFile);
    if (!File.Exists(path))
    {
        return null;
    }

    using var stream = File.OpenRead(path);
    using var document = JsonDocument.Parse(stream);
    if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
    {
        return null;
    }

    if (!connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection))
    {
        return null;
    }

    return defaultConnection.GetString();
}

static string ResolveProjectRoot(string baseDirectory)
{
    var current = new DirectoryInfo(baseDirectory);
    while (current is not null)
    {
        var appSettings = Path.Combine(current.FullName, "appsettings.json");
        var appSettingsDev = Path.Combine(current.FullName, "appsettings.Development.json");
        if (File.Exists(appSettings) || File.Exists(appSettingsDev))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
}

static async Task EnsureSqlDatabaseExistsAsync(SqlConnectionStringBuilder sqlBuilder, string targetDatabase)
{
    var adminBuilder = new SqlConnectionStringBuilder(sqlBuilder.ConnectionString)
    {
        InitialCatalog = "master"
    };

    await using var connection = new SqlConnection(adminBuilder.ConnectionString);
    await connection.OpenAsync();
    var commandText = $"""
        IF DB_ID(@dbName) IS NULL
        BEGIN
            EXEC('CREATE DATABASE [{EscapeSqlIdentifier(targetDatabase)}]');
        END
        """;
    await using var command = new SqlCommand(commandText, connection);
    command.Parameters.AddWithValue("@dbName", targetDatabase);
    await command.ExecuteNonQueryAsync();
}

static async Task<List<string>> LoadTablesAsync(MySqlConnection mysqlConnection, string sourceDatabase)
{
    const string sql = """
        SELECT TABLE_NAME
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = @schema
          AND TABLE_TYPE = 'BASE TABLE'
        ORDER BY TABLE_NAME;
        """;
    var result = new List<string>();
    await using var command = new MySqlCommand(sql, mysqlConnection);
    command.Parameters.AddWithValue("@schema", sourceDatabase);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(reader.GetString(0));
    }

    return result;
}

static async Task<List<ColumnInfo>> LoadColumnsAsync(MySqlConnection mysqlConnection, string sourceDatabase, string table)
{
    const string sql = """
        SELECT
            COLUMN_NAME,
            DATA_TYPE,
            COLUMN_TYPE,
            CHARACTER_MAXIMUM_LENGTH,
            NUMERIC_PRECISION,
            NUMERIC_SCALE,
            DATETIME_PRECISION,
            IS_NULLABLE,
            COLUMN_DEFAULT,
            EXTRA,
            ORDINAL_POSITION
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = @schema
          AND TABLE_NAME = @table
        ORDER BY ORDINAL_POSITION;
        """;

    var result = new List<ColumnInfo>();
    await using var command = new MySqlCommand(sql, mysqlConnection);
    command.Parameters.AddWithValue("@schema", sourceDatabase);
    command.Parameters.AddWithValue("@table", table);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(new ColumnInfo(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetInt64(3),
            reader.IsDBNull(4) ? null : reader.GetInt32(4),
            reader.IsDBNull(5) ? null : reader.GetInt32(5),
            reader.IsDBNull(6) ? null : reader.GetInt32(6),
            string.Equals(reader.GetString(7), "YES", StringComparison.OrdinalIgnoreCase),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            reader.GetInt32(10)));
    }

    return result;
}

static async Task<List<string>> LoadPrimaryKeyColumnsAsync(MySqlConnection mysqlConnection, string sourceDatabase, string table)
{
    const string sql = """
        SELECT k.COLUMN_NAME
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t
        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
          ON k.CONSTRAINT_NAME = t.CONSTRAINT_NAME
         AND k.TABLE_SCHEMA = t.TABLE_SCHEMA
         AND k.TABLE_NAME = t.TABLE_NAME
        WHERE t.TABLE_SCHEMA = @schema
          AND t.TABLE_NAME = @table
          AND t.CONSTRAINT_TYPE = 'PRIMARY KEY'
        ORDER BY k.ORDINAL_POSITION;
        """;

    var result = new List<string>();
    await using var command = new MySqlCommand(sql, mysqlConnection);
    command.Parameters.AddWithValue("@schema", sourceDatabase);
    command.Parameters.AddWithValue("@table", table);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(reader.GetString(0));
    }

    return result;
}

static string BuildCreateTableSql(string table, IReadOnlyList<ColumnInfo> columns, IReadOnlyList<string> primaryKeys)
{
    var lines = new List<string>();
    foreach (var column in columns)
    {
        var sqlType = MapSqlServerType(column);
        var identity = column.IsAutoIncrement ? " IDENTITY(1,1)" : string.Empty;
        var nullable = column.IsNullable ? " NULL" : " NOT NULL";
        var defaultExpression = column.IsAutoIncrement ? null : MapDefaultExpression(column.DefaultValue);
        var defaultSql = string.IsNullOrWhiteSpace(defaultExpression)
            ? string.Empty
            : $" DEFAULT {defaultExpression}";
        lines.Add($"[{EscapeSqlIdentifier(column.Name)}] {sqlType}{identity}{nullable}{defaultSql}");
    }

    if (primaryKeys.Count > 0)
    {
        var pkCols = string.Join(", ", primaryKeys.Select(pk => $"[{EscapeSqlIdentifier(pk)}]"));
        lines.Add($"CONSTRAINT [PK_{EscapeSqlIdentifier(table)}] PRIMARY KEY ({pkCols})");
    }

    var columnsSql = string.Join("," + Environment.NewLine + "        ", lines);
    return $"""
        IF OBJECT_ID(N'[dbo].[{EscapeSqlIdentifier(table)}]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[{EscapeSqlIdentifier(table)}] (
                {columnsSql}
            );
        END;
        """;
}

static string MapSqlServerType(ColumnInfo column)
{
    var dataType = column.DataType.ToLowerInvariant();
    return dataType switch
    {
        "bigint" => "BIGINT",
        "int" or "integer" or "mediumint" => "INT",
        "smallint" => "SMALLINT",
        "tinyint" when column.ColumnType.StartsWith("tinyint(1", StringComparison.OrdinalIgnoreCase) => "BIT",
        "tinyint" => "TINYINT",
        "decimal" or "numeric" => $"DECIMAL({column.NumericPrecision ?? 18},{column.NumericScale ?? 2})",
        "double" => "FLOAT",
        "float" => "REAL",
        "bit" => "BIT",
        "date" => "DATE",
        "datetime" or "timestamp" => $"DATETIME2({Math.Clamp(column.DateTimePrecision ?? 3, 0, 7)})",
        "time" => $"TIME({Math.Clamp(column.DateTimePrecision ?? 0, 0, 7)})",
        "char" => $"NCHAR({NormalizeLength(column.CharacterMaximumLength, 1, 4000)})",
        "varchar" => $"NVARCHAR({NormalizeLength(column.CharacterMaximumLength, 1, 4000)})",
        "tinytext" or "text" or "mediumtext" or "longtext" or "json" => "NVARCHAR(MAX)",
        "binary" => $"BINARY({NormalizeLength(column.CharacterMaximumLength, 1, 8000)})",
        "varbinary" => $"VARBINARY({NormalizeLength(column.CharacterMaximumLength, 1, 8000)})",
        "tinyblob" or "blob" or "mediumblob" or "longblob" => "VARBINARY(MAX)",
        "enum" or "set" => "NVARCHAR(255)",
        _ => "NVARCHAR(MAX)"
    };
}

static int NormalizeLength(long? rawLength, int min, int max)
{
    if (!rawLength.HasValue || rawLength.Value <= 0)
    {
        return min;
    }

    return (int)Math.Clamp(rawLength.Value, min, max);
}

static string? MapDefaultExpression(string? rawDefault)
{
    if (string.IsNullOrWhiteSpace(rawDefault))
    {
        return null;
    }

    var value = rawDefault.Trim();
    if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    if (value.StartsWith("current_timestamp", StringComparison.OrdinalIgnoreCase))
    {
        return "SYSUTCDATETIME()";
    }

    if (value.StartsWith("b'", StringComparison.OrdinalIgnoreCase) && value.EndsWith("'", StringComparison.Ordinal))
    {
        return value.Contains('1') ? "1" : "0";
    }

    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
    {
        return value;
    }

    if (value.StartsWith("'") && value.EndsWith("'") && value.Length >= 2)
    {
        var inner = value[1..^1].Replace("'", "''", StringComparison.Ordinal);
        return $"N'{inner}'";
    }

    return null;
}

static async Task CopyTableDataAsync(
    MySqlConnection mysqlConnection,
    SqlConnection sqlConnection,
    string table,
    IReadOnlyList<ColumnInfo> columns,
    bool truncateTarget)
{
    if (truncateTarget)
    {
        var deleteSql = $"DELETE FROM [dbo].[{EscapeSqlIdentifier(table)}];";
        await using var deleteCommand = new SqlCommand(deleteSql, sqlConnection);
        await deleteCommand.ExecuteNonQueryAsync();
    }

    var identityColumn = columns.FirstOrDefault(c => c.IsAutoIncrement)?.Name;
    if (!string.IsNullOrWhiteSpace(identityColumn))
    {
        var identityOnSql = $"SET IDENTITY_INSERT [dbo].[{EscapeSqlIdentifier(table)}] ON;";
        await using var identityOnCommand = new SqlCommand(identityOnSql, sqlConnection);
        await identityOnCommand.ExecuteNonQueryAsync();
    }

    var selectSql = $"SELECT * FROM `{table}`;";
    var columnSql = string.Join(", ", columns.Select(c => $"[{EscapeSqlIdentifier(c.Name)}]"));
    var parameterSql = string.Join(", ", columns.Select((_, idx) => $"@p{idx}"));
    var insertSql = $"INSERT INTO [dbo].[{EscapeSqlIdentifier(table)}] ({columnSql}) VALUES ({parameterSql});";

    long copied = 0;
    try
    {
        await using var selectCommand = new MySqlCommand(selectSql, mysqlConnection);
        await using var reader = await selectCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            await using var insertCommand = new SqlCommand(insertSql, sqlConnection);
            for (var i = 0; i < columns.Count; i++)
            {
                var rawValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                var parameter = CreateSqlParameter($"@p{i}", columns[i], rawValue);
                insertCommand.Parameters.Add(parameter);
            }

            await insertCommand.ExecuteNonQueryAsync();
            copied++;
        }
    }
    finally
    {
        if (!string.IsNullOrWhiteSpace(identityColumn))
        {
            var identityOffSql = $"SET IDENTITY_INSERT [dbo].[{EscapeSqlIdentifier(table)}] OFF;";
            await using var identityOffCommand = new SqlCommand(identityOffSql, sqlConnection);
            await identityOffCommand.ExecuteNonQueryAsync();
        }
    }

    Console.WriteLine($"[{table}] -> {copied} satir kopyalandi.");
}

static object NormalizeValue(object value)
{
    return value switch
    {
        ulong u => u <= long.MaxValue ? (long)u : Convert.ToDecimal(u, CultureInfo.InvariantCulture),
        uint u => (long)u,
        ushort u => (int)u,
        sbyte b => (short)b,
        TimeSpan t => t,
        DateTime dt => dt,
        byte[] bytes => bytes,
        bool flag => flag,
        _ => value
    };
}

static SqlParameter CreateSqlParameter(string name, ColumnInfo column, object? rawValue)
{
    var dbType = MapSqlDbType(column);
    var parameter = new SqlParameter(name, dbType);
    if (rawValue is null || rawValue == DBNull.Value)
    {
        parameter.Value = DBNull.Value;
        return parameter;
    }

    var normalizedValue = NormalizeValue(rawValue);
    if (normalizedValue is DateTime dt)
    {
        if (dbType == SqlDbType.DateTime && dt < new DateTime(1753, 1, 1))
        {
            normalizedValue = new DateTime(1753, 1, 1, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Unspecified);
        }

        if (dbType == SqlDbType.Date)
        {
            normalizedValue = dt.Date;
        }
    }

    parameter.Value = normalizedValue;
    return parameter;
}

static SqlDbType MapSqlDbType(ColumnInfo column)
{
    var dataType = column.DataType.ToLowerInvariant();
    return dataType switch
    {
        "bigint" => SqlDbType.BigInt,
        "int" or "integer" or "mediumint" => SqlDbType.Int,
        "smallint" => SqlDbType.SmallInt,
        "tinyint" when column.ColumnType.StartsWith("tinyint(1", StringComparison.OrdinalIgnoreCase) => SqlDbType.Bit,
        "tinyint" => SqlDbType.TinyInt,
        "decimal" or "numeric" => SqlDbType.Decimal,
        "double" => SqlDbType.Float,
        "float" => SqlDbType.Real,
        "bit" => SqlDbType.Bit,
        "date" => SqlDbType.Date,
        "datetime" or "timestamp" => SqlDbType.DateTime2,
        "time" => SqlDbType.Time,
        "char" => SqlDbType.NChar,
        "varchar" => SqlDbType.NVarChar,
        "tinytext" or "text" or "mediumtext" or "longtext" or "json" => SqlDbType.NVarChar,
        "binary" => SqlDbType.Binary,
        "varbinary" => SqlDbType.VarBinary,
        "tinyblob" or "blob" or "mediumblob" or "longblob" => SqlDbType.VarBinary,
        "enum" or "set" => SqlDbType.NVarChar,
        _ => SqlDbType.NVarChar
    };
}

static string EscapeSqlIdentifier(string value) => value.Replace("]", "]]", StringComparison.Ordinal);

internal sealed record ColumnInfo(
    string Name,
    string DataType,
    string ColumnType,
    long? CharacterMaximumLength,
    int? NumericPrecision,
    int? NumericScale,
    int? DateTimePrecision,
    bool IsNullable,
    string? DefaultValue,
    string Extra,
    int OrdinalPosition)
{
    public bool IsAutoIncrement =>
        Extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase);
}
