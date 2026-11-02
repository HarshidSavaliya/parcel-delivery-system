using MongoDB.Driver;
using ParcelDeliverySystem.Data;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Repositories.Interfaces;

namespace ParcelDeliverySystem.Repositories.Implementations;

/// <summary>
/// MongoDB implementation of INotificationRepository.
/// </summary>
public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(IMongoDbContext context) : base(context.Notifications)
    {
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(string userId, int limit = 20)
    {
        return await _collection.Find(n => n.UserId == userId)
            .SortByDescending(n => n.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<long> GetUnreadCountAsync(string userId)
    {
        return await _collection.CountDocumentsAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(string notificationId, string userId)
    {
        var update = Builders<Notification>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadAt, DateTime.UtcNow)
            .Set(n => n.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(n => n.Id == notificationId && n.UserId == userId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var update = Builders<Notification>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadAt, DateTime.UtcNow)
            .Set(n => n.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateManyAsync(n => n.UserId == userId && !n.IsRead, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }
}
