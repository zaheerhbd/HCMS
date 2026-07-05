using HCM.Application.Dashboard.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardDataHandler _handler;

    public DashboardController(IDashboardDataHandler handler)
    {
        _handler = handler;
    }

    // Returns coordinator dashboard data scoped to the calling user.
    [HttpGet("coordinator")]
    public async Task<IActionResult> GetCoordinator(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var result = await _handler.GetCoordinatorDashboardAsync(userId, ct);
        return Ok(result);
    }

    // Returns supervisor dashboard data (team-wide); requires Supervisor or Admin role.
    [HttpGet("supervisor")]
    [Authorize(Roles = "Supervisor,Admin")]
    public async Task<IActionResult> GetSupervisor(CancellationToken ct)
    {
        var result = await _handler.GetSupervisorDashboardAsync(ct);
        return Ok(result);
    }

    // Returns admin dashboard metrics; requires Admin role.
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdmin(CancellationToken ct)
    {
        var result = await _handler.GetAdminDashboardAsync(ct);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
