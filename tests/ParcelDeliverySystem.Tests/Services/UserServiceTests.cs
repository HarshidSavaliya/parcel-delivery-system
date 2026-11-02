using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Implementations;
using Xunit;

namespace ParcelDeliverySystem.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IAddressRepository> _addressRepoMock;
    private readonly Mock<IParcelRepository> _parcelRepoMock;
    private readonly Mock<INotificationRepository> _notifRepoMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _addressRepoMock = new Mock<IAddressRepository>();
        _parcelRepoMock = new Mock<IParcelRepository>();
        _notifRepoMock = new Mock<INotificationRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();

        _userService = new UserService(
            _userRepoMock.Object,
            _addressRepoMock.Object,
            _parcelRepoMock.Object,
            _notifRepoMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task UpdateAgentAvailabilityAsync_CallsRepositoryWithCorrectStatus()
    {
        // Arrange
        var agentId = "507f1f77bcf86cd799439020";
        var status = AgentAvailabilityStatus.Available;

        _userRepoMock.Setup(r => r.UpdateAvailabilityStatusAsync(agentId, status))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.UpdateAgentAvailabilityAsync(agentId, status);

        // Assert
        result.Should().BeTrue();
        _userRepoMock.Verify(r => r.UpdateAvailabilityStatusAsync(agentId, status), Times.Once);
    }

    [Fact]
    public async Task GetCustomerDashboardDataAsync_AggregatesCountsCorrectly()
    {
        // Arrange
        var customerId = "507f1f77bcf86cd799439021";
        var customer = new User { Id = customerId, FullName = "Alice Customer" };
        var parcels = new List<Parcel>
        {
            new() { Id = "p1", CustomerId = customerId, Status = ParcelStatus.PendingPickup },
            new() { Id = "p2", CustomerId = customerId, Status = ParcelStatus.InTransit },
            new() { Id = "p3", CustomerId = customerId, Status = ParcelStatus.Delivered }
        };
        var addresses = new List<Address>
        {
            new() { Id = "a1", UserId = customerId, Label = "Home" }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _parcelRepoMock.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync(parcels);
        _addressRepoMock.Setup(r => r.GetByUserIdAsync(customerId)).ReturnsAsync(addresses);
        _notifRepoMock.Setup(r => r.GetByUserIdAsync(customerId, 5)).ReturnsAsync(new List<Notification>());
        _notifRepoMock.Setup(r => r.GetUnreadCountAsync(customerId)).ReturnsAsync(2);

        // Act
        var dashboard = await _userService.GetCustomerDashboardDataAsync(customerId);

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.Customer.FullName.Should().Be("Alice Customer");
        dashboard.ActiveBookingsCount.Should().Be(2); // PendingPickup + InTransit
        dashboard.DeliveredParcelsCount.Should().Be(1);
        dashboard.SavedAddressesCount.Should().Be(1);
        dashboard.UnreadNotificationsCount.Should().Be(2);
    }

    [Fact]
    public async Task ToggleUserActiveStatusAsync_InvertsCurrentActiveState()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439022";
        var activeUser = new User { Id = userId, IsActive = true };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(activeUser);
        _userRepoMock.Setup(r => r.UpdateActiveStatusAsync(userId, false)).ReturnsAsync(true);

        // Act
        var result = await _userService.ToggleUserActiveStatusAsync(userId);

        // Assert
        result.Should().BeTrue();
        _userRepoMock.Verify(r => r.UpdateActiveStatusAsync(userId, false), Times.Once);
    }
}
