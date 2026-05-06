using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace otelturizmnew.Data;

public sealed class SqlMigrationRunner
{
    private const string ScriptsFolder = "Database\\MigrationsSql";
    private const string SnapshotFolder = "Database\\MigrationsSql\\000_current_schema_by_table";
    private const string HistoryTable = "dbo.__sql_migrations";

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SqlMigrationRunner> _logger;

    public SqlMigrationRunner(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<SqlMigrationRunner> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var configuredProvider = _configuration["Database:Provider"];
        var isSqlServer = string.Equals(configuredProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        if (!isSqlServer)
        {
            _logger.LogWarning(
                "Database:Provider degeri SqlServer degil. Bu repo MSSQL standardi ile devam ediyor; SQL migration runner calistirilmayacak.");
            return;
        }

        var connectionStringName = _configuration["Database:MigrationConnectionStringName"];
        if (string.IsNullOrWhiteSpace(connectionStringName))
        {
            connectionStringName = "DefaultConnection";
        }

        var connectionString = _configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("ConnectionStrings:{Name} tanimli degil. SQL migration adimi atlandi.", connectionStringName);
            return;
        }

        var scriptsPath = Path.Combine(_environment.ContentRootPath, ScriptsFolder);
        if (!Directory.Exists(scriptsPath))
        {
            _logger.LogWarning("Migration klasoru bulunamadi: {Path}. SQL migration adimi atlandi.", scriptsPath);
            return;
        }

        var scriptFiles = Directory
            .EnumerateFiles(scriptsPath, "*.sql", SearchOption.AllDirectories)
            .Select(path => new
            {
                FullPath = path,
                RelativePath = Path.GetRelativePath(scriptsPath, path),
                FileName = Path.GetFileName(path)
            })
            .OrderBy(x =>
            {
                // Önce: tablo/kolon onarımları (000_current_schema_by_table\0xx..8xx)
                // Sonra: normal sqlserver migration/seed dosyaları (kök klasör)
                // En son: index/fk paketleri (000_current_schema_by_table\9xx) - kolonlar oluşmadan patlamasın
                if (x.RelativePath.StartsWith("000_current_schema_by_table", StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = Path.GetFileName(x.RelativePath);
                    return fileName.StartsWith("9", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                }
                return 1;
            })
            .ThenBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (scriptFiles.Count == 0)
        {
            _logger.LogInformation("Calistirilacak SQL migration scripti yok.");
            return;
        }

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        await EnsureHistoryTableAsync(conn, cancellationToken);

        var appliedCount = 0;
        var skippedCount = 0;

        foreach (var file in scriptFiles)
        {
            if (IsIgnoredScript(file.FileName, file.RelativePath))
            {
                skippedCount++;
                continue;
            }

            var scriptText = await File.ReadAllTextAsync(file.FullPath, Encoding.UTF8, cancellationToken);
            if (IsUnsupportedForSqlServer(scriptText))
            {
                _logger.LogWarning("SQL migration atlandi (SQL Server uyumsuz): {File}", file.FileName);
                skippedCount++;
                continue;
            }
            var hash = ComputeSha256Hex(scriptText);

            if (await IsAlreadyAppliedAsync(conn, file.FileName, hash, cancellationToken))
            {
                skippedCount++;
                continue;
            }

            _logger.LogInformation("SQL migration calistiriliyor: {File}", file.FileName);
            try
            {
                await ExecuteScriptAsync(conn, scriptText, cancellationToken);
                await MarkAppliedAsync(conn, file.FileName, hash, cancellationToken);
                appliedCount++;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL migration hata verdi: {File}", file.FileName);
                throw new InvalidOperationException($"SQL migration hata verdi: {file.FileName}. Detay: {ex.Message}", ex);
            }
        }

        _logger.LogInformation("SQL migration tamamlandi. Uygulanan: {Applied}, Atlanan: {Skipped}", appliedCount, skippedCount);
    }

    public async Task<string> GenerateSchemaDriftRepairScriptAsync(CancellationToken cancellationToken = default)
    {
        var configuredProvider = _configuration["Database:Provider"];
        var isSqlServer = string.Equals(configuredProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        if (!isSqlServer)
        {
            return "-- Database:Provider SqlServer degil; schema drift raporu uretilmedi.";
        }

        var connectionStringName = _configuration["Database:MigrationConnectionStringName"];
        if (string.IsNullOrWhiteSpace(connectionStringName))
        {
            connectionStringName = "DefaultConnection";
        }

        var connectionString = _configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"-- ConnectionStrings:{connectionStringName} tanimli degil; schema drift raporu uretilmedi.";
        }

        var snapshotPath = Path.Combine(_environment.ContentRootPath, SnapshotFolder);
        if (!Directory.Exists(snapshotPath))
        {
            return $"-- Snapshot klasoru bulunamadi: {snapshotPath}";
        }

        var expected = ReadExpectedSchemaFromSnapshot(snapshotPath);
        if (expected.Count == 0)
        {
            return "-- Snapshot icinden beklenen schema okunamadi.";
        }

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var existing = await ReadExistingColumnsAsync(conn, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("-- AUTO-GENERATED (SqlMigrationRunner): local schema drift repair");
        sb.AppendLine("-- Kaynak: Database/MigrationsSql/000_current_schema_by_table snapshot");
        sb.AppendLine("SET NOCOUNT ON;");
        sb.AppendLine();

        var missingCount = 0;
        foreach (var (tableName, columns) in expected.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!existing.TryGetValue(tableName, out var existingCols))
            {
                // tablo yoksa bu rapor sadece kolonu hedefleyemez; tablo create snapshot tarafından zaten yapılmalı
                continue;
            }

            foreach (var (colName, colDef) in columns.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (existingCols.Contains(colName))
                {
                    continue;
                }

                missingCount++;
                sb.AppendLine($"IF COL_LENGTH(N'dbo.{tableName}', N'{colName}') IS NULL");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    ALTER TABLE [dbo].[{tableName}] ADD [{colName}] {colDef};");
                sb.AppendLine("END");
                sb.AppendLine("GO");
            }
        }

        if (missingCount == 0)
        {
            sb.AppendLine("-- Eksik kolon bulunmadi.");
        }

        return sb.ToString();
    }

    public async Task OptimizeSqlServerAsync(CancellationToken cancellationToken = default)
    {
        var configuredProvider = _configuration["Database:Provider"];
        var isSqlServer = string.Equals(configuredProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        if (!isSqlServer)
        {
            _logger.LogWarning("Database:Provider SqlServer degil. Optimize adimi atlandi.");
            return;
        }

        var connectionStringName = _configuration["Database:MigrationConnectionStringName"];
        if (string.IsNullOrWhiteSpace(connectionStringName))
        {
            connectionStringName = "DefaultConnection";
        }

        var connectionString = _configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("ConnectionStrings:{Name} tanimli degil. Optimize adimi atlandi.", connectionStringName);
            return;
        }

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        // 1) Update stats (hızlı ve güvenli)
        _logger.LogInformation("DB: sp_updatestats calistiriliyor...");
        await using (var cmd = new SqlCommand("EXEC sp_updatestats;", conn) { CommandTimeout = 600 })
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // 2) Lightweight index maintenance (reorganize) for local/dev
        // Not: REBUILD yerine REORGANIZE kullanıyoruz; daha az kilit ve daha hızlı.
        var indexSql = @"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql = @sql + N'ALTER INDEX ALL ON ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) + N' REORGANIZE;'
FROM sys.tables t
WHERE t.is_ms_shipped = 0;
EXEC sp_executesql @sql;";
        _logger.LogInformation("DB: index REORGANIZE calistiriliyor...");
        await using (var cmd = new SqlCommand(indexSql, conn) { CommandTimeout = 900 })
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("DB optimize tamamlandi.");
    }

    private static Dictionary<string, Dictionary<string, string>> ReadExpectedSchemaFromSnapshot(string snapshotPath)
    {
        var expected = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(snapshotPath, "*_table_*.sql", SearchOption.TopDirectoryOnly))
        {
            var text = File.ReadAllText(file, Encoding.UTF8);
            var createIdx = text.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase);
            if (createIdx < 0) continue;

            var openParenIdx = text.IndexOf('(', createIdx);
            if (openParenIdx < 0) continue;

            // tablo adı: CREATE TABLE [dbo].[oteller]  veya CREATE TABLE [dbo].[xxx]
            var header = text.Substring(createIdx, Math.Min(220, text.Length - createIdx));
            var tableName = ExtractTableName(header);
            if (string.IsNullOrWhiteSpace(tableName)) continue;

            var endIdx = text.IndexOf(");", openParenIdx, StringComparison.OrdinalIgnoreCase);
            if (endIdx < 0) continue;

            var block = text.Substring(openParenIdx + 1, endIdx - (openParenIdx + 1));
            var lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (!expected.TryGetValue(tableName, out var cols))
            {
                cols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                expected[tableName] = cols;
            }

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (!line.StartsWith("[", StringComparison.Ordinal)) continue;
                if (line.StartsWith("[CONSTRAINT", StringComparison.OrdinalIgnoreCase)) continue;

                var closeIdx = line.IndexOf(']');
                if (closeIdx <= 1) continue;
                var colName = line.Substring(1, closeIdx - 1).Trim();
                var rest = line.Substring(closeIdx + 1).Trim();
                if (string.IsNullOrWhiteSpace(colName) || string.IsNullOrWhiteSpace(rest)) continue;

                // trailing comma
                if (rest.EndsWith(",", StringComparison.Ordinal))
                {
                    rest = rest[..^1].TrimEnd();
                }

                // computed columns: "AS (...)" -> keep as-is
                // normal columns: "bigint NOT NULL" etc -> keep as-is
                cols[colName] = rest;
            }
        }

        return expected;
    }

    private static string ExtractTableName(string createHeader)
    {
        // basit: [dbo].[table] pattern
        var idx = createHeader.IndexOf("[dbo].[", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return string.Empty;
        var start = idx + "[dbo].[".Length;
        var end = createHeader.IndexOf(']', start);
        if (end <= start) return string.Empty;
        return createHeader.Substring(start, end - start);
    }

    private static async Task<Dictionary<string, HashSet<string>>> ReadExistingColumnsAsync(SqlConnection conn, CancellationToken ct)
    {
        const string sql = @"
            SELECT TABLE_NAME, COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo';";

        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var table = r.GetString(0);
            var col = r.GetString(1);
            if (!map.TryGetValue(table, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[table] = set;
            }
            set.Add(col);
        }
        return map;
    }

    private static bool IsIgnoredScript(string fileName, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return true;
        if (fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase)) return true;
        // Tek dosyada toplu uygulama runbook'u; runner zaten scriptleri tek tek izliyor (çift çalışma riski).
        if (fileName.Equals("20260504_apply_all_migrations_safe.sql", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!relativePath.StartsWith("000_current_schema_by_table", StringComparison.OrdinalIgnoreCase))
        {
            // Kök klasördeki eski/MySQL döneminden kalma 0xx create/seed scriptleri var.
            // MSSQL için: sqlserver_*, 20260504_seed_*, 20YYMMDD_* tarih önekli migrationlar, tema_panel.
            var lower = fileName.ToLowerInvariant();
            var isSqlServerScript = lower.Contains("sqlserver");
            var isSeedScript = lower.StartsWith("20260504_seed_", StringComparison.OrdinalIgnoreCase);
            var isThemePanelScript = lower.Contains("tema_panel", StringComparison.OrdinalIgnoreCase);
            var isDatedMigration = IsEightDigitDatedMigration(fileName);
            if (!isSqlServerScript && !isSeedScript && !isDatedMigration)
            {
                if (isThemePanelScript)
                {
                    return false;
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>20YYMMDD_*.sql biçimindeki tarih önekli MSSQL migration dosyaları.</summary>
    private static bool IsEightDigitDatedMigration(string fileName)
    {
        if (fileName.Length < 10 || fileName[8] != '_')
        {
            return false;
        }

        for (var i = 0; i < 8; i++)
        {
            if (!char.IsDigit(fileName[i]))
            {
                return false;
            }
        }

        return fileName.StartsWith("20", StringComparison.Ordinal);
    }

    private static bool IsUnsupportedForSqlServer(string scriptText)
    {
        // Repo geçmişinde MySQL kalan scriptler var; MSSQL runner bunları pas geçmeli.
        // Heuristik: SQL Server'da hata veren ifadeler/anahtarlar.
        var s = scriptText ?? string.Empty;
        return s.Contains("SET NAMES", StringComparison.OrdinalIgnoreCase)
               || s.Contains("FOREIGN_KEY_CHECKS", StringComparison.OrdinalIgnoreCase)
               || s.Contains("ENGINE=", StringComparison.OrdinalIgnoreCase)
               || s.Contains("DEFAULT CHARSET", StringComparison.OrdinalIgnoreCase)
               || s.Contains("CHARSET=", StringComparison.OrdinalIgnoreCase)
               || s.Contains("ENUM(", StringComparison.OrdinalIgnoreCase)
               || s.Contains("CREATE TABLE IF NOT EXISTS", StringComparison.OrdinalIgnoreCase)
               || s.Contains("ON DELETE SET NULL", StringComparison.OrdinalIgnoreCase)
               || s.Contains("INDEX idx_", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task EnsureHistoryTableAsync(SqlConnection conn, CancellationToken ct)
    {
        var sql = $@"
IF OBJECT_ID(N'{HistoryTable}', N'U') IS NULL
BEGIN
    CREATE TABLE {HistoryTable} (
        id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        script_name nvarchar(260) NOT NULL,
        script_hash char(64) NOT NULL,
        applied_at datetime2(0) NOT NULL CONSTRAINT DF___sql_migrations_applied_at DEFAULT (sysutcdatetime()),
        CONSTRAINT UQ___sql_migrations_name_hash UNIQUE(script_name, script_hash)
    );
    CREATE INDEX IX___sql_migrations_name ON {HistoryTable}(script_name);
END";
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<bool> IsAlreadyAppliedAsync(SqlConnection conn, string scriptName, string scriptHash, CancellationToken ct)
    {
        var sql = $"SELECT TOP (1) 1 FROM {HistoryTable} WHERE script_name = @name AND script_hash = @hash;";
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        cmd.Parameters.AddWithValue("@name", scriptName);
        cmd.Parameters.AddWithValue("@hash", scriptHash);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is not null && result is not DBNull;
    }

    private static async Task MarkAppliedAsync(SqlConnection conn, string scriptName, string scriptHash, CancellationToken ct)
    {
        var sql = $"INSERT INTO {HistoryTable}(script_name, script_hash) VALUES (@name, @hash);";
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        cmd.Parameters.AddWithValue("@name", scriptName);
        cmd.Parameters.AddWithValue("@hash", scriptHash);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task ExecuteScriptAsync(SqlConnection conn, string scriptText, CancellationToken ct)
    {
        var batches = SplitOnGo(scriptText);
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var cmd = new SqlCommand(batch, conn) { CommandTimeout = 300 };
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static List<string> SplitOnGo(string script)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        using var sr = new StringReader(script ?? string.Empty);
        string? line;
        while ((line = sr.ReadLine()) is not null)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }
            sb.AppendLine(line);
        }
        if (sb.Length > 0) result.Add(sb.ToString());
        return result;
    }

    private static string ComputeSha256Hex(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public static class SqlMigrationRunnerExtensions
{
    public static async Task RunSqlMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<SqlMigrationRunner>();
        await runner.RunAsync(cancellationToken);
    }

    public static async Task<string> GenerateSchemaDriftRepairScriptAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<SqlMigrationRunner>();
        return await runner.GenerateSchemaDriftRepairScriptAsync(cancellationToken);
    }

    public static async Task OptimizeSqlServerAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<SqlMigrationRunner>();
        await runner.OptimizeSqlServerAsync(cancellationToken);
    }
}
