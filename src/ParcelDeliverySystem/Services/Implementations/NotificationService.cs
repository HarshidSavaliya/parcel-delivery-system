using Microsoft.Extensions.Logging;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Service implementation handling user notification delivery, badge counting, and read states.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(string userId, int limit = 50)
    {
        return await _notificationRepository.GetByUserIdAsync(userId, limit);
    }

    public async Task<long> GetUnreadCountAsync(string userId)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId);
    }

    public async Task<bool> MarkAsReadAsync(string notificationId, string userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId)
        {
            return false;
        }

        return await _notificationRepository.MarkAsReadAsync(notificationId, userId);
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        return await _notificationRepository.MarkAllAsReadAsync(userId);
    }

    public async Task<Notification> SendNotificationAsync(string userId, string title, string message, NotificationType type, string? referenceId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _notificationRepository.CreateAsync(notification);
        _logger.LogInformation("Notification created for User {UserId}: {Title}", userId, title);
        return created;
    }
}
