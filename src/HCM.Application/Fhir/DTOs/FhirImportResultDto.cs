namespace HCM.Application.Fhir.DTOs;

public class FhirImportResultDto
{
    public bool Success { get; set; }
    public string? Mrn { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];
}
