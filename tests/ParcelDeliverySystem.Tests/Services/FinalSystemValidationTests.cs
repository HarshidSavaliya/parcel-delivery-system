using Microsoft.Extensions.Logging;
using Moq;
using ParcelDeliverySystem.Models.DTOs.Pricing;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Implementations;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Agent;
using ParcelDeliverySystem.ViewModels.Parcels;
using Xunit;

namespace ParcelDeliverySystem.Tests.Services;

/// <summary>
/// Final integration, security, and state-machine validation tests for Parcel Delivery System.
/// </summary>
public class FinalSystemValidationTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IParcelRepository> _mockParcelRepo;
    private readonly Mock<IAddressRepository> _mockAddressRepo;
    private readonly Mock<IAgentAssignmentRepository> _mockAssignmentRepo;
    private readonly Mock<IDeliveryRecordRepository> _mockDeliveryRecordRepo;
    private readonly Mock<INotificationRepository> _mockNotificationRepo;
    private readonly Mock<IPasswordHasherService> _mockPasswordHasher;
    private readonly Mock<ITrackingIdGenerator> _mockTrackingIdGen;
    private readonly Mock<IDeliveryChargeService> _mockChargeService;
    private readonly Mock<IRoutingService> _mockRoutingService;
    private readonly Mock<IGeocodingService> _mockGeocodingService;
    private readonly Mock<ILogger<DeliveryAgentService>> _mockAgentLogger;
    private readonly Mock<ILogger<ParcelBookingService>> _mockBookingLogger;

    private readonly DeliveryAgentService _agentService;
    private readonly ParcelBookingService _bookingService;

    public FinalSystemValidationTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockParcelRepo = new Mock<IParcelRepository>();
        _mockAddressRepo = new Mock<IAddressRepository>();
        _mockAssignmentRepo = new Mock<IAgentAssignmentRepository>();
        _mockDeliveryRecordRepo = new Mock<IDeliveryRecordRepository>();
        _mockNotificationRepo = new Mock<INotificationRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasherService>();
        _mockTrackingIdGen = new Mock<ITrackingIdGenerator>();
        _mockChargeService = new Mock<IDeliveryChargeService>();
        _mockRoutingService = new Mock<IRoutingService>();
        _mockGeocodingService = new Mock<IGeocodingService>();
        _mockAgentLogger = new Mock<ILogger<DeliveryAgentService>>();
        _mockBookingLogger = new Mock<ILogger<ParcelBookingService>>();

        _agentService = new DeliveryAgentService(
            _mockUserRepo.Object,
            _mockParcelRepo.Object,
            _mockAssignmentRepo.Object,
            _mockDeliveryRecordRepo.Object,
            _mockNotificationRepo.Object,
            _mockAgentLogger.Object
        );

        _bookingService = new ParcelBookingService(
            _mockParcelRepo.Object,
            _mockAddressRepo.Object,
            _mockUserRepo.Object,
            _mockNotificationRepo.Object,
            _mockTrackingIdGen.Object,
            _mockChargeService.Object,
            _mockRoutingService.Object,
            _mockGeocodingService.Object,
            _mockBookingLogger.Object
        );
    }

    #region 1. Complete State Machine Lifecycle & Invalid Transitions

    [Fact]
    public async Task FullLifecycle_ProgressesThroughAllValidStatesSequentially()
    {
        // 1. Arrange Parcel & Agent
        var agentId = "agent-007";
        var customerId = "cust-101";
        var parcelId = "parcel-lifecycle-1";
        var trackingId = "PDS-20261003-9999";

        var agent = new User
        {
            Id = agentId,
            FullName = "Alex Courier",
            Role = UserRole.DeliveryAgent,
            IsActive = true,
            AvailabilityStatus = AgentAvailabilityStatus.Available
        };

        var parcel = new Parcel
        {
            Id = parcelId,
            TrackingId = trackingId,
            CustomerId = customerId,
            Status = ParcelStatus.Booked,
            PickupAddress = new Address { City = "Springfield", FormattedAddress = "100 Main St, Springfield" },
            DeliveryAddress = new Address { City = "Portland", FormattedAddress = "500 Oak St, Portland", RecipientName = "Jane Doe" },
            StatusHistory = new List<ParcelStatusHistory>()
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(parcel);
        _mockParcelRepo.Setup(r => r.AssignAgentSafelyAsync(parcelId, agentId, It.IsAny<ParcelStatusHistory>()))
            .Callback<string, string, ParcelStatusHistory>((_, _, h) =>
            {
                parcel.Status = ParcelStatus.AgentAssigned;
                parcel.AssignedAgentId = agentId;
                parcel.StatusHistory.Add(h);
            }).ReturnsAsync(true);

        _mockParcelRepo.Setup(r => r.UpdateStatusAsync(parcelId, It.IsAny<ParcelStatus>(), It.IsAny<ParcelStatusHistory>()))
            .Callback<string, ParcelStatus, ParcelStatusHistory>((_, status, h) =>
            {
                parcel.Status = status;
                parcel.StatusHistory.Add(h);
            }).ReturnsAsync(true);

        // Step 1: Accept request (Booked -> AgentAssigned)
        var acceptResult = await _agentService.AcceptDeliveryRequestAsync(agentId, parcelId);
        Assert.True(acceptResult);
        Assert.Equal(ParcelStatus.AgentAssigned, parcel.Status);

        // Step 2: Mark Picked Up (AgentAssigned -> PickedUp)
        var pickupResult = await _agentService.MarkPickedUpAsync(agentId, parcelId, "Picked up from sender");
        Assert.True(pickupResult);
        Assert.Equal(ParcelStatus.PickedUp, parcel.Status);

        // Step 3: Mark In Transit (PickedUp -> InTransit)
        var inTransitResult = await _agentService.MarkInTransitAsync(agentId, parcelId, "On highway 5");
        Assert.True(inTransitResult);
        Assert.Equal(ParcelStatus.InTransit, parcel.Status);

        // Step 4: Mark Out For Delivery (InTransit -> OutForDelivery)
        var outResult = await _agentService.MarkOutForDeliveryAsync(agentId, parcelId, "Arriving in 15 mins");
        Assert.True(outResult);
        Assert.Equal(ParcelStatus.OutForDelivery, parcel.Status);

        // Step 5: Confirm Delivered (OutForDelivery -> Delivered)
        var confirmModel = new ConfirmDeliveryViewModel
        {
            ParcelId = parcelId,
            ReceivedBy = "Jane Doe",
            DeliveryNotes = "Left with front desk"
        };
        var deliverResult = await _agentService.ConfirmDeliveryAsync(agentId, confirmModel);
        Assert.True(deliverResult);
        Assert.Equal(ParcelStatus.Delivered, parcel.Status);

        // Verify history has records for each step
        Assert.Equal(5, parcel.StatusHistory.Count);
        Assert.Equal(ParcelStatus.AgentAssigned, parcel.StatusHistory[0].Status);
        Assert.Equal(ParcelStatus.PickedUp, parcel.StatusHistory[1].Status);
        Assert.Equal(ParcelStatus.InTransit, parcel.StatusHistory[2].Status);
        Assert.Equal(ParcelStatus.OutForDelivery, parcel.StatusHistory[3].Status);
        Assert.Equal(ParcelStatus.Delivered, parcel.StatusHistory[4].Status);
    }

    [Theory]
    [InlineData(ParcelStatus.Booked)]
    [InlineData(ParcelStatus.InTransit)]
    [InlineData(ParcelStatus.Delivered)]
    [InlineData(ParcelStatus.Cancelled)]
    public async Task MarkPickedUpAsync_WhenStatusIsNotAgentAssigned_ThrowsInvalidOperationException(ParcelStatus currentStatus)
    {
        // Arrange
        var agentId = "agent-123";
        var parcelId = "parcel-123";
        var parcel = new Parcel
        {
            Id = parcelId,
            AssignedAgentId = agentId,
            Status = currentStatus,
            PickupAddress = new Address { FormattedAddress = "123 Main St" }
        };

        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(parcel);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.MarkPickedUpAsync(agentId, parcelId));
    }

    [Theory]
    [InlineData(ParcelStatus.Delivered)]
    [InlineData(ParcelStatus.Cancelled)]
    public async Task ReportDeliveryFailureAsync_WhenParcelInTerminalStatus_ThrowsInvalidOperationException(ParcelStatus status)
    {
        // Arrange
        var agentId = "agent-123";
        var parcelId = "parcel-123";
        var parcel = new Parcel
        {
            Id = parcelId,
            AssignedAgentId = agentId,
            Status = status,
            DeliveryAddress = new Address { FormattedAddress = "456 Destination St" }
        };

        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(parcel);

        var failureModel = new ReportFailureViewModel
        {
            ParcelId = parcelId,
            Reason = "Recipient unavailable",
            Notes = "Called phone 3 times"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.ReportDeliveryFailureAsync(agentId, failureModel));
    }

    #endregion

    #region 2. Security & IDOR Isolation Tests

    [Fact]
    public async Task GetParcelDetailsAsync_WhenRequestedByDifferentCustomer_ReturnsNull()
    {
        // Arrange
        var trackingId = "PDS-SECRET-001";
        var ownerCustomerId = "cust-OWNER";
        var attackerCustomerId = "cust-ATTACKER";

        var parcel = new Parcel
        {
            Id = "p-1",
            TrackingId = trackingId,
            CustomerId = ownerCustomerId,
            AssignedAgentId = null
        };

        _mockParcelRepo.Setup(r => r.GetByTrackingIdAsync(trackingId)).ReturnsAsync(parcel);

        // Act
        var result = await _bookingService.GetParcelDetailsAsync(trackingId, attackerCustomerId, isAdmin: false);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetParcelDetailsAsync_WhenRequestedByAdmin_ReturnsParcel()
    {
        // Arrange
        var trackingId = "PDS-SECRET-001";
        var ownerCustomerId = "cust-OWNER";
        var adminUserId = "admin-999";

        var parcel = new Parcel
        {
            Id = "p-1",
            TrackingId = trackingId,
            CustomerId = ownerCustomerId,
            AssignedAgentId = null
        };

        _mockParcelRepo.Setup(r => r.GetByTrackingIdAsync(trackingId)).ReturnsAsync(parcel);

        // Act
        var result = await _bookingService.GetParcelDetailsAsync(trackingId, adminUserId, isAdmin: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(trackingId, result.TrackingId);
    }

    [Fact]
    public async Task MarkPickedUpAsync_WhenAgentIsNotAssignedToParcel_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var parcelId = "parcel-auth-test";
        var authorizedAgentId = "agent-REAL";
        var unauthorizedAgentId = "agent-IMPOSTOR";

        var parcel = new Parcel
        {
            Id = parcelId,
            AssignedAgentId = authorizedAgentId,
            Status = ParcelStatus.AgentAssigned,
            PickupAddress = new Address { FormattedAddress = "Pickup Location" }
        };

        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(parcel);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _agentService.MarkPickedUpAsync(unauthorizedAgentId, parcelId));
    }

    #endregion

    #region 3. Concurrency & Duplicate Assignment Tests

    [Fact]
    public async Task AcceptDeliveryRequestAsync_WhenAnotherAgentClaimsFirst_ThrowsInvalidOperationException()
    {
        // Arrange
        var agentId = "agent-1";
        var parcelId = "parcel-claimed";

        var agent = new User
        {
            Id = agentId,
            Role = UserRole.DeliveryAgent,
            IsActive = true,
            AvailabilityStatus = AgentAvailabilityStatus.Available
        };

        // Parcel is already assigned to agent-OTHER
        var alreadyAssignedParcel = new Parcel
        {
            Id = parcelId,
            AssignedAgentId = "agent-OTHER",
            Status = ParcelStatus.AgentAssigned
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockParcelRepo.Setup(r => r.GetByIdAsync(parcelId)).ReturnsAsync(alreadyAssignedParcel);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.AcceptDeliveryRequestAsync(agentId, parcelId));

        Assert.Contains("claimed by another", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
