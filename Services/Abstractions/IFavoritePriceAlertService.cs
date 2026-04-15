using MySqlConnector;

namespace otelturizmnew.Services.Abstractions;

public interface IFavoritePriceAlertService
{
    Task QueuePriceRecheckJobAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        long hotelId,
        DateTime startDate,
        DateTime endDate,
        long triggeredByUserId,
        CancellationToken cancellationToken = default);

    Task ProcessPendingJobsAsync(CancellationToken cancellationToken = default);
}
