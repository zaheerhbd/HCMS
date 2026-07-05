using HCM.Application.Interfaces;
using HCM.Application.Notifications.DataHandlers;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HCM.Infrastructure.BackgroundServices;

// Runs once a day. Scans for tasks that passed their due date without being completed
// and creates a TaskOverdue notification for the assigned user.
public class NotificationOverdueService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationOverdueService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public NotificationOverdueService(IServiceProvider services, ILogger<NotificationOverdueService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger the first run to avoid hammering DB on startup
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOverdueTasksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationOverdueService encountered an error");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CheckOverdueTasksAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationDataHandler>();

        var now = DateTime.UtcNow.Date;

        // Find overdue, incomplete tasks that have an assignee
        var overdueTasks = await db.CaseTasks
            .AsNoTracking()
            .Include(t => t.Case)
            .Where(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value.Date < now &&
                t.AssignedTo.HasValue &&
                t.Status != CaseTaskStatus.Done &&
                t.Status != CaseTaskStatus.Cancelled)
            .ToListAsync(ct);

        foreach (var task in overdueTasks)
        {
            // Avoid duplicate notifications: skip if we already sent one today
            var alreadyNotified = await db.Notifications
                .AsNoTracking()
                .AnyAsync(n =>
                    n.UserId == task.AssignedTo!.Value &&
                    n.Type == "TaskOverdue" &&
                    n.EntityId == task.Id.ToString() &&
                    n.CreatedAt.Date == now, ct);

            if (alreadyNotified) continue;

            var caseNumber = task.Case?.CaseNumber ?? "unknown";

            await notifications.CreateAsync(
                userId: task.AssignedTo!.Value,
                type: "TaskOverdue",
                title: "Task overdue",
                message: $"Task \"{task.Title}\" on case {caseNumber} was due {task.DueDate!.Value:yyyy-MM-dd} and is still open.",
                entityType: "Task",
                entityId: task.Id.ToString(),
                ct: ct);
        }

        _logger.LogInformation("NotificationOverdueService: processed {Count} overdue tasks", overdueTasks.Count);
    }
}
