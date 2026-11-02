using Microsoft.AspNetCore.Mvc;
using ParcelDeliverySystem.Services.Interfaces;

namespace ParcelDeliverySystem.Controllers;

/// <summary>
/// Secure API Proxy for Geoapify Location Platform services.
/// Keeps the Geoapify API key concealed on the server and provides autocomplete and routing to client-side scripts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly IGeocodingService _geocodingService;
    private readonly IRoutingService _routingService;

    public LocationController(IGeocodingService geocodingService, IRoutingService routingService)
    {
        _geocodingService = geocodingService;
        _routingService = routingService;
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string text)
    {
        var suggestions = await _geocodingService.AutocompleteAddressAsync(text);
        return Ok(suggestions);
    }

    [HttpGet("geocode")]
    public async Task<IActionResult> Geocode([FromQuery] string address)
    {
        var result = await _geocodingService.GeocodeAddressAsync(address);
        if (result == null) return NotFound(new { message = "Address not found." });
        return Ok(result);
    }

    [HttpGet("reverse")]
    public async Task<IActionResult> ReverseGeocode([FromQuery] double lat, [FromQuery] double lon)
    {
        var result = await _geocodingService.ReverseGeocodeAsync(lat, lon);
        if (result == null) return NotFound(new { message = "Unable to reverse geocode specified coordinates." });
        return Ok(result);
    }

    [HttpGet("route")]
    public async Task<IActionResult> CalculateRoute(
        [FromQuery] double fromLat,
        [FromQuery] double fromLon,
        [FromQuery] double toLat,
        [FromQuery] double toLon)
    {
        var route = await _routingService.CalculateRouteAsync(fromLat, fromLon, toLat, toLon);
        return Ok(route);
    }
}
