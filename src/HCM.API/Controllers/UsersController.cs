using HCM.Application.Users.DataHandlers;
using HCM.Application.Users.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserDataHandler _handler;

    public UsersController(IUserDataHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _handler.GetUsersAsync(page, pageSize, search, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _handler.GetUserByIdAsync(id, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _handler.CreateUserAsync(dto, ct);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _handler.UpdateUserAsync(id, dto, ct);
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

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _handler.AssignRolesAsync(id, dto.Roles, ct);
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

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> SetActiveStatus(Guid id, [FromBody] SetActiveStatusDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _handler.SetActiveStatusAsync(id, dto.IsActive, ct);
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
}
