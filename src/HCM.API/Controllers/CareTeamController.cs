using HCM.Application.CareTeam.DTOs;
using HCM.Application.CareTeam.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/cases/{caseId}/team")]
[Authorize]
public class CareTeamController : ControllerBase
{
    private readonly ICareTeamDataHandler _handler;

    public CareTeamController(ICareTeamDataHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeam(Guid caseId, CancellationToken ct)
    {
        try
        {
            var result = await _handler.GetCaseTeamAsync(caseId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,CareCoordinator,Supervisor")]
    public async Task<IActionResult> AddMember(Guid caseId, [FromBody] AddCareTeamMemberDto dto, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                return Unauthorized();

            await _handler.AddMemberAsync(caseId, dto, userIdGuid, ct);
            var team = await _handler.GetCaseTeamAsync(caseId, ct);
            return Ok(team);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{userId}")]
    [Authorize(Roles = "Admin,CareCoordinator,Supervisor")]
    public async Task<IActionResult> RemoveMember(Guid caseId, Guid userId, CancellationToken ct)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserIdGuid))
                return Unauthorized();

            await _handler.RemoveMemberAsync(caseId, userId, currentUserIdGuid, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
