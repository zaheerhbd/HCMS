using HCM.Application.CaseNotes.DTOs;

namespace HCM.Application.CaseNotes.DataHandlers;

public interface ICaseNoteDataHandler
{
    Task<CaseNoteDto> AddNoteAsync(string caseNumber, CreateCaseNoteDto dto, Guid createdBy, CancellationToken ct = default);
    Task<List<CaseNoteDto>> GetNotesAsync(string caseNumber, CancellationToken ct = default);
}
