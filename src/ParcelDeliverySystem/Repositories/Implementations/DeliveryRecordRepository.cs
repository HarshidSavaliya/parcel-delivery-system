using MongoDB.Driver;
using ParcelDeliverySystem.Data;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Repositories.Interfaces;

namespace ParcelDeliverySystem.Repositories.Implementations;

/// <summary>
/// MongoDB implementation of IDeliveryRecordRepository.
/// </summary>
public class DeliveryRecordRepository : BaseRepository<DeliveryRecord>, IDeliveryRecordRepository
{
    public DeliveryRecordRepository(IMongoDbContext context) : base(context.DeliveryRecords)
    {
    }

    public async Task<DeliveryRecord?> GetByParcelIdAsync(string parcelId)
    {
        return await _collection.Find(r => r.ParcelId == parcelId).FirstOrDefaultAsync();
    }

    public async Task<DeliveryRecord?> GetByTrackingIdAsync(string trackingId)
    {
        return await _collection.Find(r => r.TrackingId == trackingId).FirstOrDefaultAsync();
    }
}
