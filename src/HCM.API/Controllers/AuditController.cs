using HCM.Application.Audit.DataHandlers;
using HCM.Application.Audit.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
public class AuditController(IAuditDataHandler handler) : ControllerBase
{
    private readonly IAuditDataHandler _handler = handler;

    /// <summary>
    /// GET api/audit — paginated audit log with optional filters.
    /// Query params: entityType, changedBy, action, from (ISO date), to (ISO date), page, pageSize
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType,
        [FromQuery] string? changedBy,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new AuditLogFilterDto
        {
            EntityType = entityType,
            ChangedBy = changedBy,
            Action = action,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize
        };

        var result = await _handler.GetAuditLogsAsync(filter, ct);
        return Ok(result);
    }
}
