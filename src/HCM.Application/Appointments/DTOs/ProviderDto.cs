namespace HCM.Application.Appointments.DTOs;

public class ProviderDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Specialty { get; set; }
    public bool IsActive { get; set; }
}
