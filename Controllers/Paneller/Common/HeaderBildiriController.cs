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
    public async Task<IActionResult> MarkAsRead([FromBody] HeaderBildiriReadRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Unauthorized();
        }

        await _headerBildiriService.MarkAsReadAsync(request.PanelKey, userId, request.ItemKeys, cancellationToken);
        return Ok(new { success = true });
    }

    private long GetCurrentUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }
}
