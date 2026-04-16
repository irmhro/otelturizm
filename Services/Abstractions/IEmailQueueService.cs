using System.Data.Common;
using otelturizmnew.Models.Email;

namespace otelturizmnew.Services.Abstractions;

public interface IEmailQueueService
{
    Task QueueTemplateAsync(QueuedEmailTemplateRequest request, CancellationToken cancellationToken = default);
    Task QueueTemplateAsync(DbConnection connection, DbTransaction? transaction, QueuedEmailTemplateRequest request, CancellationToken cancellationToken = default);
}
