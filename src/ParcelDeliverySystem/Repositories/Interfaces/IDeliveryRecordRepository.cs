using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.Repositories.Interfaces;

/// <summary>
/// Repository interface for persisting and querying delivery confirmation records and proof documents.
/// </summary>
public interface IDeliveryRecordRepository : IRepository<DeliveryRecord>
{
    Task<DeliveryRecord?> GetByParcelIdAsync(string parcelId);
    Task<DeliveryRecord?> GetByTrackingIdAsync(string trackingId);
}
