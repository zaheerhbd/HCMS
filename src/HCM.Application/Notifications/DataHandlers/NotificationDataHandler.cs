using HCM.Application.Interfaces;
using HCM.Application.Notifications.DTOs;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Notifications.DataHandlers;

public class NotificationDataHandler : INotificationDataHandler
{
    private readonly IApplicationDbContext _db;

    public NotificationDataHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task CreateAsync(
        Guid userId, string type, string title, string message,
        string? entityType = null, string? entityId = null, CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<NotificationListDto> GetMyNotificationsAsync(
        Guid userId, int limit = 20, CancellationToken ct = default)
    {
        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        var total = await query.CountAsync(ct);
        var unread = await query.CountAsync(n => !n.IsRead, ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => MapToDto(n))
            .ToListAsync(ct);

        return new NotificationListDto
        {
            TotalCount = total,
            UnreadCount = unread,
            Items = items
        };
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        var count = await _db.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

        return new UnreadCountDto { Count = count };
    }

    public async Task MarkReadAsync(Guid userId, long notificationId, CancellationToken ct = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification == null) return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        if (unread.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        EntityType = n.EntityType,
        EntityId = n.EntityId,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        ReadAt = n.ReadAt
    };
}
