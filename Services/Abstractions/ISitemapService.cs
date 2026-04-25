namespace otelturizmnew.Services.Abstractions;

public interface ISitemapService
{
    Task EnsureFreshSitemapAsync(bool force = false, CancellationToken cancellationToken = default);
    Task<string> GetSitemapXmlAsync(CancellationToken cancellationToken = default);
}
