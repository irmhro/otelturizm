using otelturizmnew.Models.Anasayfa;
using otelturizmnew.Models.Oteller;

namespace otelturizmnew.Services.Abstractions;

public interface IHotelService
{
    Task<AnasayfaViewModel> GetHomepageAsync(CancellationToken cancellationToken = default);
    Task<HotelListingPageViewModel> GetHotelListingPageAsync(string city, string? campaignTag = null, CancellationToken cancellationToken = default);
    Task<HotelDetailPageViewModel?> GetHotelDetailPageAsync(string slug, CancellationToken cancellationToken = default);
}

