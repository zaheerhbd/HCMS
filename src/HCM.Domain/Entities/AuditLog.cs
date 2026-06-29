namespace HCM.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;   // Created | Updated | Deleted
    public string? OldValues { get; set; }               // JSON of changed fields before save
    public string? NewValues { get; set; }               // JSON of changed fields after save
    public string? ChangedBy { get; set; }               // User ID string — no FK (survives user deletion)
    public DateTime ChangedAt { get; set; }
}
