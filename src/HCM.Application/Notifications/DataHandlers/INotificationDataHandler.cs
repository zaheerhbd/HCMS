using HCM.Application.Notifications.DTOs;

namespace HCM.Application.Notifications.DataHandlers;

public interface INotificationDataHandler
{
    // Creates a notification for a specific user
    Task CreateAsync(Guid userId, string type, string title, string message,
        string? entityType = null, string? entityId = null, CancellationToken ct = default);

    // Returns latest 20 notifications for the current user
    Task<NotificationListDto> GetMyNotificationsAsync(Guid userId, int limit = 20, CancellationToken ct = default);

    // Returns unread count only (used by bell polling)
    Task<UnreadCountDto> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    // Marks a single notification as read
    Task MarkReadAsync(Guid userId, long notificationId, CancellationToken ct = default);

    // Marks all notifications for the user as read
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}
