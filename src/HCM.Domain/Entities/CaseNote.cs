namespace HCM.Domain.Entities;

public class CaseNote
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid CreatedBy { get; set; }
    public string Content { get; set; } = null!;
    public bool IsEditable { get; set; } = true; // Set to false after 24h by background job
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Case Case { get; set; } = null!;
}
