namespace HCM.Application.Tasks.DTOs;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public Guid? AssignedTo { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CaseNumber { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
