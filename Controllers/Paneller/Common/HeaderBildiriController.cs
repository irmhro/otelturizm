using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Paneller.Common;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Paneller.Common;

[Authorize]
[ApiController]
[Route("panel/header-bildiri")]
public class HeaderBildiriController : ControllerBase
{
    private readonly IHeaderBildiriService _headerBildiriService;

    public HeaderBildiriController(IHeaderBildiriService headerBildiriService)
    {
        _headerBildiriService = headerBildiriService;
    }

    [HttpPost("okundu")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead([FromBody] HeaderBildiriReadRequest? request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Unauthorized();
        }

        if (request is null || request.ItemKeys is null)
        {
            return BadRequest(new { success = false, message = "Geçersiz istek." });
        }

        await _headerBildiriService.MarkAsReadAsync(request.PanelKey, userId, request.ItemKeys, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpGet("ozet")]
    public async Task<IActionResult> GetSummary([FromQuery] string? panelKey, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Unauthorized();
        }

        var model = await _headerBildiriService.GetForPanelAsync(panelKey ?? PanelHeaderAudience.User, userId, cancellationToken);
        return Ok(new
        {
            panelKey = model.PanelKey,
            panelLabel = model.PanelLabel,
            unreadCount = model.UnreadCount,
            totalCount = model.TotalCount,
            items = model.Items.Select(item => new
            {
                itemKey = item.ItemKey,
                title = item.Title,
                description = item.Description,
                tone = item.Tone,
                timeLabel = item.TimeLabel,
                absoluteTimeLabel = item.AbsoluteTimeLabel,
                url = item.Url,
                isRead = item.IsRead,
                isPlaceholder = item.IsPlaceholder
            })
        });
    }

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }
}
