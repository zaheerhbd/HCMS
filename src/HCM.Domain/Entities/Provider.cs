namespace HCM.Domain.Entities;

public class Provider
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Specialty { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; } = [];
}
