using HCM.Application.Reports.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Supervisor,CareCoordinator")]
public class ReportsController : ControllerBase
{
    private readonly IReportDataHandler _handler;

    public ReportsController(IReportDataHandler handler)
    {
        _handler = handler;
    }

    // Returns cases grouped by status as JSON or CSV depending on the format query param.
    [HttpGet("cases-by-status")]
    public async Task<IActionResult> CasesByStatus([FromQuery] string? format, CancellationToken ct)
    {
        if (format?.ToLower() == "csv")
        {
            var csv = await _handler.GetCasesByStatusCsvAsync(ct);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "cases-by-status.csv");
        }
        return Ok(await _handler.GetCasesByStatusAsync(ct));
    }

    // Returns average case duration per case type as JSON or CSV.
    [HttpGet("case-duration")]
    public async Task<IActionResult> CaseDuration([FromQuery] string? format, CancellationToken ct)
    {
        if (format?.ToLower() == "csv")
        {
            var csv = await _handler.GetCaseDurationCsvAsync(ct);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "case-duration.csv");
        }
        return Ok(await _handler.GetCaseDurationAsync(ct));
    }

    // Returns task breakdown per assignee as JSON or CSV.
    [HttpGet("tasks-by-assignee")]
    public async Task<IActionResult> TasksByAssignee([FromQuery] string? format, CancellationToken ct)
    {
        if (format?.ToLower() == "csv")
        {
            var csv = await _handler.GetTasksByAssigneeCsvAsync(ct);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "tasks-by-assignee.csv");
        }
        return Ok(await _handler.GetTasksByAssigneeAsync(ct));
    }
}
