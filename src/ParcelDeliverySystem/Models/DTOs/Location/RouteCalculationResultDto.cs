namespace ParcelDeliverySystem.Models.DTOs.Location;

/// <summary>
/// Route and distance calculation result from Geoapify Routing API or mathematical fallback.
/// </summary>
public class RouteCalculationResultDto
{
    public bool Success { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public string? FormattedDuration { get; set; }
    public string? GeometryGeoJson { get; set; } // GeoJSON string with coordinates for Leaflet polyline
    public string? ErrorMessage { get; set; }
    public bool IsFallback { get; set; }
}
