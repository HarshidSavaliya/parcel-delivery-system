namespace ParcelDeliverySystem.Models.Enums;

/// <summary>
/// Lifecycle states for parcels in the delivery pipeline.
/// </summary>
public enum ParcelStatus
{
    Booked = 0,
    PendingPickup = 1,
    AgentAssigned = 2,
    PickedUp = 3,
    InTransit = 4,
    OutForDelivery = 5,
    Delivered = 6,
    Cancelled = 7,
    FailedAttempt = 8,
    DeliveryFailed = 8
}
