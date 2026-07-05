namespace HCM.Application.Notifications.DTOs;

public class NotificationDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class NotificationListDto
{
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public List<NotificationDto> Items { get; set; } = [];
}

public class UnreadCountDto
{
    public int Count { get; set; }
}
