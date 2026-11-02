namespace ParcelDeliverySystem.Configurations;

/// <summary>
/// Strongly typed pricing rules and rate configurations for parcel delivery computations.
/// Binds to the "DeliveryPricing" section of appsettings.json.
/// </summary>
public class DeliveryPricingSettings
{
    public const string SectionName = "DeliveryPricing";

    public decimal BaseFare { get; set; } = 5.00m;
    public decimal RatePerKm { get; set; } = 0.75m;
    public decimal RatePerKg { get; set; } = 1.20m;
    public double VolumetricDivisor { get; set; } = 5000.0; // cm^3 per kg
    public decimal MinimumCharge { get; set; } = 6.00m;

    public decimal StandardMultiplier { get; set; } = 1.00m;
    public decimal ExpressMultiplier { get; set; } = 1.60m;
    public decimal SameDayMultiplier { get; set; } = 2.20m;
}
