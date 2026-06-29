namespace HCM.Application.Patients.DTOs;

public class PatientSearchDto
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<PatientListItemDto> Results { get; set; } = [];
}

public class PatientListItemDto
{
    public Guid Id { get; set; }
    public string MRN { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
