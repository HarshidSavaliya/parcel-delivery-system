using Microsoft.Extensions.Logging;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Agent;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Service implementation handling delivery agent profile management, proximity-based request queries,
/// atomic request acceptance, delivery lifecycle progression, and agent availability coordination.
/// </summary>
public class DeliveryAgentService : IDeliveryAgentService
{
    private readonly IUserRepository _userRepository;
    private readonly IParcelRepository _parcelRepository;
    private readonly IAgentAssignmentRepository _assignmentRepository;
    private readonly IDeliveryRecordRepository _deliveryRecordRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<DeliveryAgentService> _logger;

    public DeliveryAgentService(
        IUserRepository userRepository,
        IParcelRepository parcelRepository,
        IAgentAssignmentRepository assignmentRepository,
        IDeliveryRecordRepository deliveryRecordRepository,
        INotificationRepository notificationRepository,
        ILogger<DeliveryAgentService> logger)
    {
        _userRepository = userRepository;
        _parcelRepository = parcelRepository;
        _assignmentRepository = assignmentRepository;
        _deliveryRecordRepository = deliveryRecordRepository;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<AgentProfileViewModel?> GetAgentProfileAsync(string agentId)
    {
        var user = await _userRepository.GetByIdAsync(agentId);
        if (user == null || user.Role != UserRole.DeliveryAgent)
        {
            return null;
        }

        return new AgentProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            CurrentLatitude = user.CurrentLatitude,
            CurrentLongitude = user.CurrentLongitude,
            VehicleType = user.VehicleType,
            VehiclePlateNumber = user.VehiclePlateNumber,
            DrivingLicenseNumber = user.DrivingLicenseNumber,
            AvailabilityStatus = user.AvailabilityStatus,
            CompletedDeliveriesCount = user.CompletedDeliveriesCount,
            Rating = user.Rating
        };
    }

    public async Task<bool> UpdateAgentProfileAsync(string agentId, AgentProfileViewModel model)
    {
        return await _userRepository.UpdateAgentProfileAsync(
            agentId,
            model.FullName,
            model.PhoneNumber,
            model.Address,
            model.CurrentLatitude,
            model.CurrentLongitude,
            model.VehicleType,
            model.VehiclePlateNumber,
            model.DrivingLicenseNumber
        );
    }

    public async Task<bool> UpdateAvailabilityStatusAsync(string agentId, AgentAvailabilityStatus status)
    {
        var agent = await _userRepository.GetByIdAsync(agentId);
        if (agent == null || agent.Role != UserRole.DeliveryAgent)
        {
            return false;
        }

        return await _userRepository.UpdateAvailabilityStatusAsync(agentId, status);
    }

    public async Task<bool> UpdateLocationAsync(string agentId, double latitude, double longitude)
    {
        return await _userRepository.UpdateLocationAsync(agentId, latitude, longitude);
    }

    public async Task<IReadOnlyList<AvailableRequestViewModel>> GetAvailableRequestsAsync(string agentId)
    {
        var agent = await _userRepository.GetByIdAsync(agentId);
        var agentLat = agent?.CurrentLatitude ?? 12.9716;
        var agentLon = agent?.CurrentLongitude ?? 77.5946;

        var availableParcels = await _parcelRepository.GetAvailableParcelsForPickupAsync();
        var results = new List<AvailableRequestViewModel>();

        foreach (var parcel in availableParcels)
        {
            // Calculate distance between agent location and parcel pickup location
            var distance = CalculateDistanceKm(agentLat, agentLon, parcel.PickupLatitude, parcel.PickupLongitude);
            var travelMinutes = Math.Max(4, (int)Math.Round((distance / 25.0) * 60));

            results.Add(new AvailableRequestViewModel
            {
                ParcelId = parcel.Id,
                TrackingId = parcel.TrackingId,
                PickupAddress = parcel.PickupAddress,
                DeliveryAddress = parcel.DeliveryAddress,
                PickupLatitude = parcel.PickupLatitude,
                PickupLongitude = parcel.PickupLongitude,
                DeliveryLatitude = parcel.DeliveryLatitude,
                DeliveryLongitude = parcel.DeliveryLongitude,
                DistanceFromAgentKm = Math.Round(distance, 2),
                EstimatedTravelTimeMinutes = travelMinutes,
                ParcelType = parcel.ParcelType,
                WeightKg = parcel.Weight,
                Length = parcel.Length,
                Width = parcel.Width,
                Height = parcel.Height,
                DeliveryType = parcel.DeliveryType,
                DeliveryCharge = parcel.DeliveryCharge,
                BookingTime = parcel.BookingDate,
                SpecialInstructions = parcel.SpecialInstructions
            });
        }

        // Sort by distance ascending: nearest request first!
        return results.OrderBy(r => r.DistanceFromAgentKm).ToList();
    }

    public async Task<bool> AcceptDeliveryRequestAsync(string agentId, string parcelId)
    {
        var agent = await _userRepository.GetByIdAsync(agentId);
        if (agent == null || agent.Role != UserRole.DeliveryAgent || !agent.IsActive)
        {
            throw new InvalidOperationException("Delivery agent profile is invalid or inactive.");
        }

        // 1. Agent Availability Check
        if (agent.AvailabilityStatus == AgentAvailabilityStatus.Offline)
        {
            throw new InvalidOperationException("Offline agents cannot accept delivery requests. Please switch your status to Available first.");
        }

        if (agent.AvailabilityStatus == AgentAvailabilityStatus.Busy)
        {
            throw new InvalidOperationException("You currently have an active delivery in progress. Complete it before accepting new requests.");
        }

        // 2. Fetch and Validate Parcel
        var parcel = await _parcelRepository.GetByIdAsync(parcelId);
        if (parcel == null)
        {
            throw new InvalidOperationException("The requested parcel was not found.");
        }

        if (parcel.AssignedAgentId != null || 
           (parcel.Status != ParcelStatus.Booked && parcel.Status != ParcelStatus.PendingPickup))
        {
            throw new InvalidOperationException("This delivery request has already been claimed by another delivery partner.");
        }

        // 3. Atomically Assign Parcel
        var historyEntry = new ParcelStatusHistory
        {
            Status = ParcelStatus.AgentAssigned,
            Timestamp = DateTime.UtcNow,
            Location = parcel.PickupAddress.FormattedAddress,
            Notes = $"Accepted by Delivery Partner: {agent.FullName} ({agent.PhoneNumber}). En route to pickup.",
            UpdatedByUserId = agentId,
            UpdatedByRole = "DeliveryAgent"
        };

        var assignedSuccessfully = await _parcelRepository.AssignAgentSafelyAsync(parcelId, agentId, historyEntry);
        if (!assignedSuccessfully)
        {
            throw new InvalidOperationException("This delivery request was claimed by another partner concurrently.");
        }

        // 4. Create Agent Assignment Record
        var assignment = new AgentAssignment
        {
            ParcelId = parcelId,
            AgentId = agentId,
            Status = "Accepted",
            AssignedAt = DateTime.UtcNow,
            AcceptedAt = DateTime.UtcNow,
            RespondedAt = DateTime.UtcNow
        };
        await _assignmentRepository.CreateAsync(assignment);

        // 5. Transition Agent Availability Status: Available -> Busy
        await _userRepository.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Busy);

        // 6. Send Automated Customer Notification
        var notification = new Notification
        {
            UserId = parcel.CustomerId,
            Title = "Delivery Partner Assigned 🚚",
            Message = $"Agent {agent.FullName} has accepted your parcel #{parcel.TrackingId} and is en route for pickup.",
            Type = NotificationType.AgentAssigned,
            ReferenceId = parcelId
        };
        await _notificationRepository.CreateAsync(notification);

        _logger.LogInformation("Parcel {TrackingId} successfully accepted by Agent {AgentId}.", parcel.TrackingId, agentId);
        return true;
    }

    public async Task<IReadOnlyList<Parcel>> GetAgentDeliveriesAsync(string agentId)
    {
        return await _parcelRepository.GetByAgentIdAsync(agentId);
    }

    public async Task<AgentDeliveryManagementViewModel> GetAgentDeliveryDetailAsync(string agentId, string parcelIdOrTrackingId)
    {
        Parcel? parcel;
        if (parcelIdOrTrackingId.StartsWith("PDS-", StringComparison.OrdinalIgnoreCase))
        {
            parcel = await _parcelRepository.GetByTrackingIdAsync(parcelIdOrTrackingId);
        }
        else
        {
            parcel = await _parcelRepository.GetByIdAsync(parcelIdOrTrackingId);
        }

        if (parcel == null)
        {
            throw new InvalidOperationException("Parcel shipment not found.");
        }

        if (parcel.AssignedAgentId != agentId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view or manage this shipment.");
        }

        var assignment = await _assignmentRepository.GetLatestAssignmentForParcelAsync(parcel.Id);

        return new AgentDeliveryManagementViewModel
        {
            Parcel = parcel,
            CurrentAssignment = assignment
        };
    }

    public async Task<bool> MarkPickedUpAsync(string agentId, string parcelId, string? notes = null)
    {
        var parcel = await ValidateAndAuthorizeAgentParcelAsync(agentId, parcelId);

        // State Machine Rule: AgentAssigned -> PickedUp
        if (parcel.Status != ParcelStatus.AgentAssigned)
        {
            throw new InvalidOperationException($"Invalid state transition: Cannot mark parcel as Picked Up when status is {parcel.Status}. Expected status: AgentAssigned.");
        }

        var historyEntry = new ParcelStatusHistory
        {
            Status = ParcelStatus.PickedUp,
            Timestamp = DateTime.UtcNow,
            Location = parcel.PickupAddress.FormattedAddress,
            Notes = string.IsNullOrWhiteSpace(notes) ? "Parcel safely picked up from sender address." : notes.Trim(),
            UpdatedByUserId = agentId,
            UpdatedByRole = "DeliveryAgent"
        };

        var updated = await _parcelRepository.UpdateStatusAsync(parcelId, ParcelStatus.PickedUp, historyEntry);
        if (updated)
        {
            var assignment = await _assignmentRepository.GetActiveAssignmentForParcelAsync(parcelId);
            if (assignment != null)
            {
                assignment.PickupTime = DateTime.UtcNow;
                assignment.Status = "PickedUp";
                await _assignmentRepository.UpdateAsync(assignment);
            }

            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = parcel.CustomerId,
                Title = "Parcel Picked Up 📦",
                Message = $"Your parcel #{parcel.TrackingId} has been picked up by the delivery agent.",
                Type = NotificationType.StatusUpdate,
                ReferenceId = parcelId
            });
        }

        return updated;
    }

    public async Task<bool> MarkInTransitAsync(string agentId, string parcelId, string? notes = null)
    {
        var parcel = await ValidateAndAuthorizeAgentParcelAsync(agentId, parcelId);

        // State Machine Rule: PickedUp -> InTransit
        if (parcel.Status != ParcelStatus.PickedUp)
        {
            throw new InvalidOperationException($"Invalid state transition: Cannot mark parcel as In Transit when status is {parcel.Status}. Expected status: PickedUp.");
        }

        var historyEntry = new ParcelStatusHistory
        {
            Status = ParcelStatus.InTransit,
            Timestamp = DateTime.UtcNow,
            Location = parcel.PickupAddress.City,
            Notes = string.IsNullOrWhiteSpace(notes) ? "Parcel is in transit to destination hub / address." : notes.Trim(),
            UpdatedByUserId = agentId,
            UpdatedByRole = "DeliveryAgent"
        };

        var updated = await _parcelRepository.UpdateStatusAsync(parcelId, ParcelStatus.InTransit, historyEntry);
        if (updated)
        {
            var assignment = await _assignmentRepository.GetActiveAssignmentForParcelAsync(parcelId);
            if (assignment != null)
            {
                assignment.Status = "InTransit";
                await _assignmentRepository.UpdateAsync(assignment);
            }

            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = parcel.CustomerId,
                Title = "Parcel In Transit 🚚",
                Message = $"Your parcel #{parcel.TrackingId} is in transit towards the destination.",
                Type = NotificationType.StatusUpdate,
                ReferenceId = parcelId
            });
        }

        return updated;
    }

    public async Task<bool> MarkOutForDeliveryAsync(string agentId, string parcelId, string? notes = null)
    {
        var parcel = await ValidateAndAuthorizeAgentParcelAsync(agentId, parcelId);

        // State Machine Rule: InTransit (or PickedUp) -> OutForDelivery
        if (parcel.Status != ParcelStatus.InTransit && parcel.Status != ParcelStatus.PickedUp)
        {
            throw new InvalidOperationException($"Invalid state transition: Cannot mark parcel as Out For Delivery when status is {parcel.Status}. Expected status: InTransit.");
        }

        var historyEntry = new ParcelStatusHistory
        {
            Status = ParcelStatus.OutForDelivery,
            Timestamp = DateTime.UtcNow,
            Location = parcel.DeliveryAddress.City,
            Notes = string.IsNullOrWhiteSpace(notes) ? "Parcel is out with delivery agent for final drop-off." : notes.Trim(),
            UpdatedByUserId = agentId,
            UpdatedByRole = "DeliveryAgent"
        };

        var updated = await _parcelRepository.UpdateStatusAsync(parcelId, ParcelStatus.OutForDelivery, historyEntry);
        if (updated)
        {
            var assignment = await _assignmentRepository.GetActiveAssignmentForParcelAsync(parcelId);
            if (assignment != null)
            {
                assignment.Status = "OutForDelivery";
                await _assignmentRepository.UpdateAsync(assignment);
            }

            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = parcel.CustomerId,
                Title = "Out For Delivery 🛵",
                Message = $"Your parcel #{parcel.TrackingId} is out for final delivery today.",
                Type = NotificationType.StatusUpdate,
                ReferenceId = parcelId
            });
        }

        return updated;
    }

    public async Task<bool> ConfirmDeliveryAsync(string agentId, ConfirmDeliveryViewModel model)
    {
        var parcel = await ValidateAndAuthorizeAgentParcelAsync(agentId, model.ParcelId);

        // State Machine Rule: OutForDelivery / InTransit -> Delivered
        if (parcel.Status != ParcelStatus.OutForDelivery && parcel.Status != ParcelStatus.InTransit)
        {
            throw new InvalidOperationException($"Invalid state transition: Cannot mark parcel as Delivered when status is {parcel.Status}. Expected status: OutForDelivery.");
        }

        var historyEntry = new ParcelStatusHistory
        {
            Status = ParcelStatus.Delivered,
            Timestamp = DateTime.UtcNow,
            Location = parcel.DeliveryAddress.FormattedAddress,
            Notes = $"Successfully delivered to: {model.ReceivedBy}. {(string.IsNullOrWhiteSpace(model.DeliveryNotes) ? "" : "Notes: " + model.DeliveryNotes.Trim())}",
            UpdatedByUserId = agentId,
            UpdatedByRole = "DeliveryAgent"
        };

        var updated = await _parcelRepository.UpdateStatusAsync(parcel.Id, ParcelStatus.Delivered, historyEntry);
        if (updated)
        {
            // 1. Update Assignment Record
            var assignment = await _assignmentRepository.GetActiveAssignmentForParcelAsync(parcel.Id);
            if (assignment != null)
            {
                assignment.DeliveryTime = DateTime.UtcNow;
                assignment.CompletedAt = DateTime.UtcNow;
                assignment.DeliveredTo = model.ReceivedBy;
                assignment.SignatureBase64 = model.SignatureBase64;
                assignment.Notes = model.DeliveryNotes;
                assignment.Status = "Completed";
                await _assignmentRepository.UpdateAsync(assignment);
            }

            // 2. Create Archival Delivery Record Proof
            var proofRecord = new DeliveryRecord
            {
                ParcelId = parcel.Id,
                TrackingId = parcel.TrackingId,
                CustomerId = parcel.CustomerId,
                AgentId = agentId,
                ReceivedBy = model.ReceivedBy.Trim(),
                SignatureBase64 = model.SignatureBase64,
                DeliveryNotes = model.DeliveryNotes?.Trim(),
                CompletedAt = DateTime.UtcNow
            };
            await _deliveryRecordRepository.CreateAsync(proofRecord);

            // 3. Increment agent metrics and reset status: Busy -> Available
            await _userRepository.IncrementCompletedDeliveriesAsync(agentId);
            await _userRepository.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Available);

            // 4. Send Customer Notification
            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = parcel.CustomerId,
                Title = "Parcel Delivered! 🎉",
                Message = $"Your parcel #{parcel.TrackingId} was successfully delivered to {model.ReceivedBy}.",
                Type = NotificationType.Delivered,
                ReferenceId = parcel.Id
            });
        }

        return updated;
    }

    public async Task<bool> ReportDeliveryFailureAsync(string agentId, ReportFailureViewModel model)
    {
        var parcel = await ValidateAndAuthorizeAgentParcelAsync(agentId, model.ParcelId);

        // Cannot fail a parcel that is already Delivered or Cancelled
        if (parcel.Status == ParcelStatus.Delivered || parcel.Status == ParcelStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot record delivery failure for a parcel with terminal status {parcel.Status}.");
        }

        var historyEntry = new ParcelStatusHistory
        {
            Status = ParcelStatus.DeliveryFailed,
            Timestamp = DateTime.UtcNow,
            Location = parcel.DeliveryAddress.FormattedAddress,
            Notes = $"Delivery attempt failed: {model.Reason}. {model.Notes.Trim()}",
            UpdatedByUserId = agentId,
            UpdatedByRole = "DeliveryAgent"
        };

        var updated = await _parcelRepository.UpdateStatusAsync(parcel.Id, ParcelStatus.DeliveryFailed, historyEntry);
        if (updated)
        {
            // 1. Update Assignment Record
            var assignment = await _assignmentRepository.GetActiveAssignmentForParcelAsync(parcel.Id);
            if (assignment != null)
            {
                assignment.CompletedAt = DateTime.UtcNow;
                assignment.FailureReason = model.Reason;
                assignment.Notes = model.Notes.Trim();
                assignment.Status = "Failed";
                await _assignmentRepository.UpdateAsync(assignment);
            }

            // 2. Free agent availability status: Busy -> Available
            await _userRepository.UpdateAvailabilityStatusAsync(agentId, AgentAvailabilityStatus.Available);

            // 3. Customer Notification
            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = parcel.CustomerId,
                Title = "Delivery Attempt Notice ⚠️",
                Message = $"Delivery attempt for parcel #{parcel.TrackingId} failed: {model.Reason}. Support will contact you shortly.",
                Type = NotificationType.Alert,
                ReferenceId = parcel.Id
            });
        }

        return updated;
    }

    private async Task<Parcel> ValidateAndAuthorizeAgentParcelAsync(string agentId, string parcelId)
    {
        var parcel = await _parcelRepository.GetByIdAsync(parcelId);
        if (parcel == null)
        {
            throw new InvalidOperationException("Parcel shipment not found.");
        }

        if (parcel.AssignedAgentId != agentId)
        {
            throw new UnauthorizedAccessException("You are not authorized to perform operations on this parcel delivery.");
        }

        return parcel;
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        if (Math.Abs(lat1 - lat2) < 0.00001 && Math.Abs(lon1 - lon2) < 0.00001)
        {
            return 0.1;
        }

        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Max(0.1, earthRadiusKm * c);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
