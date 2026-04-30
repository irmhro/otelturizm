using Microsoft.Data.SqlClient;

namespace otelturizmnew.Services;

public sealed class PricingRetentionBackgroundService : BackgroundService
{
    private static readonly TimeSpan RunAt = new(3, 15, 0);
    private readonly IConfiguration _configuration;
    private readonly ILogger<PricingRetentionBackgroundService> _logger;

    public PricingRetentionBackgroundService(
        IConfiguration configuration,
        ILogger<PricingRetentionBackgroundService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiyat/müsaitlik retention temizliği çalıştırılamadı.");
            }
        }
    }

    private TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var next = now.Date.Add(RunAt);
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await ProcedureExistsAsync(connection, "dbo.usp_fiyat_musaitlik_retention_cleanup", cancellationToken))
        {
            _logger.LogWarning("usp_fiyat_musaitlik_retention_cleanup bulunamadı. Temizlik atlandı.");
            return;
        }

        await using var command = new SqlCommand("EXEC dbo.usp_fiyat_musaitlik_retention_cleanup;", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            _logger.LogInformation(
                "Fiyat retention temizliği çalıştı. Cutoff={Cutoff}, OdaSilinen={RoomCount}, FirmaSilinen={CompanyCount}",
                reader.IsDBNull(0) ? null : reader.GetDateTime(0),
                reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                reader.IsDBNull(2) ? 0 : reader.GetInt32(2));
        }
    }

    private static async Task<bool> ProcedureExistsAsync(SqlConnection connection, string fullName, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM sys.procedures WHERE object_id = OBJECT_ID(@name);";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", fullName);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count > 0;
    }
}
