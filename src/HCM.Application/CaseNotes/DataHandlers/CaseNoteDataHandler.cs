using FluentValidation;
using HCM.Application.CaseNotes.DTOs;
using HCM.Application.Interfaces;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.CaseNotes.DataHandlers;

public class CaseNoteDataHandler : ICaseNoteDataHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CaseNoteDataHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CaseNoteDto> AddNoteAsync(Guid caseId, CreateCaseNoteDto dto, Guid createdBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            throw new ValidationException("Note content is required.");

        if (dto.Content.Length > 5000)
            throw new ValidationException("Note content cannot exceed 5000 characters.");

        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case with ID {caseId} not found.");

        var note = new CaseNote
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            CreatedBy = createdBy,
            Content = dto.Content.Trim(),
            IsEditable = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.CaseNotes.Add(note);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(note);
    }

    public async Task<List<CaseNoteDto>> GetNotesAsync(Guid caseId, CancellationToken ct = default)
    {
        var notes = await _dbContext.CaseNotes
            .AsNoTracking()
            .Where(cn => cn.CaseId == caseId && !cn.IsDeleted)
            .OrderByDescending(cn => cn.CreatedAt)
            .ToListAsync(ct);

        return notes.Select(MapToDto).ToList();
    }

    private CaseNoteDto MapToDto(CaseNote note)
    {
        return new CaseNoteDto
        {
            Id = note.Id,
            CaseId = note.CaseId,
            CreatedBy = note.CreatedBy,
            CreatedByUserName = "",
            Content = note.Content,
            IsEditable = note.IsEditable,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }
}
