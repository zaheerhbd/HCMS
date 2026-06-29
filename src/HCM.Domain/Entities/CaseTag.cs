namespace HCM.Domain.Entities;

public class CaseTag
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CaseCaseTag> Cases { get; set; } = [];
}
