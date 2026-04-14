using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Constants;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Api;

[ApiController]
[Route("api/favoriler")]
public class FavoritesApiController : Controller
{
    private readonly IUserFavoriteService _userFavoriteService;

    public FavoritesApiController(IUserFavoriteService userFavoriteService)
    {
        _userFavoriteService = userFavoriteService;
    }

    [HttpPost("toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle([FromBody] HotelFavoriteToggleRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId <= 0)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new HotelFavoriteToggleResponse
            {
                Success = false,
                Message = "Favori eklemek için lütfen giriş yapınız.",
                LoginUrl = "/kullanici-giris",
                RegisterUrl = "/kullanici-giris?sekme=kayit"
            });
        }

        var response = await _userFavoriteService.ToggleFavoriteAsync(
            userId,
            request.HotelId,
            request.SourcePage,
            request.SourceUrl,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    private long GetUserId()
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var userId) ? userId : 0;
    }
}
