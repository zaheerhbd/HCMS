using HCM.Application.Appointments.DataHandlers;
using HCM.Application.Audit.DataHandlers;
using HCM.Application.Dashboard.DataHandlers;
using HCM.Application.Reports.DataHandlers;
using HCM.Application.Auth.DataHandlers;
using HCM.Application.CareTeam.DataHandlers;
using HCM.Application.CaseNotes.DataHandlers;
using HCM.Application.Cases.DataHandlers;
using HCM.Application.Documents.DataHandlers;
using HCM.Application.Fhir.DataHandlers;
using HCM.Application.Hl7.DataHandlers;
using HCM.Application.Notifications.DataHandlers;
using HCM.Application.Patients.DataHandlers;
using HCM.Application.Tasks.DataHandlers;
using HCM.Application.Users.DataHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace HCM.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all DataHandlers (one per feature/aggregate)
        services.AddScoped<IAuthDataHandler, AuthDataHandler>();
        services.AddScoped<IPatientDataHandler, PatientDataHandler>();
        services.AddScoped<INotificationDataHandler, NotificationDataHandler>();
        services.AddScoped<ICaseDataHandler, CaseDataHandler>();
        services.AddScoped<ICareTeamDataHandler, CareTeamDataHandler>();
        services.AddScoped<ICaseNoteDataHandler, CaseNoteDataHandler>();
        services.AddScoped<IUserDataHandler, UserDataHandler>();
        services.AddScoped<ITaskDataHandler, TaskDataHandler>();
        services.AddScoped<IDocumentDataHandler, DocumentDataHandler>();
        services.AddScoped<IAppointmentDataHandler, AppointmentDataHandler>();
        services.AddScoped<IFhirDataHandler, FhirDataHandler>();
        services.AddScoped<IHl7DataHandler, Hl7DataHandler>();
        services.AddScoped<IAuditDataHandler, AuditDataHandler>();
        services.AddScoped<IDashboardDataHandler, DashboardDataHandler>();
        services.AddScoped<IReportDataHandler, ReportDataHandler>();

        return services;
    }
}
