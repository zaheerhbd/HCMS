using HCM.Application.CareTeam.DTOs;
using HCM.Application.CaseNotes.DTOs;

namespace HCM.Application.Cases.DTOs;

public class CaseDto
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } = null!;
    public Guid PatientId { get; set; }
    public int CaseTypeId { get; set; }
    public string CaseTypeName { get; set; } = null!;
    public string CurrentStatus { get; set; } = null!;
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CaseStatusHistoryDto> StatusHistory { get; set; } = [];
    public List<CareTeamMemberDto> CareTeam { get; set; } = [];
    public List<CaseNoteDto> Notes_Collection { get; set; } = [];
}
