namespace HCM.Application.Appointments.DTOs;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = null!;
    public string PatientMrn { get; set; } = null!;
    public Guid? CaseId { get; set; }
    public string? CaseNumber { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = null!;
    public string AppointmentType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
