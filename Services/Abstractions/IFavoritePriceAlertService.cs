using System.Data.Common;

namespace otelturizmnew.Services.Abstractions;

public interface IFavoritePriceAlertService
{
    Task QueuePriceRecheckJobAsync(
        DbConnection connection,
        DbTransaction? transaction,
        long hotelId,
        DateTime startDate,
        DateTime endDate,
        long triggeredByUserId,
        CancellationToken cancellationToken = default);

    Task ProcessPendingJobsAsync(CancellationToken cancellationToken = default);
}
