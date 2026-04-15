using otelturizmnew.Models.Reservations;

namespace otelturizmnew.Services.Abstractions;

public interface IReservationDraftService
{
    string EnsureSessionKey(HttpContext httpContext);
    Task<ReservationDraftSummaryViewModel?> GetActiveDraftAsync(long? userId, string? sessionKey, CancellationToken cancellationToken = default);
    Task<long> SaveOrUpdateAsync(ReservationDraftUpsertRequest request, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(long draftId, long reservationId, CancellationToken cancellationToken = default);
}
