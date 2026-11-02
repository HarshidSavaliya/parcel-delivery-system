using Moq;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Implementations;
using Xunit;

namespace ParcelDeliverySystem.Tests.Services;

public class TrackingIdGeneratorTests
{
    private readonly Mock<IParcelRepository> _parcelRepoMock;
    private readonly TrackingIdGenerator _generator;

    public TrackingIdGeneratorTests()
    {
        _parcelRepoMock = new Mock<IParcelRepository>();
        _generator = new TrackingIdGenerator(_parcelRepoMock.Object);
    }

    [Fact]
    public async Task GenerateTrackingIdAsync_ReturnsCorrectPrefixAndFormat()
    {
        // Arrange
        _parcelRepoMock.Setup(r => r.GetByTrackingIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Parcel?)null);

        var expectedDatePart = DateTime.UtcNow.ToString("yyyyMMdd");

        // Act
        var trackingId = await _generator.GenerateTrackingIdAsync();

        // Assert
        Assert.NotNull(trackingId);
        Assert.StartsWith($"PDS-{expectedDatePart}-", trackingId);
        Assert.True(trackingId.Length >= 16);
    }

    [Fact]
    public async Task GenerateTrackingIdAsync_WhenCollisionOccurs_GeneratesAlternativeUniqueId()
    {
        // Arrange: first query returns existing parcel, second returns null
        int callCount = 0;
        _parcelRepoMock.Setup(r => r.GetByTrackingIdAsync(It.IsAny<string>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? new Parcel { TrackingId = "PDS-COLLISION" } : null;
            });

        // Act
        var trackingId = await _generator.GenerateTrackingIdAsync();

        // Assert
        Assert.NotNull(trackingId);
        Assert.True(callCount >= 2);
    }
}
