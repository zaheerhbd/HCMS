using HCM.Application.Patients.DTOs;
using HCM.Application.Patients.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientDataHandler _handler;

    public PatientsController(IPatientDataHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("{mrn}")]
    public async Task<IActionResult> GetPatientByMrn(string mrn, CancellationToken ct)
    {
        try
        {
            var result = await _handler.GetPatientByMrnAsync(mrn, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchPatients([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        try
        {
            var result = await _handler.SearchPatientsAsync(search, page, pageSize, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,CareCoordinator")]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                return Unauthorized();

            var result = await _handler.CreatePatientAsync(dto, userIdGuid, ct);
            return CreatedAtAction(nameof(GetPatientByMrn), new { mrn = result.MRN }, result);
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

    [HttpPut("{mrn}")]
    [Authorize(Roles = "Admin,CareCoordinator")]
    public async Task<IActionResult> UpdatePatient(string mrn, [FromBody] UpdatePatientDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _handler.UpdatePatientAsync(mrn, dto, ct);
            return Ok(result);
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

    [HttpDelete("{mrn}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> DeletePatient(string mrn, CancellationToken ct)
    {
        try
        {
            await _handler.DeletePatientAsync(mrn, ct);
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
