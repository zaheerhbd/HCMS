using FluentValidation;
using HCM.Application.CareTeam.DTOs;
using HCM.Application.Interfaces;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.CareTeam.DataHandlers;

public class CareTeamDataHandler : ICareTeamDataHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CareTeamDataHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddMemberAsync(string caseNumber, AddCareTeamMemberDto dto, Guid addedBy, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"User with ID {dto.UserId} not found.");

        var existingMember = await _dbContext.CareTeamMembers
            .FirstOrDefaultAsync(ctm => ctm.CaseId == caseEntity.Id && ctm.UserId == dto.UserId && ctm.IsActive, cancellationToken: ct);

        if (existingMember != null)
            throw new ValidationException("User is already a member of this care team.");

        var member = new CareTeamMember
        {
            CaseId = caseEntity.Id,
            UserId = dto.UserId,
            TeamRole = dto.TeamRole,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.CareTeamMembers.Add(member);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(string caseNumber, Guid userId, Guid removedBy, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");

        var member = await _dbContext.CareTeamMembers
            .FirstOrDefaultAsync(ctm => ctm.CaseId == caseEntity.Id && ctm.UserId == userId && ctm.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Care team member not found.");

        member.IsActive = false;
        member.LeftAt = DateTime.UtcNow;

        _dbContext.CareTeamMembers.Update(member);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<List<CareTeamMemberDto>> GetCaseTeamAsync(string caseNumber, CancellationToken ct = default)
    {
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");

        var members = await _dbContext.CareTeamMembers
            .AsNoTracking()
            .Where(ctm => ctm.CaseId == caseEntity.Id && ctm.IsActive)
            .Include(ctm => ctm.User)
            .ToListAsync(ct);

        return members.Select(m => new CareTeamMemberDto
        {
            Id = m.Id,
            UserId = m.UserId,
            UserName = $"{m.User.FirstName} {m.User.LastName}",
            UserEmail = m.User.Email,
            TeamRole = m.TeamRole,
            JoinedAt = m.JoinedAt,
            LeftAt = m.LeftAt,
            IsActive = m.IsActive
        }).ToList();
    }
}
