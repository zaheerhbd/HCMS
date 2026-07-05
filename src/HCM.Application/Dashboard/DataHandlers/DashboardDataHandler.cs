using HCM.Application.Dashboard.DTOs;
using HCM.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Dashboard.DataHandlers;

public class DashboardDataHandler : IDashboardDataHandler
{
    private readonly IApplicationDbContext _db;

    public DashboardDataHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    // Returns dashboard data for a coordinator: their open cases, overdue tasks, today's appointments.
    public async Task<CoordinatorDashboardDto> GetCoordinatorDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var inactiveThreshold = now.AddDays(-7);
        var openStatuses = new[] { "Open", "InProgress", "PendingReview", "Draft" };

        var myCaseCount = await _db.Cases
            .Where(c => c.IsActive && c.AssignedToUserId == userId)
            .CountAsync(ct);

        var casesByStatus = await _db.Cases
            .Where(c => c.IsActive && c.AssignedToUserId == userId)
            .GroupBy(c => c.CurrentStatus)
            .Select(g => new CaseStatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var overdueTasks = await _db.CaseTasks
            .Include(t => t.Case)
            .Where(t =>
                t.AssignedTo == userId &&
                t.DueDate < now &&
                t.Status != "Done" &&
                t.Status != "Cancelled")
            .OrderBy(t => t.DueDate)
            .Take(10)
            .Select(t => new OverdueTaskSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                CaseNumber = t.Case != null ? t.Case.CaseNumber : "",
                Priority = t.Priority,
                DueDate = t.DueDate!.Value
            })
            .ToListAsync(ct);

        var todayAppts = await _db.Appointments
            .Include(a => a.Patient)
            .Where(a =>
                a.ScheduledStart >= today &&
                a.ScheduledStart < tomorrow &&
                a.Status != "Cancelled")
            .OrderBy(a => a.ScheduledStart)
            .Take(10)
            .Select(a => new TodayAppointmentDto
            {
                Id = a.Id,
                PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                MRN = a.Patient.MRN,
                ScheduledStart = a.ScheduledStart,
                AppointmentType = a.AppointmentType,
                Status = a.Status
            })
            .ToListAsync(ct);

        var inactiveCount = await _db.Cases
            .Where(c =>
                c.IsActive &&
                c.AssignedToUserId == userId &&
                openStatuses.Contains(c.CurrentStatus) &&
                c.UpdatedAt < inactiveThreshold)
            .CountAsync(ct);

        return new CoordinatorDashboardDto
        {
            OpenCasesCount = myCaseCount,
            OverdueTasksCount = overdueTasks.Count,
            TodayAppointmentsCount = todayAppts.Count,
            InactiveCasesCount = inactiveCount,
            CasesByStatus = casesByStatus,
            OverdueTasks = overdueTasks,
            TodayAppointments = todayAppts
        };
    }

    // Returns dashboard data for a supervisor: team caseloads, escalated cases, inactive cases.
    public async Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var inactiveThreshold = now.AddDays(-7);
        var openStatuses = new[] { "Open", "InProgress", "PendingReview", "Draft" };

        var totalActive = await _db.Cases
            .Where(c => c.IsActive && openStatuses.Contains(c.CurrentStatus))
            .CountAsync(ct);

        // Load open cases with assignee info
        var activeCases = await _db.Cases
            .Where(c => c.IsActive && openStatuses.Contains(c.CurrentStatus) && c.AssignedToUserId != null)
            .Select(c => new { c.AssignedToUserId })
            .ToListAsync(ct);

        var assigneeIds = activeCases
            .Select(c => c.AssignedToUserId!.Value)
            .Distinct()
            .ToList();

        var assigneeNames = await _db.Users
            .Where(u => assigneeIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Username })
            .ToDictionaryAsync(u => u.Id, u => u.Username, ct);

        var overdueCounts = await _db.CaseTasks
            .Where(t =>
                t.AssignedTo != null &&
                t.DueDate < now &&
                t.Status != "Done" &&
                t.Status != "Cancelled")
            .GroupBy(t => t.AssignedTo)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var caseloads = activeCases
            .GroupBy(c => c.AssignedToUserId!.Value)
            .Select(g =>
            {
                var uid = g.Key;
                var overdue = overdueCounts.FirstOrDefault(x => x.UserId == uid)?.Count ?? 0;
                return new UserCaseloadDto
                {
                    UserId = uid.ToString(),
                    UserName = assigneeNames.GetValueOrDefault(uid, "Unknown"),
                    OpenCases = g.Count(),
                    OverdueTasks = overdue
                };
            })
            .OrderByDescending(x => x.OpenCases)
            .ToList();

        var escalated = await _db.Cases
            .Include(c => c.Patient)
            .Where(c => c.IsActive && c.CurrentStatus == "Escalated")
            .OrderByDescending(c => c.UpdatedAt)
            .Take(20)
            .ToListAsync(ct);

        var escalatedWithUser = new List<EscalatedCaseDto>();
        foreach (var c in escalated)
        {
            var assignee = c.AssignedToUserId.HasValue
                ? assigneeNames.GetValueOrDefault(c.AssignedToUserId.Value, "Unknown")
                : "Unassigned";
            escalatedWithUser.Add(new EscalatedCaseDto
            {
                CaseNumber = c.CaseNumber,
                PatientName = c.Patient.FirstName + " " + c.Patient.LastName,
                AssignedTo = assignee,
                EscalatedAt = c.UpdatedAt
            });
        }

        var inactiveCases = await _db.Cases
            .Include(c => c.Patient)
            .Where(c =>
                c.IsActive &&
                openStatuses.Contains(c.CurrentStatus) &&
                c.UpdatedAt < inactiveThreshold)
            .OrderBy(c => c.UpdatedAt)
            .Take(20)
            .ToListAsync(ct);

        var inactiveWithUser = inactiveCases.Select(c => new InactiveCaseDto
        {
            CaseNumber = c.CaseNumber,
            PatientName = c.Patient.FirstName + " " + c.Patient.LastName,
            AssignedTo = c.AssignedToUserId.HasValue
                ? assigneeNames.GetValueOrDefault(c.AssignedToUserId.Value, "Unknown")
                : "Unassigned",
            LastActivityAt = c.UpdatedAt,
            DaysSinceActivity = (int)(now - c.UpdatedAt).TotalDays
        }).ToList();

        return new SupervisorDashboardDto
        {
            TotalActiveCases = totalActive,
            EscalatedCasesCount = escalatedWithUser.Count,
            InactiveCasesCount = inactiveWithUser.Count,
            TeamCaseloads = caseloads,
            EscalatedCases = escalatedWithUser,
            InactiveCases = inactiveWithUser
        };
    }

    // Returns admin-level metrics: user counts, logins, FHIR imports.
    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);

        var activeUsers = await _db.Users.CountAsync(u => u.IsActive, ct);
        var totalPatients = await _db.Patients.CountAsync(p => p.IsActive, ct);
        var totalCases = await _db.Cases.CountAsync(c => c.IsActive, ct);
        var openCases = await _db.Cases
            .CountAsync(c => c.IsActive && c.CurrentStatus != "Closed" && c.CurrentStatus != "Cancelled", ct);

        var loginsLast7 = await _db.RefreshTokens
            .Where(t => t.CreatedAt >= sevenDaysAgo)
            .Select(t => t.UserId)
            .Distinct()
            .CountAsync(ct);

        var fhirImports = await _db.FhirImportLogs
            .CountAsync(f => f.ImportedAt >= thirtyDaysAgo, ct);

        // Daily login trend — one query per day (7 days; acceptable for a dashboard)
        var loginTrend = new List<DailyLoginCountDto>();
        for (int i = 6; i >= 0; i--)
        {
            var day = now.Date.AddDays(-i);
            var dayEnd = day.AddDays(1);
            var count = await _db.RefreshTokens
                .Where(t => t.CreatedAt >= day && t.CreatedAt < dayEnd)
                .Select(t => t.UserId)
                .Distinct()
                .CountAsync(ct);
            loginTrend.Add(new DailyLoginCountDto
            {
                Date = day.ToString("MMM dd"),
                Count = count
            });
        }

        var usersByRole = await _db.UserRoles
            .Include(ur => ur.Role)
            .GroupBy(ur => ur.Role.Name)
            .Select(g => new RoleUserCountDto { Role = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new AdminDashboardDto
        {
            TotalActiveUsers = activeUsers,
            TotalPatients = totalPatients,
            TotalCases = totalCases,
            OpenCases = openCases,
            LoginsLast7Days = loginsLast7,
            FhirImportsLast30Days = fhirImports,
            LoginTrend = loginTrend,
            UsersByRole = usersByRole
        };
    }
}
