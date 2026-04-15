using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Api;

[ApiController]
[Route("api/pricing")]
public class PricingController : ControllerBase
{
    private readonly IHotelPricingReadService _hotelPricingReadService;

    public PricingController(IHotelPricingReadService hotelPricingReadService)
    {
        _hotelPricingReadService = hotelPricingReadService;
    }

    [HttpGet("hotel-effective")]
    public async Task<IActionResult> GetHotelEffectivePrice(
        [FromQuery] long hotelId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (hotelId <= 0)
        {
            return BadRequest(new { success = false, message = "hotelId zorunludur." });
        }

        var effectivePrice = await _hotelPricingReadService.GetHotelEffectivePriceAsync(hotelId, startDate, endDate, cancellationToken);
        return Ok(new
        {
            success = true,
            hotelId,
            startDate = startDate.ToString("yyyy-MM-dd"),
            endDate = endDate.ToString("yyyy-MM-dd"),
            effectivePrice
        });
    }

    [HttpPost("hotel-effective-batch")]
    public async Task<IActionResult> GetHotelEffectivePricesBatch(
        [FromBody] HotelPricingBatchRequest request,
        CancellationToken cancellationToken)
    {
        var ids = request.HotelIds
            .Where(static id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return BadRequest(new { success = false, message = "HotelIds listesi bos olamaz." });
        }

        var prices = await _hotelPricingReadService.GetHotelEffectivePriceMapAsync(ids, request.StartDate, request.EndDate, cancellationToken);
        var rows = ids.Select(id => new
        {
            hotelId = id,
            effectivePrice = prices.TryGetValue(id, out var price) ? price : (decimal?)null
        });

        return Ok(new
        {
            success = true,
            startDate = request.StartDate.ToString("yyyy-MM-dd"),
            endDate = request.EndDate.ToString("yyyy-MM-dd"),
            items = rows
        });
    }
}

public sealed class HotelPricingBatchRequest
{
    public List<long> HotelIds { get; set; } = new();
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
