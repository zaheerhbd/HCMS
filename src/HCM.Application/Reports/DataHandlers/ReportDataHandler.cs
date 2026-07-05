using HCM.Application.Interfaces;
using HCM.Application.Reports.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text;
// EF.Functions.DateDiffDay is SQL-Server-specific and lives in Infrastructure, so duration is computed client-side.

namespace HCM.Application.Reports.DataHandlers;

public class ReportDataHandler : IReportDataHandler
{
    private readonly IApplicationDbContext _db;

    public ReportDataHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    // Counts all active cases grouped by status.
    public async Task<CasesByStatusReportDto> GetCasesByStatusAsync(CancellationToken ct = default)
    {
        var rows = await _db.Cases
            .Where(c => c.IsActive)
            .GroupBy(c => c.CurrentStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = rows.Sum(r => r.Count);

        return new CasesByStatusReportDto
        {
            TotalCases = total,
            Rows = rows.Select(r => new CasesByStatusRowDto
            {
                Status = r.Status,
                Count = r.Count,
                Percentage = total == 0 ? 0 : Math.Round((double)r.Count / total * 100, 1)
            }).OrderByDescending(r => r.Count).ToList()
        };
    }

    // Calculates average open-to-close duration in days per case type.
    public async Task<CaseDurationReportDto> GetCaseDurationAsync(CancellationToken ct = default)
    {
        var closed = await _db.Cases
            .Include(c => c.CaseType)
            .Where(c => c.IsActive && c.CurrentStatus == "Closed" && c.ClosedAt != null)
            .Select(c => new
            {
                CaseType = c.CaseType != null ? c.CaseType.Name : "Unknown",
                CreatedAt = c.CreatedAt,
                ClosedAt  = c.ClosedAt!.Value
            })
            .ToListAsync(ct);

        // Compute duration in days client-side to avoid SQL Server-specific functions in Application layer.
        var closedWithDuration = closed.Select(c => new
        {
            c.CaseType,
            DurationDays = (int)(c.ClosedAt - c.CreatedAt).TotalDays
        }).ToList();

        var rows = closedWithDuration
            .GroupBy(c => c.CaseType)
            .Select(g => new CaseDurationRowDto
            {
                CaseType = g.Key,
                CaseCount = g.Count(),
                AverageDays = Math.Round(g.Average(x => (double)x.DurationDays), 1),
                MinDays = g.Min(x => x.DurationDays),
                MaxDays = g.Max(x => x.DurationDays)
            })
            .OrderBy(r => r.AverageDays)
            .ToList();

        return new CaseDurationReportDto { Rows = rows };
    }

    // Returns task completion breakdown per assignee.
    public async Task<TasksByAssigneeReportDto> GetTasksByAssigneeAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var tasks = await _db.CaseTasks
            .Include(t => t.AssignedToUser)
            .Where(t => t.AssignedTo != null)
            .Select(t => new
            {
                UserId = t.AssignedTo!.Value,
                UserName = t.AssignedToUser != null ? t.AssignedToUser.Username : "Unknown",
                t.Status,
                IsOverdue = t.DueDate != null && t.DueDate < now && t.Status != "Done" && t.Status != "Cancelled"
            })
            .ToListAsync(ct);

        var rows = tasks
            .GroupBy(t => new { t.UserId, t.UserName })
            .Select(g => new TasksByAssigneeRowDto
            {
                UserId = g.Key.UserId.ToString(),
                UserName = g.Key.UserName,
                TotalTasks = g.Count(),
                DoneTasks = g.Count(t => t.Status == "Done"),
                OverdueTasks = g.Count(t => t.IsOverdue),
                OpenTasks = g.Count(t => t.Status == "Todo"),
                InProgressTasks = g.Count(t => t.Status == "InProgress")
            })
            .OrderByDescending(r => r.TotalTasks)
            .ToList();

        return new TasksByAssigneeReportDto { Rows = rows };
    }

    // CSV export helpers — build RFC 4180 compliant CSV strings.
    public async Task<string> GetCasesByStatusCsvAsync(CancellationToken ct = default)
    {
        var data = await GetCasesByStatusAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("Status,Count,Percentage");
        foreach (var r in data.Rows)
            sb.AppendLine($"{EscapeCsv(r.Status)},{r.Count},{r.Percentage}%");
        return sb.ToString();
    }

    public async Task<string> GetCaseDurationCsvAsync(CancellationToken ct = default)
    {
        var data = await GetCaseDurationAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("Case Type,Case Count,Average Days,Min Days,Max Days");
        foreach (var r in data.Rows)
            sb.AppendLine($"{EscapeCsv(r.CaseType)},{r.CaseCount},{r.AverageDays},{r.MinDays},{r.MaxDays}");
        return sb.ToString();
    }

    public async Task<string> GetTasksByAssigneeCsvAsync(CancellationToken ct = default)
    {
        var data = await GetTasksByAssigneeAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("User,Total Tasks,Done,Overdue,Open,In Progress");
        foreach (var r in data.Rows)
            sb.AppendLine($"{EscapeCsv(r.UserName)},{r.TotalTasks},{r.DoneTasks},{r.OverdueTasks},{r.OpenTasks},{r.InProgressTasks}");
        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
