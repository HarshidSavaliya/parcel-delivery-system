using Microsoft.Extensions.Options;
using ParcelDeliverySystem.Configurations;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Services.Implementations;
using Xunit;

namespace ParcelDeliverySystem.Tests.Services;

public class DeliveryChargeServiceTests
{
    private readonly DeliveryChargeService _deliveryChargeService;
    private readonly DeliveryPricingSettings _pricingSettings;

    public DeliveryChargeServiceTests()
    {
        _pricingSettings = new DeliveryPricingSettings
        {
            BaseFare = 5.00m,
            RatePerKm = 0.75m,
            RatePerKg = 1.20m,
            VolumetricDivisor = 5000.0,
            MinimumCharge = 6.00m,
            StandardMultiplier = 1.00m,
            ExpressMultiplier = 1.60m,
            SameDayMultiplier = 2.20m
        };

        var options = Options.Create(_pricingSettings);
        _deliveryChargeService = new DeliveryChargeService(options);
    }

    [Fact]
    public void CalculateCharge_StandardDelivery_CalculatesAccuratePrice()
    {
        // Arrange
        double distanceKm = 10.0;
        double weightKg = 2.0;
        double length = 10.0;
        double width = 10.0;
        double height = 10.0;
        // Volumetric weight: 10*10*10/5000 = 0.2 kg -> Billable weight = 2.0 kg
        // BaseFare (5.00) + Distance (10 * 0.75 = 7.50) + Weight (2.0 * 1.20 = 2.40) = 14.90 * 1.0 = $14.90

        // Act
        var result = _deliveryChargeService.CalculateCharge(distanceKm, weightKg, length, width, height, DeliveryType.Standard);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5.00m, result.BaseFare);
        Assert.Equal(7.50m, result.DistanceCharge);
        Assert.Equal(2.40m, result.WeightCharge);
        Assert.Equal(14.90m, result.TotalCharge);
        Assert.Equal(2.0, result.BillableWeightKg);
    }

    [Theory]
    [InlineData(DeliveryType.Standard, 1.00, 14.90)]
    [InlineData(DeliveryType.Express, 1.60, 23.84)]
    [InlineData(DeliveryType.SameDay, 2.20, 32.78)]
    public void CalculateCharge_DifferentDeliveryTypes_AppliesCorrectMultiplier(
        DeliveryType deliveryType, 
        decimal expectedMultiplier, 
        decimal expectedTotal)
    {
        // Arrange
        double distanceKm = 10.0;
        double weightKg = 2.0;
        double length = 10.0, width = 10.0, height = 10.0;

        // Act
        var result = _deliveryChargeService.CalculateCharge(distanceKm, weightKg, length, width, height, deliveryType);

        // Assert
        Assert.Equal(expectedMultiplier, result.TierMultiplier);
        Assert.Equal(expectedTotal, result.TotalCharge);
    }

    [Fact]
    public void CalculateCharge_WhenVolumetricWeightExceedsActualWeight_UsesVolumetricWeight()
    {
        // Arrange: Large lightweight box: 50cm x 40cm x 30cm = 60,000 cm3 / 5000 = 12 kg
        double actualWeight = 1.0;
        double length = 50.0, width = 40.0, height = 30.0;
        double distanceKm = 10.0;

        // Act
        var result = _deliveryChargeService.CalculateCharge(distanceKm, actualWeight, length, width, height, DeliveryType.Standard);

        // Assert
        Assert.Equal(12.0, result.BillableWeightKg);
        Assert.Equal(12.0, result.VolumetricWeightKg);
        // Base (5.00) + Dist (7.50) + Weight (12.0 * 1.20 = 14.40) = 26.90
        Assert.Equal(26.90m, result.TotalCharge);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    [InlineData(-100)]
    public void CalculateCharge_InvalidWeight_ThrowsArgumentOutOfRangeException(double invalidWeight)
    {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _deliveryChargeService.CalculateCharge(10.0, invalidWeight, 10, 10, 10, DeliveryType.Standard));
    }

    [Theory]
    [InlineData(0, 10, 10)]
    [InlineData(10, 0, 10)]
    [InlineData(10, 10, 0)]
    [InlineData(-5, 10, 10)]
    public void CalculateCharge_InvalidDimensions_ThrowsArgumentOutOfRangeException(double l, double w, double h)
    {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _deliveryChargeService.CalculateCharge(10.0, 2.0, l, w, h, DeliveryType.Standard));
    }

    [Fact]
    public void CalculateCharge_EnforcesMinimumCharge()
    {
        // Arrange: Very short distance, minimal weight
        double distanceKm = 0.1;
        double weightKg = 0.1;

        // Act
        var result = _deliveryChargeService.CalculateCharge(distanceKm, weightKg, 5, 5, 5, DeliveryType.Standard);

        // Assert
        Assert.True(result.TotalCharge >= _pricingSettings.MinimumCharge);
    }
}
