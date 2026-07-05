namespace HCM.Application.Hl7.DTOs;

public record Hl7AdtResultDto(bool Success, string? Mrn, string Message);

public record Hl7OruResultDto(bool Success, string? NoteId, string Message);
