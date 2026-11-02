using Microsoft.Extensions.Options;
using ParcelDeliverySystem.Configurations;
using ParcelDeliverySystem.Models.DTOs.Pricing;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Services.Interfaces;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Implements transparent, extensible parcel delivery pricing and ETA estimation.
/// Formulas are driven by strongly-typed configuration settings.
/// </summary>
public class DeliveryChargeService : IDeliveryChargeService
{
    private readonly DeliveryPricingSettings _pricing;

    public DeliveryChargeService(IOptions<DeliveryPricingSettings> pricingOptions)
    {
        _pricing = pricingOptions.Value;
    }

    public DeliveryChargeBreakdownDto CalculateCharge(
        double distanceKm,
        double weightKg,
        double lengthCm,
        double widthCm,
        double heightCm,
        DeliveryType deliveryType)
    {
        if (weightKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Parcel weight must be greater than zero.");

        if (lengthCm <= 0 || widthCm <= 0 || heightCm <= 0)
            throw new ArgumentOutOfRangeException("Dimensions", "Parcel dimensions (length, width, height) must all be greater than zero.");

        if (distanceKm < 0)
            throw new ArgumentOutOfRangeException(nameof(distanceKm), "Distance cannot be negative.");

        // Calculate volumetric weight: (L * W * H) / VolumetricDivisor
        var volumeCm3 = lengthCm * widthCm * heightCm;
        var volumetricWeightKg = Math.Round(volumeCm3 / _pricing.VolumetricDivisor, 2);

        // Billable weight is the greater of actual scale weight or dimensional weight
        var billableWeight = Math.Max(weightKg, volumetricWeightKg);

        // Base price elements
        var baseFare = _pricing.BaseFare;
        var distanceCharge = Math.Round((decimal)distanceKm * _pricing.RatePerKm, 2);
        var weightCharge = Math.Round((decimal)billableWeight * _pricing.RatePerKg, 2);

        var subtotal = baseFare + distanceCharge + weightCharge;

        // Apply service level tier multiplier
        var multiplier = deliveryType switch
        {
            DeliveryType.Express => _pricing.ExpressMultiplier,
            DeliveryType.SameDay => _pricing.SameDayMultiplier,
            _ => _pricing.StandardMultiplier
        };

        var rawTotal = Math.Round(subtotal * multiplier, 2);
        var totalCharge = Math.Max(_pricing.MinimumCharge, rawTotal);

        // Calculate Estimated Delivery Time
        var (etaTime, etaDisplay) = CalculateEta(deliveryType, distanceKm);

        return new DeliveryChargeBreakdownDto
        {
            BaseFare = baseFare,
            DistanceCharge = distanceCharge,
            WeightCharge = weightCharge,
            VolumetricSurcharge = volumetricWeightKg > weightKg ? Math.Round((decimal)(volumetricWeightKg - weightKg) * _pricing.RatePerKg, 2) : 0,
            Subtotal = subtotal,
            TierMultiplier = multiplier,
            TotalCharge = totalCharge,
            BillableWeightKg = billableWeight,
            VolumetricWeightKg = volumetricWeightKg,
            DistanceKm = Math.Round(distanceKm, 2),
            DeliveryType = deliveryType,
            EstimatedDeliveryTime = etaTime,
            EstimatedDeliveryDisplay = etaDisplay
        };
    }

    private static (DateTime EtaTime, string Display) CalculateEta(DeliveryType deliveryType, double distanceKm)
    {
        var now = DateTime.UtcNow;

        switch (deliveryType)
        {
            case DeliveryType.SameDay:
                var sameDayEta = now.AddHours(Math.Min(8, Math.Max(3, distanceKm / 20)));
                return (sameDayEta, "Same Day (within " + Math.Round(Math.Max(3, distanceKm / 20)) + " hours)");

            case DeliveryType.Express:
                var expressDays = distanceKm > 500 ? 2 : 1;
                var expressEta = now.AddDays(expressDays).Date.AddHours(17); // By 5:00 PM next day
                return (expressEta, $"Express ({expressDays} Business Day{(expressDays > 1 ? "s" : "")})");

            case DeliveryType.Standard:
            default:
                var standardDays = distanceKm > 1000 ? 5 : distanceKm > 300 ? 3 : 2;
                var standardEta = now.AddDays(standardDays).Date.AddHours(18); // By 6:00 PM
                return (standardEta, $"Standard ({standardDays} Business Days)");
        }
    }
}
