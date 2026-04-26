using otelturizmnew.Models.Kampanyalar;

namespace otelturizmnew.Services.Abstractions;

public interface ICampaignService
{
    Task<CampaignListingPageViewModel> GetCampaignListingPageAsync(string? preset = null, CancellationToken cancellationToken = default);
    Task<CampaignDetailPageViewModel?> GetCampaignDetailPageAsync(
        string slug,
        string? q = null,
        string? city = null,
        string? district = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default);
}
