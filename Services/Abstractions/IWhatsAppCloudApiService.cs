using otelturizmnew.Models.TelefonDogrulama;

namespace otelturizmnew.Services.Abstractions;

public interface IWhatsAppCloudApiService
{
    Task<WhatsAppCloudSendResult> SendVerificationTemplateAsync(WhatsAppCloudSendRequest request, CancellationToken cancellationToken = default);
}
