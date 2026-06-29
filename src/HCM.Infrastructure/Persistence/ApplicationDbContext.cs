using System.Security.Claims;
using System.Text.Json;
using HCM.Application.Interfaces;
using HCM.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HCM.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    // IHttpContextAccessor is nullable to support design-time factory (migrations)
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientInsurance> PatientInsurances => Set<PatientInsurance>();
    public DbSet<CaseType> CaseTypes => Set<CaseType>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseStatusHistory> CaseStatusHistories => Set<CaseStatusHistory>();
    public DbSet<CaseTag> CaseTags => Set<CaseTag>();
    public DbSet<CaseCaseTag> CaseCaseTags => Set<CaseCaseTag>();
    public DbSet<CareTeamMember> CareTeamMembers => Set<CareTeamMember>();
    public DbSet<CaseNote> CaseNotes => Set<CaseNote>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // Intercept all saves to write audit entries for tracked entity types
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = BuildAuditEntries();
        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    // Build AuditLog rows from ChangeTracker before the underlying save runs
    private List<AuditLog> BuildAuditEntries()
    {
        var auditedTypes = new HashSet<Type> { typeof(Patient), typeof(Case), typeof(CaseNote), typeof(User) };
        var changedBy = _httpContextAccessor?.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var now = DateTime.UtcNow;
        var entries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries()
            .Where(e => auditedTypes.Contains(e.Entity.GetType()) &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var entityId = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "unknown";

            string? oldValues = null;
            string? newValues = null;

            if (entry.State == EntityState.Modified)
            {
                var changed = entry.Properties.Where(p => p.IsModified).ToList();
                oldValues = JsonSerializer.Serialize(
                    changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString()));
                newValues = JsonSerializer.Serialize(
                    changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString()));
            }
            else if (entry.State == EntityState.Added)
            {
                newValues = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString()));
            }
            else if (entry.State == EntityState.Deleted)
            {
                oldValues = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString()));
            }

            entries.Add(new AuditLog
            {
                EntityType = entry.Entity.GetType().Name,
                EntityId = entityId,
                Action = entry.State switch
                {
                    EntityState.Added => "Created",
                    EntityState.Modified => "Updated",
                    EntityState.Deleted => "Deleted",
                    _ => "Unknown"
                },
                OldValues = oldValues,
                NewValues = newValues,
                ChangedBy = changedBy,
                ChangedAt = now
            });
        }

        return entries;
    }
}
