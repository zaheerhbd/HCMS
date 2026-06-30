using HCM.Application.CaseNotes.DTOs;
using HCM.Application.CaseNotes.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/cases/{caseNumber}/notes")]
[Authorize]
public class CaseNotesController : ControllerBase
{
    private readonly ICaseNoteDataHandler _handler;

    public CaseNotesController(ICaseNoteDataHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotes(string caseNumber, CancellationToken ct)
    {
        try
        {
            var result = await _handler.GetNotesAsync(caseNumber, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,CareCoordinator,Clinician")]
    public async Task<IActionResult> AddNote(string caseNumber, [FromBody] CreateCaseNoteDto dto, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                return Unauthorized();

            var result = await _handler.AddNoteAsync(caseNumber, dto, userIdGuid, ct);
            return CreatedAtAction(nameof(GetNotes), new { caseNumber }, result);
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
}
