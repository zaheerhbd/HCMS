namespace HCM.Domain.Entities;

public class CaseCaseTag
{
    public Guid CaseId { get; set; }
    public int TagId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Case Case { get; set; } = null!;
    public virtual CaseTag Tag { get; set; } = null!;
}
