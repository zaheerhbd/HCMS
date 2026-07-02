namespace HCM.Domain.Entities;

public class CaseTask
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = CaseTaskStatus.Todo;
    public string Priority { get; set; } = "Medium";
    public Guid? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedBy { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Case Case { get; set; } = null!;
    public virtual User? AssignedToUser { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
}
