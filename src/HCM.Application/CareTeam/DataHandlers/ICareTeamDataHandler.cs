using HCM.Application.CareTeam.DTOs;

namespace HCM.Application.CareTeam.DataHandlers;

public interface ICareTeamDataHandler
{
    Task AddMemberAsync(string caseNumber, AddCareTeamMemberDto dto, Guid addedBy, CancellationToken ct = default);
    Task RemoveMemberAsync(string caseNumber, Guid userId, Guid removedBy, CancellationToken ct = default);
    Task<List<CareTeamMemberDto>> GetCaseTeamAsync(string caseNumber, CancellationToken ct = default);
}
