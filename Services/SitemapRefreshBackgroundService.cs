using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class SitemapRefreshBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SitemapRefreshBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sitemapService = scope.ServiceProvider.GetRequiredService<ISitemapService>();
                await sitemapService.EnsureFreshSitemapAsync(false, stoppingToken);
            }
            catch
            {
                // Uygulamayi durdurmamak icin sessiz geciyoruz.
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
