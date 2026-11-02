using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.Repositories.Interfaces;

/// <summary>
/// Notification repository interface for managing user alerts and notifications.
/// </summary>
public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(string userId, int limit = 20);
    Task<long> GetUnreadCountAsync(string userId);
    Task<bool> MarkAsReadAsync(string notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
}
