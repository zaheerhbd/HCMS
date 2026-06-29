using HCM.Application.CaseNotes.DTOs;

namespace HCM.Application.CaseNotes.DataHandlers;

public interface ICaseNoteDataHandler
{
    Task<CaseNoteDto> AddNoteAsync(Guid caseId, CreateCaseNoteDto dto, Guid createdBy, CancellationToken ct = default);
    Task<List<CaseNoteDto>> GetNotesAsync(Guid caseId, CancellationToken ct = default);
}
