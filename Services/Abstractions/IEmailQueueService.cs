using MySqlConnector;
using otelturizmnew.Models.Email;

namespace otelturizmnew.Services.Abstractions;

public interface IEmailQueueService
{
    Task QueueTemplateAsync(QueuedEmailTemplateRequest request, CancellationToken cancellationToken = default);
    Task QueueTemplateAsync(MySqlConnection connection, MySqlTransaction? transaction, QueuedEmailTemplateRequest request, CancellationToken cancellationToken = default);
}
