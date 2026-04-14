using System.Security.Cryptography;
using System.Text;
using MySqlConnector;

namespace otelturizmnew.Data;

public sealed class SqlMigrationRunner
{
    private const string ScriptsFolder = "Database\\MigrationsSql";
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
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("ConnectionStrings:DefaultConnection tanimli degil. SQL migration adimi atlandi.");
            return;
        }

        var scriptsPath = Path.Combine(_environment.ContentRootPath, ScriptsFolder);
        if (!Directory.Exists(scriptsPath))
        {
            _logger.LogWarning("Migration klasoru bulunamadi: {Path}. SQL migration adimi atlandi.", scriptsPath);
            return;
        }

        var scriptFiles = Directory
            .EnumerateFiles(scriptsPath, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (scriptFiles.Count == 0)
        {
            _logger.LogInformation("Calistirilacak SQL migration scripti yok.");
            return;
        }

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureHistoryTableAsync(connection, cancellationToken);

        foreach (var scriptFile in scriptFiles)
        {
            var scriptName = Path.GetFileName(scriptFile);
            var scriptText = await File.ReadAllTextAsync(scriptFile, cancellationToken);
            var checksum = ComputeSha256(scriptText);

            var existingChecksum = await GetAppliedChecksumAsync(connection, scriptName, cancellationToken);
            if (existingChecksum is not null)
            {
                if (!string.Equals(existingChecksum, checksum, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Migration script degismis: {scriptName}. Yeni bir script dosyasi (or: 002_*.sql) olusturun.");
                }

                _logger.LogInformation("Script zaten uygulanmis: {ScriptName}", scriptName);
                continue;
            }

            var statements = SplitSqlStatements(scriptText).ToList();
            foreach (var statement in statements)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = statement;
                command.CommandTimeout = 180;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
INSERT INTO schema_migrations (script_name, checksum, applied_at)
VALUES (@scriptName, @checksum, UTC_TIMESTAMP());";
            insertCommand.Parameters.AddWithValue("@scriptName", scriptName);
            insertCommand.Parameters.AddWithValue("@checksum", checksum);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Script uygulandi: {ScriptName} ({StatementCount} ifade)", scriptName, statements.Count);
        }
    }

    private static async Task EnsureHistoryTableAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS schema_migrations (
    script_name VARCHAR(255) NOT NULL PRIMARY KEY,
    checksum CHAR(64) NOT NULL,
    applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string?> GetAppliedChecksumAsync(
        MySqlConnection connection,
        string scriptName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT checksum FROM schema_migrations WHERE script_name = @scriptName LIMIT 1;";
        command.Parameters.AddWithValue("@scriptName", scriptName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }

    private static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static IEnumerable<string> SplitSqlStatements(string sql)
    {
        var sb = new StringBuilder();

        var inSingleQuote = false;
        var inDoubleQuote = false;
        var inBacktick = false;
        var inLineComment = false;
        var inBlockComment = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];
            var next = i + 1 < sql.Length ? sql[i + 1] : '\0';

            if (inLineComment)
            {
                sb.Append(c);
                if (c == '\n')
                {
                    inLineComment = false;
                }
                continue;
            }

            if (inBlockComment)
            {
                sb.Append(c);
                if (c == '*' && next == '/')
                {
                    sb.Append(next);
                    i++;
                    inBlockComment = false;
                }
                continue;
            }

            if (!inSingleQuote && !inDoubleQuote && !inBacktick)
            {
                if (c == '-' && next == '-')
                {
                    sb.Append(c);
                    sb.Append(next);
                    i++;
                    inLineComment = true;
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    sb.Append(c);
                    sb.Append(next);
                    i++;
                    inBlockComment = true;
                    continue;
                }
            }

            if (!inDoubleQuote && !inBacktick && c == '\'' && !IsEscaped(sql, i))
            {
                inSingleQuote = !inSingleQuote;
            }
            else if (!inSingleQuote && !inBacktick && c == '"' && !IsEscaped(sql, i))
            {
                inDoubleQuote = !inDoubleQuote;
            }
            else if (!inSingleQuote && !inDoubleQuote && c == '`')
            {
                inBacktick = !inBacktick;
            }

            if (c == ';' && !inSingleQuote && !inDoubleQuote && !inBacktick)
            {
                var statement = sb.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    yield return statement;
                }

                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        var trailing = sb.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(trailing))
        {
            yield return trailing;
        }
    }

    private static bool IsEscaped(string text, int index)
    {
        var backslashCount = 0;
        for (var i = index - 1; i >= 0 && text[i] == '\\'; i--)
        {
            backslashCount++;
        }

        return backslashCount % 2 == 1;
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
}

