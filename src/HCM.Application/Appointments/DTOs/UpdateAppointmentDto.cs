namespace HCM.Application.Appointments.DTOs;

public class UpdateAppointmentDto
{
    public string? AppointmentType { get; set; }
    public DateTime? ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}
