namespace HCM.Application.Fhir.DTOs;

public record FhirBundleImportResultDto(
    bool Success,
    List<BundleEntryResultDto> Entries,
    List<string> Errors);

public record BundleEntryResultDto(
    string ResourceType,
    string AssignedId,
    string Status);
