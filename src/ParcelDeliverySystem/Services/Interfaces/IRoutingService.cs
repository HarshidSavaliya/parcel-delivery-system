using ParcelDeliverySystem.Models.DTOs.Location;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Service contract for waypoint routing, road distance, duration, and GeoJSON path calculations.
/// </summary>
public interface IRoutingService
{
    Task<RouteCalculationResultDto> CalculateRouteAsync(double fromLat, double fromLon, double toLat, double toLon);
}
