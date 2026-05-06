using otelturizmnew.Models.Destek;

namespace otelturizmnew.Services.Abstractions;

public interface ISupportService
{
    Task<YardimMerkeziViewModel> GetHelpCenterAsync(string? searchTerm, CancellationToken cancellationToken = default);
    Task<YardimMerkeziKategoriDetaySayfaViewModel?> GetHelpCategoryAsync(string slug, string? searchTerm, CancellationToken cancellationToken = default);
    Task<YardimMerkeziIcerikSayfaViewModel?> GetHelpContentPageAsync(string type, string slug, CancellationToken cancellationToken = default);
    Task<SssViewModel> GetFaqPageAsync(string? categorySlug, string? searchTerm, CancellationToken cancellationToken = default);
}
