using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace otelturizmnew.Services.Health;

/// <summary>SQL Server erişilebilirlik kontrolü (Health Checks pipeline).</summary>
public sealed class SqlConnectionHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlConnectionHealthCheck> _logger;

    public SqlConnectionHealthCheck(IConfiguration configuration, ILogger<SqlConnectionHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cs = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
        {
            return HealthCheckResult.Unhealthy("DefaultConnection tanimli degil.");
        }

        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand("SELECT 1;", conn) { CommandTimeout = 3 };
            await cmd.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "sql_health_check_failed");
            return HealthCheckResult.Unhealthy("SQL ping basarisiz.");
        }
    }
}
