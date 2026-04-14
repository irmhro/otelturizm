using otelturizmnew.Models.Destek;

namespace otelturizmnew.Services.Abstractions;

public interface ISupportService
{
    Task<YardimMerkeziViewModel> GetHelpCenterAsync(string? searchTerm, CancellationToken cancellationToken = default);
    Task<SssViewModel> GetFaqPageAsync(string? categorySlug, string? searchTerm, CancellationToken cancellationToken = default);
}
