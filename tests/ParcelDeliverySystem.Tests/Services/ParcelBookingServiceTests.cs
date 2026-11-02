using Microsoft.Extensions.Logging;
using Moq;
using ParcelDeliverySystem.Models.DTOs.Location;
using ParcelDeliverySystem.Models.DTOs.Pricing;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Implementations;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Parcels;
using Xunit;

namespace ParcelDeliverySystem.Tests.Services;

public class ParcelBookingServiceTests
{
    private readonly Mock<IParcelRepository> _parcelRepoMock;
    private readonly Mock<IAddressRepository> _addressRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<INotificationRepository> _notifRepoMock;
    private readonly Mock<ITrackingIdGenerator> _trackingIdGenMock;
    private readonly Mock<IDeliveryChargeService> _chargeServiceMock;
    private readonly Mock<IRoutingService> _routingServiceMock;
    private readonly Mock<IGeocodingService> _geocodingServiceMock;
    private readonly Mock<ILogger<ParcelBookingService>> _loggerMock;
    private readonly ParcelBookingService _bookingService;

    public ParcelBookingServiceTests()
    {
        _parcelRepoMock = new Mock<IParcelRepository>();
        _addressRepoMock = new Mock<IAddressRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _notifRepoMock = new Mock<INotificationRepository>();
        _trackingIdGenMock = new Mock<ITrackingIdGenerator>();
        _chargeServiceMock = new Mock<IDeliveryChargeService>();
        _routingServiceMock = new Mock<IRoutingService>();
        _geocodingServiceMock = new Mock<IGeocodingService>();
        _loggerMock = new Mock<ILogger<ParcelBookingService>>();

        _bookingService = new ParcelBookingService(
            _parcelRepoMock.Object,
            _addressRepoMock.Object,
            _userRepoMock.Object,
            _notifRepoMock.Object,
            _trackingIdGenMock.Object,
            _chargeServiceMock.Object,
            _routingServiceMock.Object,
            _geocodingServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task BookParcelAsync_ValidModel_CreatesParcelWithBookedStatusAndInitialHistory()
    {
        // Arrange
        var customerId = "cust-123";
        var customer = new User { Id = customerId, FullName = "Alice", Email = "alice@example.com", IsActive = true };

        var savedPickup = new Address
        {
            Id = "addr-pickup-1",
            UserId = customerId,
            RecipientName = "Alice",
            FormattedAddress = "742 Evergreen Terrace, Springfield, OR",
            Latitude = 44.0462,
            Longitude = -123.0220
        };

        var model = new BookParcelViewModel
        {
            PickupMode = "saved",
            SelectedPickupAddressId = "addr-pickup-1",
            DeliveryMode = "new",
            RecipientName = "Bob Smith",
            RecipientPhone = "+15559876",
            NewDeliveryStreet = "350 5th Ave",
            NewDeliveryCity = "New York",
            NewDeliveryState = "NY",
            NewDeliveryPostalCode = "10118",
            NewDeliveryLatitude = 40.7484,
            NewDeliveryLongitude = -73.9857,
            ParcelType = "Medium Box",
            Weight = 3.5,
            Length = 20,
            Width = 20,
            Height = 15,
            DeliveryType = DeliveryType.Express
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _addressRepoMock.Setup(r => r.GetByIdAsync("addr-pickup-1")).ReturnsAsync(savedPickup);
        _trackingIdGenMock.Setup(g => g.GenerateTrackingIdAsync()).ReturnsAsync("PDS-20261003-0001");

        _routingServiceMock.Setup(r => r.CalculateRouteAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(new RouteCalculationResultDto { Success = true, DistanceKm = 450.0, DurationMinutes = 320 });

        _chargeServiceMock.Setup(c => c.CalculateCharge(450.0, 3.5, 20, 20, 15, DeliveryType.Express))
            .Returns(new DeliveryChargeBreakdownDto
            {
                TotalCharge = 75.50m,
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(24),
                EstimatedDeliveryDisplay = "Express (1 Business Day)"
            });

        _parcelRepoMock.Setup(p => p.CreateAsync(It.IsAny<Parcel>()))
            .ReturnsAsync((Parcel p) => { p.Id = "parcel-new-1"; return p; });

        // Act
        var created = await _bookingService.BookParcelAsync(model, customerId);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("PDS-20261003-0001", created.TrackingId);
        Assert.Equal(customerId, created.CustomerId);
        Assert.Equal(ParcelStatus.Booked, created.Status);
        Assert.Equal(75.50m, created.DeliveryCharge);
        Assert.Single(created.StatusHistory);
        Assert.Equal(ParcelStatus.Booked, created.StatusHistory[0].Status);

        _parcelRepoMock.Verify(p => p.CreateAsync(It.IsAny<Parcel>()), Times.Once);
        _notifRepoMock.Verify(n => n.CreateAsync(It.IsAny<Notification>()), Times.Once);
    }

    [Fact]
    public async Task GetParcelDetailsAsync_WhenRequestedByOtherCustomer_ReturnsNullForSecurity()
    {
        // Arrange
        var parcel = new Parcel
        {
            Id = "p100",
            TrackingId = "PDS-20261003-9999",
            CustomerId = "owner-customer-1",
            Status = ParcelStatus.InTransit
        };

        _parcelRepoMock.Setup(r => r.GetByTrackingIdAsync("PDS-20261003-9999")).ReturnsAsync(parcel);

        // Act: Another customer tries to view
        var result = await _bookingService.GetParcelDetailsAsync("PDS-20261003-9999", "intruder-customer-2", isAdmin: false);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetParcelDetailsAsync_WhenRequestedByAdmin_AllowsAccess()
    {
        // Arrange
        var parcel = new Parcel
        {
            Id = "p100",
            TrackingId = "PDS-20261003-9999",
            CustomerId = "owner-customer-1",
            Status = ParcelStatus.InTransit
        };

        _parcelRepoMock.Setup(r => r.GetByTrackingIdAsync("PDS-20261003-9999")).ReturnsAsync(parcel);

        // Act: Admin requests access
        var result = await _bookingService.GetParcelDetailsAsync("PDS-20261003-9999", "admin-user-id", isAdmin: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PDS-20261003-9999", result.TrackingId);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenParcelIsInTransit_ReturnsFalse()
    {
        // Arrange
        var customerId = "cust-1";
        var parcel = new Parcel
        {
            Id = "p1",
            TrackingId = "PDS-20261003-0002",
            CustomerId = customerId,
            Status = ParcelStatus.InTransit
        };

        _parcelRepoMock.Setup(r => r.GetByTrackingIdAsync("PDS-20261003-0002")).ReturnsAsync(parcel);

        // Act
        var success = await _bookingService.CancelBookingAsync("PDS-20261003-0002", customerId);

        // Assert
        Assert.False(success);
        _parcelRepoMock.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<ParcelStatus>(), It.IsAny<ParcelStatusHistory>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenParcelIsBooked_CancelsSuccessfully()
    {
        // Arrange
        var customerId = "cust-1";
        var parcel = new Parcel
        {
            Id = "p1",
            TrackingId = "PDS-20261003-0003",
            CustomerId = customerId,
            Status = ParcelStatus.Booked
        };

        _parcelRepoMock.Setup(r => r.GetByTrackingIdAsync("PDS-20261003-0003")).ReturnsAsync(parcel);
        _parcelRepoMock.Setup(r => r.UpdateStatusAsync("p1", ParcelStatus.Cancelled, It.IsAny<ParcelStatusHistory>())).ReturnsAsync(true);

        // Act
        var success = await _bookingService.CancelBookingAsync("PDS-20261003-0003", customerId);

        // Assert
        Assert.True(success);
        _parcelRepoMock.Verify(r => r.UpdateStatusAsync("p1", ParcelStatus.Cancelled, It.IsAny<ParcelStatusHistory>()), Times.Once);
    }
}
