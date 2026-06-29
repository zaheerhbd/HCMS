using HCM.Application.CareTeam.DTOs;

namespace HCM.Application.CareTeam.DataHandlers;

public interface ICareTeamDataHandler
{
    Task AddMemberAsync(Guid caseId, AddCareTeamMemberDto dto, Guid addedBy, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid caseId, Guid userId, Guid removedBy, CancellationToken ct = default);
    Task<List<CareTeamMemberDto>> GetCaseTeamAsync(Guid caseId, CancellationToken ct = default);
}
