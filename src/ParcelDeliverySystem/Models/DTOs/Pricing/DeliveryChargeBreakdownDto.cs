using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.Models.DTOs.Pricing;

/// <summary>
/// Detailed breakdown of calculated delivery charges.
/// </summary>
public class DeliveryChargeBreakdownDto
{
    public decimal BaseFare { get; set; }
    public decimal DistanceCharge { get; set; }
    public decimal WeightCharge { get; set; }
    public decimal VolumetricSurcharge { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TierMultiplier { get; set; } = 1.0m;
    public decimal TotalCharge { get; set; }
    public double BillableWeightKg { get; set; }
    public double VolumetricWeightKg { get; set; }
    public double DistanceKm { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public DateTime EstimatedDeliveryTime { get; set; }
    public string EstimatedDeliveryDisplay { get; set; } = string.Empty;
}
