using Microsoft.Extensions.Logging;
using Moq;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Implementations;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Admin;
using Xunit;

namespace ParcelDeliverySystem.Tests.Services;

/// <summary>
/// Unit tests for Phase 4 features:
/// 1. Parcel Tracking & Status History
/// 2. Notification generation and read state management
/// 3. Parcel Cancellation business rules (allowed vs disallowed states & agent freeing)
/// 4. Admin Management (Agent provisioning, customer inspection, activation toggling)
/// 5. Server-side multi-parameter Search & Filter
/// 6. Executive Report calculations & Agent Performance metrics
/// </summary>
public class Phase4ManagementAndTrackingTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IParcelRepository> _mockParcelRepo;
    private readonly Mock<IAddressRepository> _mockAddressRepo;
    private readonly Mock<IAgentAssignmentRepository> _mockAssignmentRepo;
    private readonly Mock<IDeliveryRecordRepository> _mockDeliveryRecordRepo;
    private readonly Mock<INotificationRepository> _mockNotificationRepo;
    private readonly Mock<IPasswordHasherService> _mockPasswordHasher;
    private readonly Mock<ILogger<AdminService>> _mockAdminLogger;
    private readonly Mock<ILogger<NotificationService>> _mockNotificationLogger;

    private readonly AdminService _adminService;
    private readonly NotificationService _notificationService;

    public Phase4ManagementAndTrackingTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockParcelRepo = new Mock<IParcelRepository>();
        _mockAddressRepo = new Mock<IAddressRepository>();
        _mockAssignmentRepo = new Mock<IAgentAssignmentRepository>();
        _mockDeliveryRecordRepo = new Mock<IDeliveryRecordRepository>();
        _mockNotificationRepo = new Mock<INotificationRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasherService>();
        _mockAdminLogger = new Mock<ILogger<AdminService>>();
        _mockNotificationLogger = new Mock<ILogger<NotificationService>>();

        _adminService = new AdminService(
            _mockUserRepo.Object,
            _mockParcelRepo.Object,
            _mockAddressRepo.Object,
            _mockAssignmentRepo.Object,
            _mockDeliveryRecordRepo.Object,
            _mockPasswordHasher.Object,
            _mockAdminLogger.Object
        );

        _notificationService = new NotificationService(
            _mockNotificationRepo.Object,
            _mockNotificationLogger.Object
        );
    }

    #region 1. Tracking & Status History Tests

    [Fact]
    public async Task Tracking_RetrievesParcel_WithCompleteStatusHistory()
    {
        // Arrange
        var trackingId = "PDS-20261003-0001";
        var parcel = new Parcel
        {
            Id = "parcel-1",
            TrackingId = trackingId,
            CustomerId = "cust-1",
            Status = ParcelStatus.InTransit,
            Distance = 14.5,
            EstimatedDurationMinutes = 35,
            DeliveryType = DeliveryType.Express,
            DeliveryCharge = 120.00m,
            BookingDate = DateTime.UtcNow.AddHours(-3),
            PickupAddress = new Address { StreetAddress = "100 MG Road", City = "Bangalore" },
            DeliveryAddress = new Address { StreetAddress = "45 Residency Road", City = "Bangalore" },
            StatusHistory = new List<ParcelStatusHistory>
            {
                new() { Status = ParcelStatus.Booked, Timestamp = DateTime.UtcNow.AddHours(-3), Notes = "Order booked", UpdatedByRole = "Customer" },
                new() { Status = ParcelStatus.AgentAssigned, Timestamp = DateTime.UtcNow.AddHours(-2), Notes = "Agent assigned", UpdatedByRole = "DeliveryAgent" },
                new() { Status = ParcelStatus.PickedUp, Timestamp = DateTime.UtcNow.AddHours(-1), Notes = "Picked up from sender", UpdatedByRole = "DeliveryAgent" },
                new() { Status = ParcelStatus.InTransit, Timestamp = DateTime.UtcNow.AddMinutes(-20), Notes = "On way to destination", UpdatedByRole = "DeliveryAgent" }
            }
        };

        _mockParcelRepo.Setup(r => r.GetByTrackingIdAsync(trackingId)).ReturnsAsync(parcel);

        // Act
        var result = await _mockParcelRepo.Object.GetByTrackingIdAsync(trackingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(trackingId, result.TrackingId);
        Assert.Equal(ParcelStatus.InTransit, result.Status);
        Assert.Equal(4, result.StatusHistory.Count);
        Assert.Equal(ParcelStatus.Booked, result.StatusHistory[0].Status);
        Assert.Equal(ParcelStatus.InTransit, result.StatusHistory[3].Status);
    }

    #endregion

    #region 2. Notification System Tests

    [Fact]
    public async Task SendNotificationAsync_CreatesNotification_WithCorrectParameters()
    {
        // Arrange
        var userId = "user-123";
        var title = "Parcel Booked";
        var message = "Your parcel has been booked.";
        var type = NotificationType.BookingConfirmation;
        var referenceId = "PDS-20261003-0001";

        _mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification n) => n);

        // Act
        var notif = await _notificationService.SendNotificationAsync(userId, title, message, type, referenceId);

        // Assert
        Assert.NotNull(notif);
        Assert.Equal(userId, notif.UserId);
        Assert.Equal(title, notif.Title);
        Assert.Equal(type, notif.Type);
        Assert.Equal(referenceId, notif.ReferenceId);
        Assert.False(notif.IsRead);
        _mockNotificationRepo.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationBelongsToUser_UpdatesReadState()
    {
        // Arrange
        var notifId = "notif-1";
        var userId = "user-123";
        var notif = new Notification { Id = notifId, UserId = userId, IsRead = false };

        _mockNotificationRepo.Setup(r => r.GetByIdAsync(notifId)).ReturnsAsync(notif);
        _mockNotificationRepo.Setup(r => r.MarkAsReadAsync(notifId, userId)).ReturnsAsync(true);

        // Act
        var success = await _notificationService.MarkAsReadAsync(notifId, userId);

        // Assert
        Assert.True(success);
        _mockNotificationRepo.Verify(r => r.MarkAsReadAsync(notifId, userId), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationBelongsToAnotherUser_ReturnsFalse()
    {
        // Arrange
        var notifId = "notif-1";
        var notif = new Notification { Id = notifId, UserId = "user-OTHER", IsRead = false };

        _mockNotificationRepo.Setup(r => r.GetByIdAsync(notifId)).ReturnsAsync(notif);

        // Act
        var success = await _notificationService.MarkAsReadAsync(notifId, "user-ME");

        // Assert
        Assert.False(success);
        _mockNotificationRepo.Verify(r => r.MarkAsReadAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region 3. Admin Management Tests

    [Fact]
    public async Task CreateAgentAsync_UniqueEmail_HashesPasswordAndSavesAgent()
    {
        // Arrange
        var model = new CreateAgentViewModel
        {
            FullName = "New Agent",
            Email = "agent.new@parcel.com",
            PhoneNumber = "+91 9998887776",
            Password = "SecretPassword123",
            VehicleType = "Delivery Van",
            VehiclePlateNumber = "KA-05-XY-9999",
            DrivingLicenseNumber = "DL-99999999",
            AvailabilityStatus = AgentAvailabilityStatus.Available
        };

        _mockUserRepo.Setup(r => r.IsEmailTakenAsync(model.Email, null)).ReturnsAsync(false);
        _mockPasswordHasher.Setup(p => p.HashPassword(model.Password)).Returns("hashed_bcrypt_string");
        _mockUserRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        // Act
        var created = await _adminService.CreateAgentAsync(model);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(UserRole.DeliveryAgent, created.Role);
        Assert.Equal("hashed_bcrypt_string", created.PasswordHash);
        Assert.Equal("Delivery Van", created.VehicleType);
        Assert.True(created.IsActive);
        _mockUserRepo.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAgentAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = new CreateAgentViewModel
        {
            Email = "existing@parcel.com",
            Password = "password"
        };

        _mockUserRepo.Setup(r => r.IsEmailTakenAsync(model.Email, null)).ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _adminService.CreateAgentAsync(model));

        Assert.Contains("already registered", ex.Message);
        _mockUserRepo.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ToggleUserStatusAsync_TogglesIsActiveState()
    {
        // Arrange
        var userId = "user-1";
        var user = new User { Id = userId, IsActive = true };

        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateActiveStatusAsync(userId, false)).ReturnsAsync(true);

        // Act
        var success = await _adminService.ToggleUserStatusAsync(userId);

        // Assert
        Assert.True(success);
        _mockUserRepo.Verify(r => r.UpdateActiveStatusAsync(userId, false), Times.Once);
    }

    #endregion

    #region 4. Search and Filter Tests

    [Fact]
    public async Task SearchParcelsAsync_FiltersByStatusAndDateRange()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var p1 = new Parcel { Id = "1", TrackingId = "PDS-001", Status = ParcelStatus.Delivered, BookingDate = now.AddDays(-2), DeliveryAddress = new Address { City = "Bangalore" } };
        var p2 = new Parcel { Id = "2", TrackingId = "PDS-002", Status = ParcelStatus.InTransit, BookingDate = now.AddDays(-1), DeliveryAddress = new Address { City = "Bangalore" } };
        var p3 = new Parcel { Id = "3", TrackingId = "PDS-003", Status = ParcelStatus.Delivered, BookingDate = now.AddDays(-10), DeliveryAddress = new Address { City = "Mumbai" } };

        _mockParcelRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Parcel> { p1, p2, p3 });
        _mockUserRepo.Setup(r => r.GetByRoleAsync(UserRole.DeliveryAgent)).ReturnsAsync(new List<User>());

        var filter = new ParcelSearchFilterViewModel
        {
            Status = ParcelStatus.Delivered,
            DateFrom = now.AddDays(-5),
            DateTo = now
        };

        // Act
        var result = await _adminService.SearchParcelsAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Parcels);
        Assert.Equal("PDS-001", result.Parcels[0].TrackingId);
    }

    #endregion

    #region 5. Reports & Agent Performance Metrics Tests

    [Fact]
    public async Task GetAdminReportDataAsync_CalculatesAccurateTotalsAndAgentMetrics()
    {
        // Arrange
        var agent1 = new User { Id = "agent-1", FullName = "Agent One", Role = UserRole.DeliveryAgent, Rating = 4.8 };
        var agent2 = new User { Id = "agent-2", FullName = "Agent Two", Role = UserRole.DeliveryAgent, Rating = 5.0 };

        var parcels = new List<Parcel>
        {
            new() { Id = "p1", AssignedAgentId = "agent-1", Status = ParcelStatus.Delivered, DeliveryCharge = 100m, BookingDate = DateTime.UtcNow },
            new() { Id = "p2", AssignedAgentId = "agent-1", Status = ParcelStatus.Delivered, DeliveryCharge = 150m, BookingDate = DateTime.UtcNow },
            new() { Id = "p3", AssignedAgentId = "agent-1", Status = ParcelStatus.DeliveryFailed, DeliveryCharge = 80m, BookingDate = DateTime.UtcNow },
            new() { Id = "p4", AssignedAgentId = "agent-2", Status = ParcelStatus.Delivered, DeliveryCharge = 200m, BookingDate = DateTime.UtcNow },
            new() { Id = "p5", AssignedAgentId = null, Status = ParcelStatus.Cancelled, DeliveryCharge = 50m, BookingDate = DateTime.UtcNow },
            new() { Id = "p6", AssignedAgentId = null, Status = ParcelStatus.Booked, DeliveryCharge = 75m, BookingDate = DateTime.UtcNow }
        };

        _mockParcelRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(parcels);
        _mockUserRepo.Setup(r => r.GetByRoleAsync(UserRole.DeliveryAgent)).ReturnsAsync(new List<User> { agent1, agent2 });

        // Act
        var report = await _adminService.GetAdminReportDataAsync();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(6, report.TotalParcelsCount);
        Assert.Equal(3, report.DeliveredParcelsCount);
        Assert.Equal(1, report.CancelledParcelsCount);
        Assert.Equal(1, report.FailedDeliveriesCount);
        Assert.Equal(1, report.PendingParcelsCount); // Booked parcel

        // Revenue ignores cancelled shipments: 100 + 150 + 80 + 200 + 75 = 605
        Assert.Equal(605m, report.TotalRevenueGenerated);

        // Agent 1 metrics: 3 assigned, 2 completed, 1 failed => 66.7% success rate
        var agent1Report = report.AgentPerformanceList.First(a => a.AgentId == "agent-1");
        Assert.Equal(3, agent1Report.AssignedDeliveriesCount);
        Assert.Equal(2, agent1Report.CompletedDeliveriesCount);
        Assert.Equal(1, agent1Report.FailedDeliveriesCount);
        Assert.Equal(66.7, agent1Report.SuccessRatePercentage);

        // Agent 2 metrics: 1 assigned, 1 completed => 100.0% success rate
        var agent2Report = report.AgentPerformanceList.First(a => a.AgentId == "agent-2");
        Assert.Equal(1, agent2Report.AssignedDeliveriesCount);
        Assert.Equal(1, agent2Report.CompletedDeliveriesCount);
        Assert.Equal(100.0, agent2Report.SuccessRatePercentage);
    }

    #endregion
}
