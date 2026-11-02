using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Service interface for dispatching, querying, and managing in-app notifications.
/// </summary>
public interface INotificationService
{
    Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(string userId, int limit = 50);
    Task<long> GetUnreadCountAsync(string userId);
    Task<bool> MarkAsReadAsync(string notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<Notification> SendNotificationAsync(string userId, string title, string message, NotificationType type, string? referenceId = null);
}
