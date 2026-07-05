namespace HCM.Application.Dashboard.DTOs;

public class AdminDashboardDto
{
    public int TotalActiveUsers { get; set; }
    public int TotalPatients { get; set; }
    public int TotalCases { get; set; }
    public int OpenCases { get; set; }
    public int LoginsLast7Days { get; set; }
    public int FhirImportsLast30Days { get; set; }
    public List<DailyLoginCountDto> LoginTrend { get; set; } = [];
    public List<RoleUserCountDto> UsersByRole { get; set; } = [];
}

public class DailyLoginCountDto
{
    public string Date { get; set; } = "";
    public int Count { get; set; }
}

public class RoleUserCountDto
{
    public string Role { get; set; } = "";
    public int Count { get; set; }
}
