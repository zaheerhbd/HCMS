namespace HCM.Application.Reports.DTOs;

public class CasesByStatusReportDto
{
    public List<CasesByStatusRowDto> Rows { get; set; } = [];
    public int TotalCases { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class CasesByStatusRowDto
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}
