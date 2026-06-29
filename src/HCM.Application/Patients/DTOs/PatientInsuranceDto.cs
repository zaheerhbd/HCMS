namespace HCM.Application.Patients.DTOs;

public class PatientInsuranceDto
{
    public Guid Id { get; set; }
    public string InsurancePlan { get; set; } = null!;
    public string MemberId { get; set; } = null!;
    public string? GroupNumber { get; set; }
    public string? SubscriberName { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsPrimary { get; set; }
}
