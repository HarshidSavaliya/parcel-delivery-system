using MongoDB.Driver;
using ParcelDeliverySystem.Data;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;

namespace ParcelDeliverySystem.Repositories.Implementations;

/// <summary>
/// MongoDB implementation of IParcelRepository.
/// </summary>
public class ParcelRepository : BaseRepository<Parcel>, IParcelRepository
{
    public ParcelRepository(IMongoDbContext context) : base(context.Parcels)
    {
    }

    public async Task<Parcel?> GetByTrackingIdAsync(string trackingId)
    {
        if (string.IsNullOrWhiteSpace(trackingId)) return null;
        var normalized = trackingId.Trim().ToUpperInvariant();
        return await _collection.Find(p => p.TrackingId == normalized).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Parcel>> GetByCustomerIdAsync(string customerId)
    {
        return await _collection.Find(p => p.CustomerId == customerId)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Parcel>> GetByAgentIdAsync(string agentId)
    {
        return await _collection.Find(p => p.AssignedAgentId == agentId)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Parcel>> GetAvailableParcelsForPickupAsync()
    {
        return await _collection.Find(p => 
            (p.Status == ParcelStatus.Booked || p.Status == ParcelStatus.PendingPickup) && 
            p.AssignedAgentId == null
        ).SortByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<IReadOnlyList<Parcel>> GetRecentParcelsAsync(int limit = 10)
    {
        return await _collection.Find(FilterDefinition<Parcel>.Empty)
            .SortByDescending(p => p.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<bool> UpdateStatusAsync(string parcelId, ParcelStatus newStatus, ParcelStatusHistory historyEntry)
    {
        var update = Builders<Parcel>.Update
            .Set(p => p.Status, newStatus)
            .Set(p => p.UpdatedAt, DateTime.UtcNow)
            .Push(p => p.StatusHistory, historyEntry);

        if (newStatus == ParcelStatus.PickedUp)
            update = update.Set(p => p.PickedUpAt, DateTime.UtcNow);

        if (newStatus == ParcelStatus.Delivered)
            update = update.Set(p => p.DeliveredAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(p => p.Id == parcelId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> AssignAgentAsync(string parcelId, string agentId)
    {
        var update = Builders<Parcel>.Update
            .Set(p => p.AssignedAgentId, agentId)
            .Set(p => p.Status, ParcelStatus.AgentAssigned)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(p => p.Id == parcelId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> AssignAgentSafelyAsync(string parcelId, string agentId, ParcelStatusHistory historyEntry)
    {
        var filter = Builders<Parcel>.Filter.And(
            Builders<Parcel>.Filter.Eq(p => p.Id, parcelId),
            Builders<Parcel>.Filter.Or(
                Builders<Parcel>.Filter.Eq(p => p.Status, ParcelStatus.Booked),
                Builders<Parcel>.Filter.Eq(p => p.Status, ParcelStatus.PendingPickup)
            ),
            Builders<Parcel>.Filter.Eq(p => p.AssignedAgentId, null)
        );

        var update = Builders<Parcel>.Update
            .Set(p => p.AssignedAgentId, agentId)
            .Set(p => p.Status, ParcelStatus.AgentAssigned)
            .Set(p => p.UpdatedAt, DateTime.UtcNow)
            .Push(p => p.StatusHistory, historyEntry);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<long> CountByStatusAsync(ParcelStatus status)
    {
        return await _collection.CountDocumentsAsync(p => p.Status == status);
    }
}
