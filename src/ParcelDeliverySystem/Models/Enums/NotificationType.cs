namespace ParcelDeliverySystem.Models.Enums;

/// <summary>
/// Categorization of in-app and system notifications.
/// </summary>
public enum NotificationType
{
    General = 0,
    BookingConfirmation = 1,
    AgentAssigned = 2,
    StatusUpdate = 3,
    Delivered = 4,
    Alert = 5
}
