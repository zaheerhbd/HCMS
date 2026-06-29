using HCM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HCM.Infrastructure.BackgroundJobs;

// Runs every hour and locks CaseNotes whose 24-hour edit window has expired
public class CaseNoteEditLockService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CaseNoteEditLockService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public CaseNoteEditLockService(IServiceScopeFactory scopeFactory, ILogger<CaseNoteEditLockService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await LockExpiredNotesAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    // Sets IsEditable = false on all notes older than 24 hours that are still marked editable
    private async Task LockExpiredNotesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff = DateTime.UtcNow.AddHours(-24);

            var updated = await db.CaseNotes
                .Where(n => n.IsEditable && !n.IsDeleted && n.CreatedAt <= cutoff)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsEditable, false), ct);

            if (updated > 0)
                _logger.LogInformation("Locked {Count} expired case notes.", updated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred while locking expired case notes.");
        }
    }
}
