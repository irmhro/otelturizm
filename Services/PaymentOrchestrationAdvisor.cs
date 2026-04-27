using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class PaymentOrchestrationAdvisor : IPaymentOrchestrationAdvisor
{
    private readonly ILogger<PaymentOrchestrationAdvisor> _logger;

    public PaymentOrchestrationAdvisor(ILogger<PaymentOrchestrationAdvisor> logger)
    {
        _logger = logger;
    }

    public void LogOrchestrationContext(string stage, string? detail = null)
    {
        _logger.LogInformation("PAYMENT_ORCHESTRATION stage={Stage} detail={Detail}", stage, detail ?? string.Empty);
    }
}
