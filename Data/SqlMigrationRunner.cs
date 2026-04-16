namespace otelturizmnew.Data;

public sealed class SqlMigrationRunner
{
    private const string ScriptsFolder = "Database\\MigrationsSql";
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SqlMigrationRunner> _logger;
    private readonly bool _isSqlServer;

    public SqlMigrationRunner(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<SqlMigrationRunner> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        var configuredProvider = configuration["Database:Provider"];
        _isSqlServer = string.Equals(configuredProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
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

        if (_isSqlServer)
        {
            _logger.LogInformation(
                "SQL script runner dogrulandi. Proje MSSQL/LocalDB modunda calisiyor ve startup migration adimi bilerek pasif tutuluyor.");
            return;
        }

        _logger.LogWarning(
            "Database:Provider degeri SqlServer degil. Bu repo artik sadece MSSQL standardi ile devam ediyor; startup migration adimi calistirilmayacak.");
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
