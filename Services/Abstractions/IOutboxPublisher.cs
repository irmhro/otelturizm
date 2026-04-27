namespace otelturizmnew.Services.Abstractions;

/// <summary>
/// Transactional outbox: DB commit ile aynı atomik işlemde kuyruk kaydı (paket 242 hedef mimari).
/// Şimdilik no-op / log; tablo migration ile birlikte gerçek INSERT uygulanacaktır.
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>Fiziksel outbox tablosuna yazılmadan önce planlanan olayları işaretler (şimdilik structured log).</summary>
    Task PublishPlannedAsync(string eventType, string payloadSummary, CancellationToken cancellationToken = default);
}
