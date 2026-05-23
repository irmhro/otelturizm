using Microsoft.Extensions.Hosting;

namespace otelturizmnew.Services;

/// <summary>
/// Eski rezervasyonları arşive taşıma işi için yer tutucu (paket 236).
/// Üretimde tarih/eşik ve rezervasyonlar_archive şeması ile doldurulmalıdır.
/// </summary>
public sealed class ReservationsArchiveBackgroundService : BackgroundService
{
    private readonly ILogger<ReservationsArchiveBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public ReservationsArchiveBackgroundService(
        ILogger<ReservationsArchiveBackgroundService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                var enabled = _configuration.GetValue("Archive:Reservations:Enabled", false);
                _logger.LogInformation(
                    "ARCHIVE_COLD_STORAGE_TICK enabled={Enabled}",
                    enabled);
                if (enabled)
                {
                    _logger.LogInformation("ARCHIVE_COLD_STORAGE_PLANNED migrate old rows to [dbo].[REZERVASYONLAR_ARSIV] (implement with DBA review).");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ARCHIVE_COLD_STORAGE_TICK failed.");
            }
        }
    }
}
