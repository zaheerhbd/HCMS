using HCM.Application.Fhir.DTOs;


namespace HCM.Application.Fhir.DataHandlers;

public interface IFhirDataHandler
{
    Task<string> GetCapabilityStatementAsync(string baseUrl, CancellationToken ct = default);

    Task<string> SearchPatientsAsync(string? id, string? name, string? birthdate, string? identifier, string baseUrl, CancellationToken ct = default);
    Task<string> GetPatientAsync(string mrn, string baseUrl, CancellationToken ct = default);
    Task<string> GetEverythingAsync(string mrn, string baseUrl, CancellationToken ct = default);
    Task<FhirImportResultDto> ImportPatientAsync(string fhirJson, string importedBy, CancellationToken ct = default);

    Task<string> SearchEncountersAsync(string? patientMrn, string? status, string baseUrl, CancellationToken ct = default);
    Task<string> GetEncounterAsync(string caseNumber, string baseUrl, CancellationToken ct = default);

    Task<string> SearchAppointmentsAsync(string? id, string? patientMrn, string? status, string baseUrl, CancellationToken ct = default);
    Task<string> GetAppointmentAsync(string id, string baseUrl, CancellationToken ct = default);
    Task<string> SearchTasksAsync(string? id, string? patientMrn, string? focus, string? status, string baseUrl, CancellationToken ct = default);
    Task<string> GetTaskAsync(string id, string baseUrl, CancellationToken ct = default);
    Task<string> GetDocumentReferenceAsync(string id, string baseUrl, CancellationToken ct = default);

    // Imports a FHIR Transaction Bundle atomically: Patient + Encounter + Appointments + Tasks in one request.
    Task<FhirBundleImportResultDto> ImportTransactionBundleAsync(string bundleJson, string importedBy, CancellationToken ct = default);
}
