namespace ParcelDeliverySystem.Models.Enums;

/// <summary>
/// Status of payment for parcel delivery services.
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}
