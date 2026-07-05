namespace HCM.Application.Interfaces;

// Data extracted from HL7 v2 ADT^A01 — no PHI fields beyond what's needed to create a patient record.
public record Hl7PatientData(
    string? FirstName, string? LastName, DateTime? DateOfBirth,
    string? Gender, string? Phone, string? ExternalId);

// Data extracted from HL7 v2 ORU^R01 observation message.
public record Hl7ObservationData(
    string? PatientId, string? ObservationType,
    string? ObservationValue, string? Units);

// Parses HL7 v2.5.1 messages using NHapi. Lives in Infrastructure; Application stays SDK-free.
public interface IHl7MappingService
{
    Hl7PatientData ParseAdtA01(string hl7Message);
    Hl7ObservationData ParseOruR01(string hl7Message);
}
