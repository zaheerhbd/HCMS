namespace HCM.Domain.Entities;

public class Case
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } = null!; // System-generated: CASE-YYYY-NNNNN
    public Guid PatientId { get; set; }
    public int CaseTypeId { get; set; }
    public string CurrentStatus { get; set; } = CaseStatus.Open;
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Patient Patient { get; set; } = null!;
    public virtual CaseType CaseType { get; set; } = null!;
    public virtual ICollection<CaseStatusHistory> StatusHistory { get; set; } = [];
    public virtual ICollection<CareTeamMember> CareTeam { get; set; } = [];
    public virtual ICollection<CaseNote> Notes_Collection { get; set; } = [];
    public virtual ICollection<CaseCaseTag> CaseTags { get; set; } = [];
}
