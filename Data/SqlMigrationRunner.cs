using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace otelturizmnew.Data;

public sealed class SqlMigrationRunner
{
    private const string ScriptsFolder = "Database\\MigrationsSql";
    private const string TableMigrationsFolder = "tablo\\migrationlar";
    private const string DataMigrationsFolder = "veri\\migrationlar";
    private const string LegacyTablesFolder = "tables";
    private const string ConstraintsFolder = "constraints";
    private const string SnapshotFolder = "Database\\MigrationsSql\\tablo\\migrationlar";
    private const string HistoryTable = "dbo.SEMA_MIGRASYONLARI";

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

        var scriptFiles = BuildOrderedScriptFiles(scriptsPath);

        if (scriptFiles.Count == 0)
        {
            _logger.LogInformation("Calistirilacak SQL migration scripti yok.");
            return;
        }

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await ApplyRequiredSetOptionsAsync(conn, cancellationToken);

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
        await ApplyRequiredSetOptionsAsync(conn, cancellationToken);

        var existing = await ReadExistingColumnsAsync(conn, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("-- AUTO-GENERATED (SqlMigrationRunner): local schema drift repair");
        sb.AppendLine("-- Kaynak: Database/MigrationsSql/tablo/migrationlar snapshot");
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
        await ApplyRequiredSetOptionsAsync(conn, cancellationToken);

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

        foreach (var file in Directory.EnumerateFiles(snapshotPath, "*.sql", SearchOption.TopDirectoryOnly))
        {
            var text = File.ReadAllText(file, Encoding.UTF8);
            var createIdx = text.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase);
            if (createIdx < 0) continue;

            var openParenIdx = text.IndexOf('(', createIdx);
            if (openParenIdx < 0) continue;

            var header = text.Substring(createIdx, Math.Min(220, text.Length - createIdx));
            var tableName = ExtractTableName(header);
            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = ExtractTableNameFromSnapshotFile(Path.GetFileName(file));
            }
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

    private static string ExtractTableNameFromSnapshotFile(string fileName)
    {
        // 077_OTELLER.sql -> OTELLER
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var idx = stem.IndexOf('_');
        if (idx < 0 || idx >= stem.Length - 1) return string.Empty;
        return stem[(idx + 1)..];
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

    private static List<(string FullPath, string RelativePath, string FileName)> BuildOrderedScriptFiles(string scriptsPath)
    {
        var scriptFiles = new List<(string FullPath, string RelativePath, string FileName)>();

        AddSqlFilesFromFolder(scriptFiles, scriptsPath, TableMigrationsFolder);
        AddSqlFilesFromFolder(scriptFiles, scriptsPath, LegacyTablesFolder);

        foreach (var tail in new[] { "900_foreign_keys.sql", "901_indexes.sql", "902_triggers.sql" })
        {
            var path = Path.Combine(scriptsPath, ConstraintsFolder, tail);
            if (!File.Exists(path))
            {
                path = Path.Combine(scriptsPath, tail);
            }
            if (!File.Exists(path)) continue;
            var rel = Path.GetRelativePath(scriptsPath, path);
            scriptFiles.Add((path, rel, tail));
        }

        AddSqlFilesFromFolder(scriptFiles, scriptsPath, DataMigrationsFolder);

        foreach (var path in Directory.EnumerateFiles(scriptsPath, "*.sql", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(path);
            if (!IsEightDigitDatedMigration(fileName))
            {
                continue;
            }

            scriptFiles.Add((path, fileName, fileName));
        }

        return scriptFiles
            .OrderBy(x => GetScriptSortKey(x.RelativePath, x.FileName))
            .ThenBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddSqlFilesFromFolder(
        List<(string FullPath, string RelativePath, string FileName)> scriptFiles,
        string scriptsPath,
        string folderRelative)
    {
        var folderPath = Path.Combine(scriptsPath, folderRelative);
        if (!Directory.Exists(folderPath))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(folderPath, "*.sql"))
        {
            var fileName = Path.GetFileName(path);
            var relativePath = Path.Combine(folderRelative, fileName);
            scriptFiles.Add((path, relativePath, fileName));
        }
    }

    private static int GetScriptSortKey(string relativePath, string fileName)
    {
        if (IsUnderMigrationFolder(relativePath, TableMigrationsFolder)
            || IsUnderMigrationFolder(relativePath, LegacyTablesFolder))
        {
            return 0;
        }

        if (fileName.StartsWith("900_", StringComparison.OrdinalIgnoreCase)) return 1;
        if (fileName.StartsWith("901_", StringComparison.OrdinalIgnoreCase)) return 2;
        if (fileName.StartsWith("902_", StringComparison.OrdinalIgnoreCase)) return 3;
        if (IsUnderMigrationFolder(relativePath, DataMigrationsFolder)) return 4;
        if (IsEightDigitDatedMigration(fileName)) return 4;
        return 9;
    }

    private static bool IsUnderMigrationFolder(string relativePath, string folderRelative)
    {
        return relativePath.StartsWith($"{folderRelative}\\", StringComparison.OrdinalIgnoreCase)
               || relativePath.StartsWith($"{folderRelative}/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIgnoredScript(string fileName, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return true;
        if (fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase)) return true;
        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return true;
        if (relativePath.Contains("_archive", StringComparison.OrdinalIgnoreCase)) return true;
        if (fileName.Equals("20260504_apply_all_migrations_safe.sql", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // veri/migrationlar altinda zaten var; kokteki kopya cift calistirmayi onler
        if (fileName.Equals("20260524_seed_koordinat_turkiye.sql", StringComparison.OrdinalIgnoreCase)
            && string.Equals(relativePath, fileName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsUnderMigrationFolder(relativePath, TableMigrationsFolder)
            || IsUnderMigrationFolder(relativePath, LegacyTablesFolder)
            || IsUnderMigrationFolder(relativePath, DataMigrationsFolder))
        {
            return false;
        }

        if (relativePath.StartsWith($"{ConstraintsFolder}\\", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith($"{ConstraintsFolder}/", StringComparison.OrdinalIgnoreCase))
        {
            if (fileName.StartsWith("900_", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("901_", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("902_", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        if (fileName.StartsWith("900_", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("901_", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("902_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsEightDigitDatedMigration(fileName)
            && string.Equals(relativePath, fileName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
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
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [BETIK_ADI] nvarchar(260) NOT NULL,
        [KONTROL_TOPLAMI] char(64) NOT NULL,
        [UYGULANMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_SEMA_MIGRASYONLARI_UYGULANMA] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_SEMA_MIGRASYONLARI] PRIMARY KEY CLUSTERED ([ID] ASC),
        CONSTRAINT [UQ_SEMA_MIGRASYONLARI_BETIK_HASH] UNIQUE ([BETIK_ADI], [KONTROL_TOPLAMI])
    );
    CREATE INDEX [IX_SEMA_MIGRASYONLARI_BETIK] ON {HistoryTable}([BETIK_ADI]);
END";
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<bool> IsAlreadyAppliedAsync(SqlConnection conn, string scriptName, string scriptHash, CancellationToken ct)
    {
        var sql = $"SELECT TOP (1) 1 FROM {HistoryTable} WHERE [BETIK_ADI] = @name AND [KONTROL_TOPLAMI] = @hash;";
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        cmd.Parameters.AddWithValue("@name", scriptName);
        cmd.Parameters.AddWithValue("@hash", scriptHash);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is not null && result is not DBNull;
    }

    private static async Task MarkAppliedAsync(SqlConnection conn, string scriptName, string scriptHash, CancellationToken ct)
    {
        var sql = $"INSERT INTO {HistoryTable}([BETIK_ADI], [KONTROL_TOPLAMI]) VALUES (@name, @hash);";
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
            await ApplyRequiredSetOptionsAsync(conn, ct);
            await using var cmd = new SqlCommand(batch, conn) { CommandTimeout = 300 };
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task ApplyRequiredSetOptionsAsync(SqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("""
            SET ANSI_NULLS ON;
            SET ANSI_PADDING ON;
            SET ANSI_WARNINGS ON;
            SET ARITHABORT ON;
            SET CONCAT_NULL_YIELDS_NULL ON;
            SET QUOTED_IDENTIFIER ON;
            SET NUMERIC_ROUNDABORT OFF;
            """, conn)
        {
            CommandTimeout = 60
        };
        await cmd.ExecuteNonQueryAsync(ct);
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
