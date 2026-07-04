namespace HCM.Domain.Entities;

public class FhirImportLog
{
    public long Id { get; set; }
    public string ResourceType { get; set; } = string.Empty;    // "Patient", "Encounter", etc.
    public string? SourceSystem { get; set; }                   // "FHIR-HTTP", "HL7v2-ADT"
    public string? FhirResourceId { get; set; }                 // Incoming FHIR resource id
    public string? InternalId { get; set; }                     // MRN or CaseNumber created
    public string Status { get; set; } = string.Empty;          // "Success", "Failed", "Partial"
    public string? ErrorMessage { get; set; }
    public string? ImportedBy { get; set; }                     // User ID — no FK (survives user deletion)
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public string? RawPayload { get; set; }                     // Original FHIR JSON submitted
}
