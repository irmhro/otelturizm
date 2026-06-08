using otelturizmnew.Models.Seo;

namespace otelturizmnew.Services.Abstractions;

public interface ISitemapService
{
    Task EnsureFreshSitemapAsync(bool force = false, CancellationToken cancellationToken = default);
    Task<string> GetSitemapXmlAsync(CancellationToken cancellationToken = default);
    Task<string?> GetSubSitemapXmlAsync(string fileName, CancellationToken cancellationToken = default);
    Task<string?> GetRegionalSitemapXmlAsync(string fileName, CancellationToken cancellationToken = default);
    Task<string?> GetHotelOffersFeedJsonAsync(CancellationToken cancellationToken = default);
    Task<SitemapDiagnosticsViewModel> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
}
