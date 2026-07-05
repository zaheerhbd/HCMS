namespace HCM.Application.Reports.DTOs;

public class TasksByAssigneeReportDto
{
    public List<TasksByAssigneeRowDto> Rows { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class TasksByAssigneeRowDto
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public int TotalTasks { get; set; }
    public int DoneTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int OpenTasks { get; set; }
    public int InProgressTasks { get; set; }
}
