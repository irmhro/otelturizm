using otelturizmnew.Models.Reservations;

namespace otelturizmnew.Services.Abstractions;

public interface IPublicReservationService
{
    Task<ReservationDraftSummaryViewModel?> GetActiveDraftAsync(long? userId, string? sessionKey, CancellationToken cancellationToken = default);
    Task<PublicReservationResult> StartReservationAsync(long? userId, string? sessionKey, PublicHotelReservationForm form, CancellationToken cancellationToken = default);
    Task<PublicReservationPriceQuoteViewModel> GetPriceQuoteAsync(long roomTypeId, DateOnly checkInDate, DateOnly checkOutDate, int roomCount, CancellationToken cancellationToken = default);
}
