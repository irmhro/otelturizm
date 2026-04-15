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

        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            AllowUserVariables = true
        };

        await using var connection = new MySqlConnection(builder.ConnectionString);
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
                try
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = statement;
                    command.CommandTimeout = 180;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (MySqlException ex) when (IsIdempotentMigrationConflict(ex))
                {
                    _logger.LogWarning(
                        "Migration ifadesi mevcut semaya zaten uygulanmis gorunuyor, devam ediliyor. Script: {ScriptName}, Kod: {ErrorCode}, Mesaj: {Message}",
                        scriptName,
                        ex.Number,
                        ex.Message);
                }
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
        var normalizedSql = sql.Replace("\r\n", "\n");
        var lines = normalizedSql.Split('\n');
        var delimiter = ";";
        var current = new StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = rawLine ?? string.Empty;
            var trimmed = line.Trim();
            if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
            {
                delimiter = trimmed["DELIMITER ".Length..].Trim();
                if (string.IsNullOrWhiteSpace(delimiter))
                {
                    delimiter = ";";
                }
                continue;
            }

            current.Append(line);
            current.Append('\n');

            if (!StatementEndsWithDelimiter(current, delimiter))
            {
                continue;
            }

            var statementText = current.ToString();
            var delimiterIndex = statementText.LastIndexOf(delimiter, StringComparison.Ordinal);
            if (delimiterIndex >= 0)
            {
                statementText = statementText[..delimiterIndex];
            }

            var statement = statementText.Trim();
            if (!string.IsNullOrWhiteSpace(statement))
            {
                yield return statement;
            }

            current.Clear();
        }

        var trailing = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(trailing))
        {
            yield return trailing;
        }
    }

    private static bool StatementEndsWithDelimiter(StringBuilder statementBuilder, string delimiter)
    {
        if (statementBuilder.Length == 0)
        {
            return false;
        }

        var statementText = statementBuilder.ToString().TrimEnd();
        return statementText.EndsWith(delimiter, StringComparison.Ordinal);
    }

    private static bool IsIdempotentMigrationConflict(MySqlException ex)
    {
        // MySQL duplicate/object exists codes commonly seen when an old schema already contains bootstrap objects.
        return ex.Number is 1007 or 1050 or 1060 or 1061 or 1062 or 1091 or 1451 or 1452 or 1831 or 1826;
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

