using HCM.Application.Cases.DTOs;
using HCM.Application.Cases.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize]
public class CasesController : ControllerBase
{
    private readonly ICaseDataHandler _handler;

    public CasesController(ICaseDataHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCases([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var (items, totalCount) = await _handler.GetAllCasesAsync(status, page, pageSize, ct);
        return Ok(new { items, totalCount, page, pageSize });
    }

    [HttpGet("{caseNumber}")]
    public async Task<IActionResult> GetCaseByCaseNumber(string caseNumber, CancellationToken ct)
    {
        try
        {
            var result = await _handler.GetCaseByCaseNumberAsync(caseNumber, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("patient/mrn/{mrn}")]
    public async Task<IActionResult> GetCasesByPatient(string mrn, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        try
        {
            var (items, totalCount) = await _handler.GetCasesByPatientMrnAsync(mrn, status, page, pageSize, ct);
            return Ok(new { items, totalCount, page, pageSize });
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

    [HttpPost]
    [Authorize(Roles = "Admin,CareCoordinator")]
    public async Task<IActionResult> CreateCase([FromQuery] string patientMrn, [FromBody] CreateCaseDto dto, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                return Unauthorized();

            var result = await _handler.CreateCaseAsync(patientMrn, dto, userIdGuid, userIdGuid, ct);
            return CreatedAtAction(nameof(GetCaseByCaseNumber), new { caseNumber = result.CaseNumber }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{caseNumber}")]
    [Authorize(Roles = "Admin,CareCoordinator")]
    public async Task<IActionResult> UpdateCase(string caseNumber, [FromBody] UpdateCaseDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _handler.UpdateCaseAsync(caseNumber, dto, ct);
            return Ok(result);
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

    [HttpPost("{caseNumber}/status")]
    [Authorize(Roles = "Admin,CareCoordinator,Supervisor")]
    public async Task<IActionResult> ChangeStatus(string caseNumber, [FromBody] CaseStatusChangeDto dto, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                return Unauthorized();

            var result = await _handler.ChangeCaseStatusAsync(caseNumber, dto, userIdGuid, ct);
            return Ok(result);
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

    [HttpPost("{caseNumber}/close")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> CloseCase(string caseNumber, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                return Unauthorized();

            await _handler.CloseCaseAsync(caseNumber, userIdGuid, ct);
            return NoContent();
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
