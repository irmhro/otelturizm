using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using otelturizmnew.Models.TelefonDogrulama;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class WhatsAppCloudApiService : IWhatsAppCloudApiService
{
    private readonly HttpClient _httpClient;

    public WhatsAppCloudApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WhatsAppCloudSendResult> SendVerificationTemplateAsync(WhatsAppCloudSendRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = request.RecipientPhoneE164.TrimStart('+'),
            type = "template",
            template = new
            {
                name = request.TemplateName,
                language = new
                {
                    code = request.LanguageCode
                },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = request.VerificationCode
                            }
                        }
                    }
                }
            }
        };

        var requestPayload = JsonSerializer.Serialize(payload);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v22.0/{request.PhoneNumberId}/messages");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);
        httpRequest.Content = new StringContent(requestPayload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = new WhatsAppCloudSendResult
        {
            Success = response.IsSuccessStatusCode,
            RequestPayload = requestPayload,
            ResponsePayload = responsePayload
        };

        try
        {
            using var document = JsonDocument.Parse(responsePayload);
            if (response.IsSuccessStatusCode)
            {
                if (document.RootElement.TryGetProperty("messages", out var messagesElement)
                    && messagesElement.ValueKind == JsonValueKind.Array
                    && messagesElement.GetArrayLength() > 0
                    && messagesElement[0].TryGetProperty("id", out var idElement))
                {
                    result.MessageId = idElement.GetString() ?? string.Empty;
                }
            }
            else
            {
                if (document.RootElement.TryGetProperty("error", out var errorElement))
                {
                    if (errorElement.TryGetProperty("code", out var codeElement))
                    {
                        result.ErrorCode = codeElement.ToString();
                    }

                    if (errorElement.TryGetProperty("message", out var messageElement))
                    {
                        result.ErrorMessage = messageElement.GetString() ?? "WhatsApp Cloud API hatasi.";
                    }
                }
            }
        }
        catch
        {
            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                result.ErrorMessage = "WhatsApp Cloud API cevabi ayrıştırılamadı.";
            }
        }

        if (!result.Success && string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            result.ErrorMessage = $"WhatsApp Cloud API isteği başarısız oldu. HTTP {(int)response.StatusCode}.";
        }

        return result;
    }
}
