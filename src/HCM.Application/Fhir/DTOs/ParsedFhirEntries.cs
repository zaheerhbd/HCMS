namespace HCM.Application.Fhir.DTOs;

// DTOs for parsed FHIR Transaction Bundle entries — no Firely SDK types in Application layer.

public class ParsedBundleEntry
{
    public required string ResourceType { get; init; }
    public string? FullUrl { get; init; }
    public ParsedFhirPatient? Patient { get; init; }
    public ParsedFhirEncounter? Encounter { get; init; }
    public ParsedFhirAppointment? Appointment { get; init; }
    public ParsedFhirTask? Task { get; init; }
}

public record ParsedFhirPatient(
    string FirstName, string LastName, DateTime DateOfBirth,
    string? Gender, string? Phone, string? Email,
    string? Address, string? City, string? State, string? ZipCode);

public record ParsedFhirEncounter(
    string? SubjectRef,
    string? TypeText);

public record ParsedFhirAppointment(
    string? PatientRef,
    DateTime? Start,
    DateTime? End,
    string? ServiceType,
    string? PractitionerRef);

public record ParsedFhirTask(
    string? Description,
    string? PatientRef,
    string? EncounterRef,
    DateTime? DueDate,
    string? Priority);
