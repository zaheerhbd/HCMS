using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Patient> Patients { get; }
    DbSet<PatientInsurance> PatientInsurances { get; }
    DbSet<CaseType> CaseTypes { get; }
    DbSet<Case> Cases { get; }
    DbSet<CaseStatusHistory> CaseStatusHistories { get; }
    DbSet<CaseTag> CaseTags { get; }
    DbSet<CaseCaseTag> CaseCaseTags { get; }
    DbSet<CareTeamMember> CareTeamMembers { get; }
    DbSet<CaseNote> CaseNotes { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<CaseTask> CaseTasks { get; }
    DbSet<Document> Documents { get; }
    DbSet<Provider> Providers { get; }
    DbSet<Appointment> Appointments { get; }

    DbSet<FhirImportLog> FhirImportLogs { get; }

    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
