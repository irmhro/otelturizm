using otelturizmnew.Models.Kampanyalar;

namespace otelturizmnew.Services.Abstractions;

public interface ICampaignService
{
    Task<CampaignListingPageViewModel> GetCampaignListingPageAsync(CancellationToken cancellationToken = default);
    Task<CampaignDetailPageViewModel?> GetCampaignDetailPageAsync(string slug, CancellationToken cancellationToken = default);
}
