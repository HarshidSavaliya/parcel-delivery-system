using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Agent;

/// <summary>
/// View model representing an available parcel pickup request sorted by distance from agent.
/// </summary>
public class AvailableRequestViewModel
{
    public string ParcelId { get; set; } = string.Empty;
    public string TrackingId { get; set; } = string.Empty;

    public Address PickupAddress { get; set; } = new();
    public Address DeliveryAddress { get; set; } = new();

    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }
    public double DeliveryLatitude { get; set; }
    public double DeliveryLongitude { get; set; }

    public double DistanceFromAgentKm { get; set; }
    public int EstimatedTravelTimeMinutes { get; set; }

    public string ParcelType { get; set; } = "Standard Box";
    public double WeightKg { get; set; }
    public double Length { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public DeliveryType DeliveryType { get; set; } = DeliveryType.Standard;
    public decimal DeliveryCharge { get; set; }
    public DateTime BookingTime { get; set; }
    public string? SpecialInstructions { get; set; }

    public string FormattedDistance => $"{DistanceFromAgentKm:F1} km";
    public string FormattedTravelTime => EstimatedTravelTimeMinutes > 60
        ? $"{EstimatedTravelTimeMinutes / 60}h {EstimatedTravelTimeMinutes % 60}m"
        : $"{EstimatedTravelTimeMinutes} mins";
}
