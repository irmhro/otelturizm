namespace otelturizmnew.Services.Abstractions;

/// <summary>
/// Ödeme sağlayıcı orkestrasyonu (3DS retry, yedek gateway) için genişletilebilir köprü — şimdilik yapılandırma + log.
/// </summary>
public interface IPaymentOrchestrationAdvisor
{
    void LogOrchestrationContext(string stage, string? detail = null);
}
