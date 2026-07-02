namespace HCM.Domain.Entities;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? CaseId { get; set; }
    public Guid ProviderId { get; set; }
    public string AppointmentType { get; set; } = null!;
    public string Status { get; set; } = AppointmentStatus.Scheduled;
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Patient Patient { get; set; } = null!;
    public virtual Case? Case { get; set; }
    public virtual Provider Provider { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
}
