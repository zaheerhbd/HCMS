namespace HCM.Domain.Entities;

public class CaseStatusHistory
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = null!;
    public string? Comment { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Case Case { get; set; } = null!;
}
