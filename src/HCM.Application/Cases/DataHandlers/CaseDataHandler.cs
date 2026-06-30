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

    public async Task<(List<CaseListItemDto> Items, int TotalCount)> GetAllCasesAsync(
        string? statusFilter, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _dbContext.Cases
            .Where(c => c.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(c => c.CurrentStatus == statusFilter);

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
            AssignedToUserName = null
        }).ToList();

        return (items, totalCount);
    }

    public async Task<CaseDto> GetCaseByCaseNumberAsync(string caseNumber, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.StatusHistory)
            .Include(c => c.CareTeam)
            .Include(c => c.Notes_Collection.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");

        return MapToDto(caseEntity);
    }

    public async Task<(List<CaseListItemDto> Items, int TotalCount)> GetCasesByPatientMrnAsync(
        string patientMrn, string? statusFilter, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var patient = await _dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MRN == patientMrn && p.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Patient with MRN {patientMrn} not found.");

        var query = _dbContext.Cases
            .Where(c => c.PatientId == patient.Id && c.IsActive)
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
            AssignedToUserName = null
        }).ToList();

        return (items, totalCount);
    }

    public async Task<CaseDto> CreateCaseAsync(string patientMrn, CreateCaseDto dto, Guid createdBy, Guid assignedTo, CancellationToken ct = default)
    {
        var patient = await _dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MRN == patientMrn && p.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Patient with MRN {patientMrn} not found.");

        var caseType = await _dbContext.CaseTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(ct2 => ct2.Id == dto.CaseTypeId && ct2.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case type with ID {dto.CaseTypeId} not found.");

        var caseNumber = await GenerateCaseNumberAsync(ct);
        var newCase = new Case
        {
            CaseNumber = caseNumber,
            PatientId = patient.Id,
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

        // Save first to get the DB-generated Id for the status history FK
        await _dbContext.SaveChangesAsync(ct);

        var statusHistory = new CaseStatusHistory
        {
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

    public async Task<CaseDto> ChangeCaseStatusAsync(string caseNumber, CaseStatusChangeDto dto, Guid changedBy, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.StatusHistory)
            .Include(c => c.CareTeam)
            .Include(c => c.Notes_Collection.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");

        if (!IsValidStatusTransition(caseEntity.CurrentStatus, dto.NewStatus))
            throw new ValidationException($"Cannot transition from {caseEntity.CurrentStatus} to {dto.NewStatus}.");

        var fromStatus = caseEntity.CurrentStatus;
        caseEntity.CurrentStatus = dto.NewStatus;
        if (dto.NewStatus == CaseStatus.Closed)
        {
            caseEntity.ClosedAt = DateTime.UtcNow;
        }
        caseEntity.UpdatedAt = DateTime.UtcNow;

        var statusHistory = new CaseStatusHistory
        {
            CaseId = caseEntity.Id,
            FromStatus = fromStatus,
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

    public async Task<CaseDto> UpdateCaseAsync(string caseNumber, UpdateCaseDto dto, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.StatusHistory)
            .Include(c => c.CareTeam)
            .Include(c => c.Notes_Collection.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");

        caseEntity.Notes = dto.Notes?.Trim();
        caseEntity.UpdatedAt = DateTime.UtcNow;

        _dbContext.Cases.Update(caseEntity);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(caseEntity);
    }

    public async Task CloseCaseAsync(string caseNumber, Guid closedBy, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.StatusHistory)
            .Include(c => c.CareTeam)
            .Include(c => c.Notes_Collection.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");

        if (caseEntity.CurrentStatus == CaseStatus.Closed)
            throw new ValidationException("Case is already closed.");

        var fromStatus = caseEntity.CurrentStatus;
        caseEntity.CurrentStatus = CaseStatus.Closed;
        caseEntity.ClosedAt = DateTime.UtcNow;
        caseEntity.UpdatedAt = DateTime.UtcNow;

        var statusHistory = new CaseStatusHistory
        {
            CaseId = caseEntity.Id,
            FromStatus = fromStatus,
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
