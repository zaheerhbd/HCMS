namespace HCM.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; }
    public string MRN { get; set; } = null!; // System-generated: MRN-YYYY-NNNNN
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; } // Male, Female, NonBinary, Prefer Not to Say
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<PatientInsurance> Insurance { get; set; } = [];
    public virtual ICollection<Case> Cases { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";
}
