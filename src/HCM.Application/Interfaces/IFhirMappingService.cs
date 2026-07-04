using HCM.Domain.Entities;

namespace HCM.Application.Interfaces;

// Maps domain entities → serialized FHIR R4 JSON strings.
// The Infrastructure implementation uses the Firely SDK; Application stays SDK-free.
public interface IFhirMappingService
{
    string GetCapabilityStatement(string baseUrl);

    string MapPatientToJson(Patient patient);
    string MapPatientsBundle(IEnumerable<Patient> patients, int totalCount, string selfUrl);

    string MapEverythingBundle(
        Patient patient,
        IEnumerable<Case> cases,
        IEnumerable<Appointment> appointments,
        IEnumerable<CaseTask> tasks,
        IEnumerable<Document> documents,
        string selfUrl);

    string MapEncounterToJson(Case c);
    string MapEncountersBundle(IEnumerable<Case> cases, int totalCount, string selfUrl);

    string MapAppointmentToJson(Appointment appointment);
    string MapAppointmentsBundle(IEnumerable<Appointment> appointments, int totalCount, string selfUrl);
    string MapTaskToJson(CaseTask task);
    string MapTasksBundle(IEnumerable<CaseTask> tasks, int totalCount, string selfUrl);
    string MapDocumentReferenceToJson(Document document);

    // Parses an incoming FHIR Patient JSON into a domain Patient shell for import.
    // Returns (null, errorMessage) when the payload cannot be parsed.
    (Patient? patient, string? error) ParsePatientFromFhir(string fhirJson);
}
