using otelturizmnew.Models.Oteller;
using otelturizmnew.Models.Paneller.Partner;

namespace otelturizmnew.Services.Abstractions;

public interface ISmartRouteService
{
    Task<List<SmartRouteFilterViewModel>> GetListingFiltersAsync(CancellationToken cancellationToken = default);

    Task EnrichListingCardsAsync(IReadOnlyList<HotelListingCardViewModel> cards, CancellationToken cancellationToken = default);

    Task<PartnerSmartRoutesPageViewModel> GetPartnerPageAsync(long userId, long? hotelId, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> ToggleMembershipAsync(long userId, PartnerSmartRouteToggleRequest request, CancellationToken cancellationToken = default);

    void InvalidateCache();
}
