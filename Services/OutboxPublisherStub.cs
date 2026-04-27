using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

/// <summary>Outbox tablosu gelene kadar güvenli yer tutucu.</summary>
public sealed class OutboxPublisherStub : IOutboxPublisher
{
    private readonly ILogger<OutboxPublisherStub> _logger;

    public OutboxPublisherStub(ILogger<OutboxPublisherStub> logger)
    {
        _logger = logger;
    }

    public Task PublishPlannedAsync(string eventType, string payloadSummary, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "OUTBOX_EVENT_PLANNED type={EventType} summary={Summary}",
            eventType,
            payloadSummary.Length > 400 ? payloadSummary[..400] + "…" : payloadSummary);
        return Task.CompletedTask;
    }
}
