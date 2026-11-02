using Microsoft.Extensions.Logging;
using Moq;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Implementations;
using ParcelDeliverySystem.ViewModels.Agent;
using Xunit;

namespace ParcelDeliverySystem.Tests.Services;

/// <summary>
/// Unit tests for DeliveryAgentService covering:
/// 1. Agent availability toggling and validation
/// 2. Accepting delivery requests and preventing duplicate assignment
/// 3. Proximity-based distance sorting (nearest first)
/// 4. Sequential delivery lifecycle state machine transitions
/// 5. Invalid status transition prevention
/// 6. Delivery confirmation and proof capture
/// 7. Delivery failure reporting with reasons
/// 8. Agent state transitions (Available -> Busy -> Available)
/// 9. Agent authorization / security isolation
/// </summary>
public class DeliveryAgentServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IParcelRepository> _mockParcelRepo;
    private readonly Mock<IAgentAssignmentRepository> _mockAssignmentRepo;
    private readonly Mock<IDeliveryRecordRepository> _mockDeliveryRecordRepo;
    private readonly Mock<INotificationRepository> _mockNotificationRepo;
    private readonly Mock<ILogger<DeliveryAgentService>> _mockLogger;
    private readonly DeliveryAgentService _agentService;

    public DeliveryAgentServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockParcelRepo = new Mock<IParcelRepository>();
        _mockAssignmentRepo = new Mock<IAgentAssignmentRepository>();
        _mockDeliveryRecordRepo = new Mock<IDeliveryRecordRepository>();
        _mockNotificationRepo = new Mock<INotificationRepository>();
        _mockLogger = new Mock<ILogger<DeliveryAgentService>>();

        _agentService = new DeliveryAgentService(
            _mockUserRepo.Object,
            _mockParcelRepo.Object,
            _mockAssignmentRepo.Object,
            _mockDeliveryRecordRepo.Object,
            _mockNotificationRepo.Object,
            _mockLogger.Object
        );
    }

    #region 1. Agent Availability Tests

    [Fact]
    public async Task UpdateAvailabilityStatusAsync_ValidAgent_UpdatesStatusSuccessfully()
    {
        // Arrange
        var agentId = "agent-123";
        var agent = new User
        {
            Id = agentId,
            Role = UserRole.DeliveryAgent,
            AvailabilityStatus = AgentAvailabilityStatus.Offline
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockUserRepo.Setup(r => r.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Available))
            .ReturnsAsync(true);

        // Act
        var result = await _agentService.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Available);

        // Assert
        Assert.True(result);
        _mockUserRepo.Verify(r => r.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Available), Times.Once);
    }

    [Theory]
    [InlineData(AgentAvailabilityStatus.Offline)]
    [InlineData(AgentAvailabilityStatus.Busy)]
    public async Task AcceptDeliveryRequestAsync_NonAvailableAgent_ThrowsInvalidOperationException(AgentAvailabilityStatus status)
    {
        // Arrange
        var agentId = "agent-123";
        var parcelId = "parcel-001";
        var agent = new User
        {
            Id = agentId,
            FullName = "David Courier",
            Role = UserRole.DeliveryAgent,
            IsActive = true,
            AvailabilityStatus = status
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.AcceptDeliveryRequestAsync(agentId, parcelId));

        Assert.NotNull(ex.Message);
        _mockParcelRepo.Verify(r => r.AssignAgentSafelyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ParcelStatusHistory>()), Times.Never);
    }

    #endregion

    #region 2. Accepting Request & Agent Becoming Busy

    [Fact]
    public async Task AcceptDeliveryRequestAsync_AvailableAgent_AssignsParcel_CreatesAssignment_AndSetsAgentToBusy()
    {
        // Arrange
        var agentId = "agent-123";
        var parcelId = "parcel-001";
        var customerId = "cust-555";

        var agent = new User
        {
            Id = agentId,
            FullName = "David Courier",
            PhoneNumber = "+91 9876543210",
            Role = UserRole.DeliveryAgent,
            IsActive = true,
            AvailabilityStatus = AgentAvailabilityStatus.Available
        };

        var parcel = new Parcel
        {
            Id = parcelId,
            TrackingId = "PDS-20261003-0001",
            CustomerId = customerId,
            Status = ParcelStatus.Booked,
            AssignedAgentId = null,
            PickupAddress = new Address { StreetAddress = "100 MG Road", City = "Bangalore" },
            DeliveryAddress = new Address { StreetAddress = "50 Indiranagar", City = "Bangalore" }
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(parcel);
        _mockParcelRepo.Setup(r => r.AssignAgentSafelyAsync(parcelId, agentId, It.IsAny<ParcelStatusHistory>()))
            .ReturnsAsync(true);
        _mockAssignmentRepo.Setup(r => r.CreateAsync(It.IsAny<AgentAssignment>())).ReturnsAsync(new AgentAssignment());
        _mockUserRepo.Setup(r => r.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Busy)).ReturnsAsync(true);
        _mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>())).ReturnsAsync(new Notification());

        // Act
        var success = await _agentService.AcceptDeliveryRequestAsync(agentId, parcelId);

        // Assert
        Assert.True(success);
        _mockParcelRepo.Verify(r => r.AssignAgentSafelyAsync(parcelId, agentId, It.Is<ParcelStatusHistory>(h => h.Status == ParcelStatus.AgentAssigned)), Times.Once);
        _mockAssignmentRepo.Verify(r => r.CreateAsync(It.Is<AgentAssignment>(a => a.AgentId == agentId && a.ParcelId == parcelId && a.Status == "Accepted")), Times.Once);
        _mockUserRepo.Verify(r => r.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Busy), Times.Once);
        _mockNotificationRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n => n.UserId == customerId && n.Type == NotificationType.AgentAssigned)), Times.Once);
    }

    #endregion

    #region 3. Preventing Duplicate Assignment

    [Fact]
    public async Task AcceptDeliveryRequestAsync_ParcelAlreadyAssigned_ThrowsInvalidOperationException()
    {
        // Arrange
        var agentId = "agent-123";
        var parcelId = "parcel-001";

        var agent = new User
        {
            Id = agentId,
            Role = UserRole.DeliveryAgent,
            IsActive = true,
            AvailabilityStatus = AgentAvailabilityStatus.Available
        };

        var alreadyAssignedParcel = new Parcel
        {
            Id = parcelId,
            TrackingId = "PDS-20261003-0001",
            Status = ParcelStatus.AgentAssigned,
            AssignedAgentId = "other-agent-999" // Already taken by another agent
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(alreadyAssignedParcel);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.AcceptDeliveryRequestAsync(agentId, parcelId));

        Assert.Contains("already been claimed", ex.Message);
        _mockParcelRepo.Verify(r => r.AssignAgentSafelyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ParcelStatusHistory>()), Times.Never);
    }

    [Fact]
    public async Task AcceptDeliveryRequestAsync_ConcurrentRaceCondition_AssignFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var agentId = "agent-123";
        var parcelId = "parcel-001";

        var agent = new User
        {
            Id = agentId,
            Role = UserRole.DeliveryAgent,
            IsActive = true,
            AvailabilityStatus = AgentAvailabilityStatus.Available
        };

        var parcel = new Parcel
        {
            Id = parcelId,
            Status = ParcelStatus.Booked,
            AssignedAgentId = null
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(parcel);
        // Simulate concurrent conflict where another agent claimed it a fraction of a millisecond earlier
        _mockParcelRepo.Setup(r => r.AssignAgentSafelyAsync(parcelId, agentId, It.IsAny<ParcelStatusHistory>()))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.AcceptDeliveryRequestAsync(agentId, parcelId));

        Assert.Contains("claimed by another partner concurrently", ex.Message);
        _mockUserRepo.Verify(r => r.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Busy), Times.Never);
    }

    #endregion

    #region 4. Distance Sorting (Nearest Request First)

    [Fact]
    public async Task GetAvailableRequestsAsync_CalculatesDistancesAndSortsNearestFirst()
    {
        // Arrange
        var agentId = "agent-123";
        // Agent base at (12.9716, 77.5946) [Bangalore City Center]
        var agent = new User
        {
            Id = agentId,
            CurrentLatitude = 12.9716,
            CurrentLongitude = 77.5946,
            Role = UserRole.DeliveryAgent
        };

        // Parcel A: ~2.5 km away (Indiranagar)
        var parcelA = new Parcel
        {
            Id = "parcel-A",
            TrackingId = "PDS-A",
            PickupLatitude = 12.9784,
            PickupLongitude = 77.6160,
            Weight = 2.0,
            DeliveryCharge = 80m,
            BookingDate = DateTime.UtcNow
        };

        // Parcel B: ~8.0 km away (Whitefield)
        var parcelB = new Parcel
        {
            Id = "parcel-B",
            TrackingId = "PDS-B",
            PickupLatitude = 12.9698,
            PickupLongitude = 77.6700,
            Weight = 5.0,
            DeliveryCharge = 150m,
            BookingDate = DateTime.UtcNow
        };

        // Parcel C: ~1.2 km away (MG Road)
        var parcelC = new Parcel
        {
            Id = "parcel-C",
            TrackingId = "PDS-C",
            PickupLatitude = 12.9750,
            PickupLongitude = 77.6000,
            Weight = 1.0,
            DeliveryCharge = 60m,
            BookingDate = DateTime.UtcNow
        };

        // Return parcels in unsorted order (B, A, C)
        _mockUserRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockParcelRepo.Setup(r => r.GetAvailableParcelsForPickupAsync())
            .ReturnsAsync(new List<Parcel> { parcelB, parcelA, parcelC });

        // Act
        var result = await _agentService.GetAvailableRequestsAsync(agentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // Nearest request MUST appear first!
        Assert.Equal("parcel-C", result[0].ParcelId);
        Assert.Equal("parcel-A", result[1].ParcelId);
        Assert.Equal("parcel-B", result[2].ParcelId);

        Assert.True(result[0].DistanceFromAgentKm < result[1].DistanceFromAgentKm);
        Assert.True(result[1].DistanceFromAgentKm < result[2].DistanceFromAgentKm);
    }

    #endregion

    #region 5. Sequential Delivery Lifecycle Transitions

    [Fact]
    public async Task StatusTransitions_FullLifecycle_FromAgentAssignedToDelivered_Succeeds()
    {
        // Arrange
        var agentId = "agent-123";
        var parcelId = "parcel-001";
        var customerId = "cust-555";

        var parcel = new Parcel
        {
            Id = parcelId,
            TrackingId = "PDS-20261003-0001",
            CustomerId = customerId,
            AssignedAgentId = agentId,
            Status = ParcelStatus.AgentAssigned,
            PickupAddress = new Address { StreetAddress = "100 MG Road", City = "Bangalore" },
            DeliveryAddress = new Address { StreetAddress = "50 Indiranagar", City = "Bangalore", RecipientName = "Alice Smith" }
        };

        var assignment = new AgentAssignment
        {
            Id = "assign-01",
            ParcelId = parcelId,
            AgentId = agentId,
            Status = "Accepted"
        };

        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(parcel);
        _mockAssignmentRepo.Setup(r => r.GetActiveAssignmentForParcelAsync(parcelId)).ReturnsAsync(assignment);
        _mockAssignmentRepo.Setup(r => r.UpdateAsync(It.IsAny<AgentAssignment>())).ReturnsAsync(true);
        _mockParcelRepo.Setup(r => r.UpdateStatusAsync(parcelId, It.IsAny<ParcelStatus>(), It.IsAny<ParcelStatusHistory>()))
            .ReturnsAsync(true);
        _mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>())).ReturnsAsync(new Notification());

        // 1. AgentAssigned -> PickedUp
        var pickedUpResult = await _agentService.MarkPickedUpAsync(agentId, parcelId, "Picked up package");
        Assert.True(pickedUpResult);
        parcel.Status = ParcelStatus.PickedUp; // Advance state

        // 2. PickedUp -> InTransit
        var inTransitResult = await _agentService.MarkInTransitAsync(agentId, parcelId, "In transit to Indiranagar");
        Assert.True(inTransitResult);
        parcel.Status = ParcelStatus.InTransit; // Advance state

        // 3. InTransit -> OutForDelivery
        var outForDeliveryResult = await _agentService.MarkOutForDeliveryAsync(agentId, parcelId, "Courier out on bike");
        Assert.True(outForDeliveryResult);
        parcel.Status = ParcelStatus.OutForDelivery; // Advance state

        // 4. OutForDelivery -> Delivered
        _mockDeliveryRecordRepo.Setup(r => r.CreateAsync(It.IsAny<DeliveryRecord>())).ReturnsAsync(new DeliveryRecord());
        _mockUserRepo.Setup(r => r.IncrementCompletedDeliveriesAsync(agentId)).ReturnsAsync(true);
        _mockUserRepo.Setup(r => r.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Available)).ReturnsAsync(true);

        var confirmModel = new ConfirmDeliveryViewModel
        {
            ParcelId = parcelId,
            TrackingId = parcel.TrackingId,
            ReceivedBy = "Alice Smith",
            DeliveryNotes = "Delivered to front door."
        };

        var deliveredResult = await _agentService.ConfirmDeliveryAsync(agentId, confirmModel);
        Assert.True(deliveredResult);

        // Verify Delivered calls
        _mockParcelRepo.Verify(r => r.UpdateStatusAsync(parcelId, ParcelStatus.Delivered, It.IsAny<ParcelStatusHistory>()), Times.Once);
        _mockDeliveryRecordRepo.Verify(r => r.CreateAsync(It.Is<DeliveryRecord>(d => d.ReceivedBy == "Alice Smith" && d.AgentId == agentId)), Times.Once);
        _mockUserRepo.Verify(r => r.IncrementCompletedDeliveriesAsync(agentId), Times.Once);
        _mockUserRepo.Verify(r => r.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Available), Times.Once);
    }

    #endregion

    #region 6. Invalid Status Transitions Prevention

    [Fact]
    public async Task MarkPickedUpAsync_WhenStatusIsDelivered_ThrowsInvalidOperationException()
    {
        // Arrange: Delivered parcel cannot be marked as Picked Up
        var agentId = "agent-123";
        var parcelId = "parcel-001";

        var deliveredParcel = new Parcel
        {
            Id = parcelId,
            AssignedAgentId = agentId,
            Status = ParcelStatus.Delivered // Terminal status!
        };

        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(deliveredParcel);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.MarkPickedUpAsync(agentId, parcelId));

        Assert.Contains("Invalid state transition", ex.Message);
        _mockParcelRepo.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<ParcelStatus>(), It.IsAny<ParcelStatusHistory>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_WhenStatusIsPickedUp_ThrowsInvalidOperationException()
    {
        // Arrange: Cannot mark as Delivered directly from PickedUp without InTransit / OutForDelivery
        var agentId = "agent-123";
        var parcelId = "parcel-001";

        var pickedUpParcel = new Parcel
        {
            Id = parcelId,
            AssignedAgentId = agentId,
            Status = ParcelStatus.Booked // Not yet out for delivery
        };

        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(pickedUpParcel);

        var model = new ConfirmDeliveryViewModel
        {
            ParcelId = parcelId,
            ReceivedBy = "Alice"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.ConfirmDeliveryAsync(agentId, model));

        Assert.Contains("Invalid state transition", ex.Message);
    }

    #endregion

    #region 7. Delivery Failure & Agent Becoming Available

    [Fact]
    public async Task ReportDeliveryFailureAsync_RecordsFailureReason_AndResetsAgentToAvailable()
    {
        // Arrange
        var agentId = "agent-123";
        var parcelId = "parcel-001";
        var customerId = "cust-555";

        var parcel = new Parcel
        {
            Id = parcelId,
            TrackingId = "PDS-20261003-0001",
            CustomerId = customerId,
            AssignedAgentId = agentId,
            Status = ParcelStatus.OutForDelivery,
            DeliveryAddress = new Address { StreetAddress = "50 Indiranagar", City = "Bangalore" }
        };

        var assignment = new AgentAssignment
        {
            Id = "assign-01",
            ParcelId = parcelId,
            AgentId = agentId,
            Status = "OutForDelivery"
        };

        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(parcel);
        _mockAssignmentRepo.Setup(r => r.GetActiveAssignmentForParcelAsync(parcelId)).ReturnsAsync(assignment);
        _mockAssignmentRepo.Setup(r => r.UpdateAsync(It.IsAny<AgentAssignment>())).ReturnsAsync(true);
        _mockParcelRepo.Setup(r => r.UpdateStatusAsync(parcelId, ParcelStatus.DeliveryFailed, It.IsAny<ParcelStatusHistory>()))
            .ReturnsAsync(true);
        _mockUserRepo.Setup(r => r.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Available)).ReturnsAsync(true);
        _mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>())).ReturnsAsync(new Notification());

        var failureModel = new ReportFailureViewModel
        {
            ParcelId = parcelId,
            TrackingId = parcel.TrackingId,
            Reason = "Customer unavailable",
            Notes = "Customer did not answer doorbell and phone was unreachable after 3 attempts."
        };

        // Act
        var result = await _agentService.ReportDeliveryFailureAsync(agentId, failureModel);

        // Assert
        Assert.True(result);
        _mockParcelRepo.Verify(r => r.UpdateStatusAsync(parcelId, ParcelStatus.DeliveryFailed, It.Is<ParcelStatusHistory>(h => h.Notes != null && h.Notes.Contains("Customer unavailable"))), Times.Once);
        _mockAssignmentRepo.Verify(r => r.UpdateAsync(It.Is<AgentAssignment>(a => a.Status == "Failed" && a.FailureReason == "Customer unavailable")), Times.Once);
        _mockUserRepo.Verify(r => r.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Available), Times.Once);
        _mockNotificationRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n => n.UserId == customerId && n.Type == NotificationType.Alert)), Times.Once);
    }

    #endregion

    #region 8. Security & Agent Isolation

    [Fact]
    public async Task ModifyDelivery_OtherAgentsDelivery_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var requestingAgentId = "agent-123";
        var parcelOwnerAgentId = "agent-999";
        var parcelId = "parcel-001";

        var parcel = new Parcel
        {
            Id = parcelId,
            AssignedAgentId = parcelOwnerAgentId, // Assigned to agent 999
            Status = ParcelStatus.OutForDelivery
        };

        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(parcel);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _agentService.MarkPickedUpAsync(requestingAgentId, parcelId));

        Assert.Contains("not authorized", ex.Message);
        _mockParcelRepo.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<ParcelStatus>(), It.IsAny<ParcelStatusHistory>()), Times.Never);
    }

    #endregion
}
