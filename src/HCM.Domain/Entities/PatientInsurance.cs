namespace HCM.Domain.Entities;

public class PatientInsurance
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string InsurancePlan { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string? GroupNumber { get; set; }
    public string? SubscriberName { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsPrimary { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Patient Patient { get; set; } = null!;
}
