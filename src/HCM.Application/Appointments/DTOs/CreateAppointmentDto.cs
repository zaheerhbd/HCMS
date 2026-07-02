namespace HCM.Application.Appointments.DTOs;

public class CreateAppointmentDto
{
    public string PatientMrn { get; set; } = null!;
    public string? CaseNumber { get; set; }
    public Guid ProviderId { get; set; }
    public string AppointmentType { get; set; } = null!;
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}
