using Microsoft.AspNetCore.Http;
using otelturizmnew.Models.Reservations;

namespace otelturizmnew.Services.Abstractions;

public interface IPublicReservationService
{
    Task<ReservationDraftSummaryViewModel?> GetActiveDraftAsync(long? userId, string? sessionKey, CancellationToken cancellationToken = default);
    Task<PublicReservationResult> StartReservationAsync(long? userId, string? sessionKey, PublicHotelReservationForm form, IFormFile? bankTransferReceipt, CancellationToken cancellationToken = default);
    Task<PublicReservationPriceQuoteViewModel> GetPriceQuoteAsync(
        long roomTypeId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int roomCount,
        string? guestCheckOutTime = null,
        bool applyLateCheckoutSurcharge = true,
        CancellationToken cancellationToken = default);
    Task SaveBookingDraftAsync(long? userId, string sessionKey, long hotelId, string hotelSlug, PublicHotelReservationForm form, CancellationToken cancellationToken = default);
    void ApplyDawnSurpriseToQuote(PublicReservationPriceQuoteViewModel quote);
}
