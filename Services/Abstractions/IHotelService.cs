using otelturizmnew.Models.Anasayfa;
using otelturizmnew.Models.Oteller;

namespace otelturizmnew.Services.Abstractions;

public interface IHotelService
{
    Task<AnasayfaViewModel> GetHomepageAsync(CancellationToken cancellationToken = default);
    Task<HotelListingPageViewModel> GetHotelListingPageAsync(string? searchTerm, string? campaignTag = null, string? campaignSlug = null, int page = 1, string? contextualSearchBoostNormalized = null, decimal? minPrice = null, decimal? maxPrice = null, long? ilceId = null, long? sehirId = null, CancellationToken cancellationToken = default);
    Task<HotelDetailPageViewModel?> GetHotelDetailPageAsync(string slug, HotelDetailLoadOptions? options = null, CancellationToken cancellationToken = default);
    Task<List<HotelSearchSuggestionViewModel>> GetSearchSuggestionsAsync(string query, CancellationToken cancellationToken = default);
}

