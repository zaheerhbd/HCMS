namespace HCM.Application.Cases.DTOs;

public class CaseStatusChangeDto
{
    public string NewStatus { get; set; } = null!;
    public string? Comment { get; set; }
}
