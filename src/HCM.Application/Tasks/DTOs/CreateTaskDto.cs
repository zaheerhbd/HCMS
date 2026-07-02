namespace HCM.Application.Tasks.DTOs;

public class CreateTaskDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
    public Guid? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
}
