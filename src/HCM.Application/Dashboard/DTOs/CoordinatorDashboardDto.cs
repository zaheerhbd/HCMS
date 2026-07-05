namespace HCM.Application.Dashboard.DTOs;

public class CoordinatorDashboardDto
{
    public int OpenCasesCount { get; set; }
    public int OverdueTasksCount { get; set; }
    public int TodayAppointmentsCount { get; set; }
    public int InactiveCasesCount { get; set; }
    public List<CaseStatusCountDto> CasesByStatus { get; set; } = [];
    public List<OverdueTaskSummaryDto> OverdueTasks { get; set; } = [];
    public List<TodayAppointmentDto> TodayAppointments { get; set; } = [];
}

public class CaseStatusCountDto
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public class OverdueTaskSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string CaseNumber { get; set; } = "";
    public string Priority { get; set; } = "";
    public DateTime DueDate { get; set; }
}

public class TodayAppointmentDto
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = "";
    public string MRN { get; set; } = "";
    public DateTime ScheduledStart { get; set; }
    public string AppointmentType { get; set; } = "";
    public string Status { get; set; } = "";
}
