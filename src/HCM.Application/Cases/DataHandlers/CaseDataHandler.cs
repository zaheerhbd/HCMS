using FluentValidation;
using HCM.Application.CareTeam.DTOs;
using HCM.Application.CaseNotes.DTOs;
using HCM.Application.Cases.DTOs;
using HCM.Application.Interfaces;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Cases.DataHandlers;

public class CaseDataHandler : ICaseDataHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CaseDataHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CaseDto> GetCaseByIdAsync(Guid id, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.StatusHistory)
            .Include(c => c.CareTeam)
            .Include(c => c.Notes_Collection.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case with ID {id} not found.");

        return MapToDto(caseEntity);
    }

    public async Task<(List<CaseListItemDto> Items, int TotalCount)> GetCasesByPatientAsync(
        Guid patientId, string? statusFilter, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _dbContext.Cases
            .Where(c => c.PatientId == patientId && c.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(c => c.CurrentStatus == statusFilter);
        }

        var totalCount = await query.CountAsync(ct);
        var cases = await query
            .OrderByDescending(c => c.OpenedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.CaseType)
            .ToListAsync(ct);

        var items = cases.Select(c => new CaseListItemDto
        {
            Id = c.Id,
            CaseNumber = c.CaseNumber,
            CaseTypeId = c.CaseTypeId,
            CaseTypeName = c.CaseType.Name,
            CurrentStatus = c.CurrentStatus,
            OpenedAt = c.OpenedAt,
            ClosedAt = c.ClosedAt,
            AssignedToUserName = null // Would need User lookup if needed
        }).ToList();

        return (items, totalCount);
    }

    public async Task<CaseDto> CreateCaseAsync(Guid patientId, CreateCaseDto dto, Guid createdBy, Guid assignedTo, CancellationToken ct = default)
    {
        var patient = await _dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

        var caseType = await _dbContext.CaseTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.Id == dto.CaseTypeId && ct.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case type with ID {dto.CaseTypeId} not found.");

        var caseNumber = await GenerateCaseNumberAsync(ct);
        var newCase = new Case
        {
            Id = Guid.NewGuid(),
            CaseNumber = caseNumber,
            PatientId = patientId,
            CaseTypeId = dto.CaseTypeId,
            CurrentStatus = CaseStatus.Open,
            OpenedAt = DateTime.UtcNow,
            AssignedToUserId = assignedTo,
            Notes = dto.Notes?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Cases.Add(newCase);

        var statusHistory = new CaseStatusHistory
        {
            Id = Guid.NewGuid(),
            CaseId = newCase.Id,
            FromStatus = null,
            ToStatus = CaseStatus.Open,
            Comment = "Case created",
            ChangedBy = createdBy,
            ChangedAt = DateTime.UtcNow
        };
        _dbContext.CaseStatusHistories.Add(statusHistory);

        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(newCase);
    }

    public async Task<CaseDto> ChangeCaseStatusAsync(Guid caseId, CaseStatusChangeDto dto, Guid changedBy, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.StatusHistory)
            .Include(c => c.CareTeam)
            .Include(c => c.Notes_Collection.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == caseId && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case with ID {caseId} not found.");

        if (!IsValidStatusTransition(caseEntity.CurrentStatus, dto.NewStatus))
            throw new ValidationException($"Cannot transition from {caseEntity.CurrentStatus} to {dto.NewStatus}.");

        caseEntity.CurrentStatus = dto.NewStatus;
        if (dto.NewStatus == CaseStatus.Closed)
        {
            caseEntity.ClosedAt = DateTime.UtcNow;
        }
        caseEntity.UpdatedAt = DateTime.UtcNow;

        var statusHistory = new CaseStatusHistory
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            FromStatus = caseEntity.CurrentStatus,
            ToStatus = dto.NewStatus,
            Comment = dto.Comment,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };
        _dbContext.CaseStatusHistories.Add(statusHistory);

        _dbContext.Cases.Update(caseEntity);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(caseEntity);
    }

    public async Task<CaseDto> UpdateCaseAsync(Guid caseId, UpdateCaseDto dto, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.StatusHistory)
            .Include(c => c.CareTeam)
            .Include(c => c.Notes_Collection.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == caseId && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case with ID {caseId} not found.");

        caseEntity.Notes = dto.Notes?.Trim();
        caseEntity.UpdatedAt = DateTime.UtcNow;

        _dbContext.Cases.Update(caseEntity);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(caseEntity);
    }

    public async Task CloseCaseAsync(Guid caseId, Guid closedBy, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.StatusHistory)
            .Include(c => c.CareTeam)
            .Include(c => c.Notes_Collection.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == caseId && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case with ID {caseId} not found.");

        if (caseEntity.CurrentStatus == CaseStatus.Closed)
            throw new ValidationException("Case is already closed.");

        caseEntity.CurrentStatus = CaseStatus.Closed;
        caseEntity.ClosedAt = DateTime.UtcNow;
        caseEntity.UpdatedAt = DateTime.UtcNow;

        var statusHistory = new CaseStatusHistory
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            FromStatus = caseEntity.CurrentStatus,
            ToStatus = CaseStatus.Closed,
            Comment = "Case closed",
            ChangedBy = closedBy,
            ChangedAt = DateTime.UtcNow
        };
        _dbContext.CaseStatusHistories.Add(statusHistory);

        _dbContext.Cases.Update(caseEntity);
        await _dbContext.SaveChangesAsync(ct);
    }

    private bool IsValidStatusTransition(string currentStatus, string newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (CaseStatus.Open, CaseStatus.InProgress) => true,
            (CaseStatus.Open, CaseStatus.OnHold) => true,
            (CaseStatus.Open, CaseStatus.Closed) => true,
            (CaseStatus.InProgress, CaseStatus.OnHold) => true,
            (CaseStatus.InProgress, CaseStatus.Closed) => true,
            (CaseStatus.OnHold, CaseStatus.InProgress) => true,
            (CaseStatus.OnHold, CaseStatus.Closed) => true,
            (CaseStatus.Closed, CaseStatus.Reopened) => true,
            (CaseStatus.Reopened, CaseStatus.InProgress) => true,
            (CaseStatus.Reopened, CaseStatus.OnHold) => true,
            (CaseStatus.Reopened, CaseStatus.Closed) => true,
            _ => false
        };
    }

    private async Task<string> GenerateCaseNumberAsync(CancellationToken ct)
    {
        var currentYear = DateTime.UtcNow.Year;
        var lastCase = await _dbContext.Cases
            .AsNoTracking()
            .Where(c => c.CaseNumber.StartsWith($"CASE-{currentYear}-"))
            .OrderByDescending(c => c.CaseNumber)
            .FirstOrDefaultAsync(cancellationToken: ct);

        int nextSequence = 1;
        if (lastCase != null && int.TryParse(lastCase.CaseNumber.Substring(10), out int lastSequence))
        {
            nextSequence = lastSequence + 1;
        }

        return $"CASE-{currentYear}-{nextSequence:D5}";
    }

    private CaseDto MapToDto(Case caseEntity)
    {
        return new CaseDto
        {
            Id = caseEntity.Id,
            CaseNumber = caseEntity.CaseNumber,
            PatientId = caseEntity.PatientId,
            CaseTypeId = caseEntity.CaseTypeId,
            CaseTypeName = caseEntity.CaseType?.Name ?? "",
            CurrentStatus = caseEntity.CurrentStatus,
            OpenedAt = caseEntity.OpenedAt,
            ClosedAt = caseEntity.ClosedAt,
            AssignedToUserId = caseEntity.AssignedToUserId,
            AssignedToUserName = null,
            Notes = caseEntity.Notes,
            CreatedAt = caseEntity.CreatedAt,
            UpdatedAt = caseEntity.UpdatedAt,
            StatusHistory = caseEntity.StatusHistory?.Select(sh => new CaseStatusHistoryDto
            {
                Id = sh.Id,
                FromStatus = sh.FromStatus,
                ToStatus = sh.ToStatus,
                Comment = sh.Comment,
                ChangedBy = sh.ChangedBy,
                ChangedByUserName = "",
                ChangedAt = sh.ChangedAt
            }).ToList() ?? [],
            CareTeam = caseEntity.CareTeam?.Select(ctm => new CareTeamMemberDto
            {
                Id = ctm.Id,
                UserId = ctm.UserId,
                UserName = "",
                UserEmail = "",
                TeamRole = ctm.TeamRole,
                JoinedAt = ctm.JoinedAt,
                LeftAt = ctm.LeftAt,
                IsActive = ctm.IsActive
            }).ToList() ?? [],
            Notes_Collection = caseEntity.Notes_Collection?.Where(n => !n.IsDeleted).Select(cn => new CaseNoteDto
            {
                Id = cn.Id,
                CaseId = cn.CaseId,
                CreatedBy = cn.CreatedBy,
                CreatedByUserName = "",
                Content = cn.Content,
                IsEditable = cn.IsEditable,
                CreatedAt = cn.CreatedAt,
                UpdatedAt = cn.UpdatedAt
            }).ToList() ?? []
        };
    }
}
