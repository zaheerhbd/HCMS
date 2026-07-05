namespace HCM.Application.Dashboard.DTOs;

public class SupervisorDashboardDto
{
    public int TotalActiveCases { get; set; }
    public int EscalatedCasesCount { get; set; }
    public int InactiveCasesCount { get; set; }
    public List<UserCaseloadDto> TeamCaseloads { get; set; } = [];
    public List<EscalatedCaseDto> EscalatedCases { get; set; } = [];
    public List<InactiveCaseDto> InactiveCases { get; set; } = [];
}

public class UserCaseloadDto
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public int OpenCases { get; set; }
    public int OverdueTasks { get; set; }
}

public class EscalatedCaseDto
{
    public string CaseNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string AssignedTo { get; set; } = "";
    public DateTime EscalatedAt { get; set; }
}

public class InactiveCaseDto
{
    public string CaseNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string AssignedTo { get; set; } = "";
    public DateTime LastActivityAt { get; set; }
    public int DaysSinceActivity { get; set; }
}
