using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Api;

[ApiController]
[Route("api/integrations/whatsapp")]
public class TelefonDogrulamaWebhookController : ControllerBase
{
    private readonly IPhoneVerificationService _phoneVerificationService;

    public TelefonDogrulamaWebhookController(IPhoneVerificationService phoneVerificationService)
    {
        _phoneVerificationService = phoneVerificationService;
    }

    [HttpGet("webhook")]
    public async Task<IActionResult> VerifyWebhook([FromQuery(Name = "hub.mode")] string? mode, [FromQuery(Name = "hub.verify_token")] string? verifyToken, [FromQuery(Name = "hub.challenge")] string? challenge, CancellationToken cancellationToken)
    {
        if (!string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported hub.mode");
        }

        if (string.IsNullOrWhiteSpace(verifyToken) || !await _phoneVerificationService.VerifyWebhookChallengeAsync(verifyToken, cancellationToken))
        {
            return Unauthorized();
        }

        return Content(challenge ?? string.Empty, "text/plain");
    }

    [HttpPost("webhook")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);
        var signatureHeader = Request.Headers["X-Hub-Signature-256"].ToString();
        var result = await _phoneVerificationService.HandleWebhookAsync(rawPayload, signatureHeader, cancellationToken);
        return result.Success ? Ok() : Unauthorized();
    }
}
