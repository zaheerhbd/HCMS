namespace HCM.Application.Reports.DTOs;

public class CaseDurationReportDto
{
    public List<CaseDurationRowDto> Rows { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class CaseDurationRowDto
{
    public string CaseType { get; set; } = "";
    public int CaseCount { get; set; }
    public double AverageDays { get; set; }
    public double MinDays { get; set; }
    public double MaxDays { get; set; }
}
