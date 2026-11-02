using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Parcels;

/// <summary>
/// Lightweight DTO for live client-side quote and route calculation requests.
/// </summary>
public class BookingQuoteRequestDto
{
    public double PickupLat { get; set; }
    public double PickupLon { get; set; }
    public string? PickupAddressText { get; set; }

    public double DeliveryLat { get; set; }
    public double DeliveryLon { get; set; }
    public string? DeliveryAddressText { get; set; }

    public double Weight { get; set; } = 1.0;
    public double Length { get; set; } = 10.0;
    public double Width { get; set; } = 10.0;
    public double Height { get; set; } = 10.0;
    public DeliveryType DeliveryType { get; set; } = DeliveryType.Standard;
}
