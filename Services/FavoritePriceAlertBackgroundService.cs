using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class FavoritePriceAlertBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FavoritePriceAlertBackgroundService> _logger;

    public FavoritePriceAlertBackgroundService(IServiceScopeFactory scopeFactory, ILogger<FavoritePriceAlertBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IFavoritePriceAlertService>();
                await service.ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiyat alarmi arka plan isleyicisi hata verdi.");
            }

            await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        }
    }
}
