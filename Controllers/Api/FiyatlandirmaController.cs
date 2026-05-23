using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Api;

[ApiController]
[Route("api/pricing")]
[IgnoreAntiforgeryToken]
[EnableRateLimiting("quote-strict")]
public class FiyatlandirmaController : ControllerBase
{
    private const int MaxBatchHotels = 150;
    private const int MaxDateRangeDays = 400;

    private readonly IHotelPricingReadService _hotelPricingReadService;

    public FiyatlandirmaController(IHotelPricingReadService hotelPricingReadService)
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

        if (!ValidateDateRange(startDate, endDate, out var rangeError))
        {
            return BadRequest(new { success = false, message = rangeError });
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
        [FromBody] OtelFiyatTopluIstek request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { success = false, message = "İstek gövdesi gerekli." });
        }

        if (!ValidateDateRange(request.StartDate, request.EndDate, out var rangeError))
        {
            return BadRequest(new { success = false, message = rangeError });
        }

        var ids = (request.HotelIds ?? new List<long>())
            .Where(static id => id > 0)
            .Distinct()
            .Take(MaxBatchHotels)
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

    private static bool ValidateDateRange(DateOnly start, DateOnly end, out string? error)
    {
        if (end < start)
        {
            error = "Bitiş tarihi başlangıçtan önce olamaz.";
            return false;
        }

        var days = end.DayNumber - start.DayNumber;
        if (days > MaxDateRangeDays)
        {
            error = $"Tarih aralığı en fazla {MaxDateRangeDays} gün olabilir.";
            return false;
        }

        error = null;
        return true;
    }
}

public sealed class OtelFiyatTopluIstek
{
    public List<long> HotelIds { get; set; } = new();
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
