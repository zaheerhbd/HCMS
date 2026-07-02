using System.Security.Claims;
using HCM.Application.Appointments.DataHandlers;
using HCM.Application.Appointments.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentDataHandler _handler;

    public AppointmentsController(IAppointmentDataHandler handler)
    {
        _handler = handler;
    }

    // Paginated list of all appointments, optionally filtered by status or patient MRN
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? patientMrn,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _handler.GetAllAppointmentsAsync(status, patientMrn, page, pageSize, ct);
        return Ok(new { items, totalCount, page, pageSize });
    }

    // Single appointment by ID
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var appt = await _handler.GetAppointmentByIdAsync(id, ct);
            return Ok(appt);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // Schedules a new appointment with conflict detection (FR-31)
    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor,CareCoordinator")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var appt = await _handler.CreateAppointmentAsync(dto, userId, ct);
            return CreatedAtAction(nameof(GetById), new { id = appt.Id }, appt);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (FluentValidation.ValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // Updates appointment details (cannot edit completed or cancelled appointments)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Supervisor,CareCoordinator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAppointmentDto dto, CancellationToken ct)
    {
        try
        {
            var appt = await _handler.UpdateAppointmentAsync(id, dto, ct);
            return Ok(appt);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (FluentValidation.ValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // Transitions appointment status (Confirm, Complete, Cancel, NoShow)
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] AppointmentStatusChangeDto dto, CancellationToken ct)
    {
        try
        {
            var appt = await _handler.ChangeAppointmentStatusAsync(id, dto, ct);
            return Ok(appt);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (FluentValidation.ValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // Lists all appointments for a specific patient by MRN
    [HttpGet("patient/{mrn}")]
    public async Task<IActionResult> GetByPatientMrn(string mrn, CancellationToken ct)
    {
        try
        {
            var appts = await _handler.GetAppointmentsByPatientMrnAsync(mrn, ct);
            return Ok(appts);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // Returns all active providers for use in appointment scheduling dropdowns
    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders(CancellationToken ct)
    {
        var providers = await _handler.GetProvidersAsync(ct);
        return Ok(providers);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
