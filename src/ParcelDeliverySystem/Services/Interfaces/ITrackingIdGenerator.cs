namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Service contract for generating unique, customer-friendly tracking identifiers.
/// Format: PDS-YYYYMMDD-XXXX
/// </summary>
public interface ITrackingIdGenerator
{
    Task<string> GenerateTrackingIdAsync();
}
