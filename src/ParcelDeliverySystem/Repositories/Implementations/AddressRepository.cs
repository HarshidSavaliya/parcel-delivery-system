using MongoDB.Driver;
using ParcelDeliverySystem.Data;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Repositories.Interfaces;

namespace ParcelDeliverySystem.Repositories.Implementations;

/// <summary>
/// MongoDB implementation of IAddressRepository.
/// </summary>
public class AddressRepository : BaseRepository<Address>, IAddressRepository
{
    public AddressRepository(IMongoDbContext context) : base(context.Addresses)
    {
    }

    public async Task<IReadOnlyList<Address>> GetByUserIdAsync(string userId)
    {
        return await _collection.Find(a => a.UserId == userId).ToListAsync();
    }

    public async Task<Address?> GetDefaultByUserIdAsync(string userId)
    {
        return await _collection.Find(a => a.UserId == userId && a.IsDefault).FirstOrDefaultAsync();
    }

    public async Task ClearDefaultAsync(string userId)
    {
        var update = Builders<Address>.Update.Set(a => a.IsDefault, false);
        await _collection.UpdateManyAsync(a => a.UserId == userId, update);
    }

    public async Task SetAsDefaultAsync(string userId, string addressId)
    {
        // First clear all existing defaults for user
        await ClearDefaultAsync(userId);

        // Then set the selected address as default
        var update = Builders<Address>.Update
            .Set(a => a.IsDefault, true)
            .Set(a => a.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(a => a.Id == addressId && a.UserId == userId, update);
    }
}
