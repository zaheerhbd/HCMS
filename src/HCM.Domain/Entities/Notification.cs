namespace HCM.Domain.Entities;

public class Notification
{
    public long Id { get; set; }

    // Who receives this notification
    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty;  // CaseAssigned | TaskAssigned | TaskOverdue | CaseEscalated | AppointmentReminder

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    // Optional deep-link: e.g. "CASE-2026-00001" or a task GUID
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
