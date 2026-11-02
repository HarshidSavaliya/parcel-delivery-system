using ParcelDeliverySystem.Models.DTOs.Pricing;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Business logic contract for computing dynamic parcel delivery charges and ETAs.
/// </summary>
public interface IDeliveryChargeService
{
    DeliveryChargeBreakdownDto CalculateCharge(
        double distanceKm,
        double weightKg,
        double lengthCm,
        double widthCm,
        double heightCm,
        DeliveryType deliveryType);
}
