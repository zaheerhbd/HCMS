namespace HCM.Application.Cases.DTOs;

public class CaseListItemDto
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } = null!;
    public int CaseTypeId { get; set; }
    public string CaseTypeName { get; set; } = null!;
    public string CurrentStatus { get; set; } = null!;
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? AssignedToUserName { get; set; }
}
