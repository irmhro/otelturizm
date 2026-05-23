using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Api;

[ApiController]
[Route("api/oteller")]
public class OtelAramaApiController : ControllerBase
{
    private readonly IHotelService _hotelService;

    public OtelAramaApiController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    [HttpGet("arama-onerileri")]
    public async Task<IActionResult> SearchSuggestions([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var suggestions = await _hotelService.GetSearchSuggestionsAsync(q ?? string.Empty, cancellationToken);
        return Ok(suggestions);
    }
}
