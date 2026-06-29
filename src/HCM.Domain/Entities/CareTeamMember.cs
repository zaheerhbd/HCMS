namespace HCM.Domain.Entities;

public class CareTeamMember
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid UserId { get; set; }
    public string TeamRole { get; set; } = null!; // Lead, Clinician, Specialist, Support
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Case Case { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
