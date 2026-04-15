using Microsoft.AspNetCore.Mvc;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Controllers.Api;

[ApiController]
[Route("api/adres")]
public class AddressLookupController : ControllerBase
{
    private readonly IAddressLookupService _addressLookupService;

    public AddressLookupController(IAddressLookupService addressLookupService)
    {
        _addressLookupService = addressLookupService;
    }

    [HttpGet("ulkeler")]
    public async Task<IActionResult> Countries(CancellationToken cancellationToken)
    {
        var items = await _addressLookupService.GetCountriesAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("iller")]
    public async Task<IActionResult> Provinces(CancellationToken cancellationToken)
    {
        var items = await _addressLookupService.GetProvincesAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("ilceler")]
    public async Task<IActionResult> Districts([FromQuery] long ilId, CancellationToken cancellationToken)
    {
        var items = await _addressLookupService.GetDistrictsAsync(ilId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("mahalleler")]
    public async Task<IActionResult> Neighborhoods([FromQuery] long ilceId, CancellationToken cancellationToken)
    {
        var items = await _addressLookupService.GetNeighborhoodsAsync(ilceId, cancellationToken);
        return Ok(items);
    }
}
