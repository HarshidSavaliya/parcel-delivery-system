namespace ParcelDeliverySystem.Models.Enums;

/// <summary>
/// Defines real-time availability states for delivery agents.
/// </summary>
public enum AgentAvailabilityStatus
{
    Offline = 0,
    Available = 1,
    Busy = 2,
    OnDelivery = 2
}
