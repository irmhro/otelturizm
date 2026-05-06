using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using otelturizmnew.Constants;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Api;

[ApiController]
[Route("api/favoriler")]
public class FavoritesApiController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IUserFavoriteService _userFavoriteService;
    private readonly ILogger<FavoritesApiController> _logger;

    public FavoritesApiController(
        IConfiguration configuration,
        IUserFavoriteService userFavoriteService,
        ILogger<FavoritesApiController> logger)
    {
        _configuration = configuration;
        _userFavoriteService = userFavoriteService;
        _logger = logger;
    }

    [HttpPost("toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle([FromBody] HotelFavoriteToggleRequest request, CancellationToken cancellationToken)
    {
        if (request is null || request.HotelId <= 0)
        {
            return BadRequest(new HotelFavoriteToggleResponse
            {
                Success = false,
                Message = "Favori islemi icin gecerli otel bilgisi alinamadi."
            });
        }

        var userId = await GetUserIdAsync(cancellationToken);
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

        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "favori_toggle_basarisiz hotelId={HotelId}", request.HotelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new HotelFavoriteToggleResponse
            {
                Success = false,
                Message = "Favori kaydı şu anda güncellenemedi. Lütfen kısa süre sonra tekrar deneyin."
            });
        }
    }

    private async Task<long> GetUserIdAsync(CancellationToken cancellationToken)
    {
        var raw = User.FindFirstValue(AuthClaimTypes.UserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(raw, out var userId) && userId > 0)
        {
            return userId;
        }

        var email = User.FindFirstValue(AuthClaimTypes.Email)
                    ?? User.FindFirstValue(ClaimTypes.Email)
                    ?? User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return 0;
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return 0;
        }

        const string sql = """
            SELECT TOP (1) id
            FROM users
            WHERE LTRIM(RTRIM(LOWER(eposta))) = LTRIM(RTRIM(LOWER(@email)))
            ORDER BY id ASC;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@email", email.Trim());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null || result == DBNull.Value
            ? 0
            : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }
}
