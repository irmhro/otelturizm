using otelturizmnew.Models.Oteller;
using otelturizmnew.Models.Paneller.User;

namespace otelturizmnew.Services.Abstractions;

public interface IUserFavoriteService
{
    Task<HashSet<long>> GetFavoriteHotelIdsAsync(long userId, IEnumerable<long> hotelIds, CancellationToken cancellationToken = default);
    Task<int> GetFavoriteCountAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserFavoritesPageViewModel> GetFavoritesPageAsync(long userId, string? searchTerm = null, string? sort = null, int page = 1, CancellationToken cancellationToken = default);
    Task<OtelFavoriToggleYanit> ToggleFavoriteAsync(
        long userId,
        long hotelId,
        string sourcePage,
        string sourceUrl,
        string? deviceType,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> SavePriceAlertAsync(
        long userId,
        UserFavoritePriceAlertForm form,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> DeletePriceAlertAsync(
        long userId,
        long hotelId,
        CancellationToken cancellationToken = default);
}
