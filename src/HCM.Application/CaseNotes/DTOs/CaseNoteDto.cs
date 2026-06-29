namespace HCM.Application.CaseNotes.DTOs;

public class CaseNoteDto
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByUserName { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsEditable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
