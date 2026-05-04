using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using otelturizmnew.Constants;
using otelturizmnew.Models.DeveloperFeedback;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Common;

[Authorize]
[Route("dev-bildirim")]
public sealed class DeveloperFeedbackController : Controller
{
    private readonly IDeveloperFeedbackService _feedbackService;

    public DeveloperFeedbackController(IDeveloperFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost("gonder")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 8388608)]
    [RequestSizeLimit(8388608)]
    public async Task<IActionResult> Submit(DeveloperFeedbackForm form, CancellationToken cancellationToken)
    {
        DeveloperFeedbackActionResponse actionResponse;
        try
        {
            var userIdRaw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = long.TryParse(userIdRaw, out var parsedUserId) ? parsedUserId : 0;
            var result = form.FeedbackId.HasValue && form.FeedbackId.Value > 0
                ? await _feedbackService.UpdateAsync(userId, form, cancellationToken)
                : await _feedbackService.CreateAsync(
                    userId,
                    User.FindFirstValue(AuthClaimTypes.FullName) ?? User.Identity?.Name,
                    User.FindFirstValue(AuthClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Email),
                    User.FindFirstValue(AuthClaimTypes.AccountType),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    form,
                    cancellationToken);

            TempData[result.Success ? "PanelToastSuccess" : "PanelToastError"] = result.Message;
            if (result.Success)
            {
                TempData["DeveloperFeedbackSubmitted"] = "1";
            }
            actionResponse = new DeveloperFeedbackActionResponse { Success = result.Success, Message = result.Message };
        }
        catch (Exception ex)
        {
            var message = $"Bildirim gönderilemedi: {ex.Message}";
            TempData["PanelToastError"] = message;
            actionResponse = new DeveloperFeedbackActionResponse { Success = false, Message = message };
        }

        if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
        {
            return Json(actionResponse);
        }

        var returnUrl = string.IsNullOrWhiteSpace(form.ReturnUrl) ? "/" : form.ReturnUrl;
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : Redirect("/");
    }

    [HttpGet("gecmis")]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = long.TryParse(userIdRaw, out var parsedUserId) ? parsedUserId : 0;
        var result = await _feedbackService.GetUserHistoryAsync(userId, cancellationToken);
        return Json(result);
    }

    [HttpPost("sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = long.TryParse(userIdRaw, out var parsedUserId) ? parsedUserId : 0;
        var result = await _feedbackService.DeleteAsync(userId, id, cancellationToken);
        return Json(new DeveloperFeedbackActionResponse { Success = result.Success, Message = result.Message });
    }
}
