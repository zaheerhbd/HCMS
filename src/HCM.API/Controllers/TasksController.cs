using System.Security.Claims;
using HCM.Application.Tasks.DataHandlers;
using HCM.Application.Tasks.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/cases/{caseNumber}/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskDataHandler _handler;

    public TasksController(ITaskDataHandler handler)
    {
        _handler = handler;
    }

    // Returns all tasks for a given case
    [HttpGet]
    public async Task<IActionResult> GetTasks(string caseNumber, CancellationToken ct)
    {
        try
        {
            var tasks = await _handler.GetTasksByCaseNumberAsync(caseNumber, ct);
            return Ok(tasks);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // Creates a new task within a case (CareCoordinator, Supervisor, Admin)
    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor,CareCoordinator")]
    public async Task<IActionResult> CreateTask(string caseNumber, [FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var task = await _handler.CreateTaskAsync(caseNumber, dto, userId, ct);
            return CreatedAtAction(nameof(GetTask), new { caseNumber, taskId = task.Id }, task);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (FluentValidation.ValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // Returns a single task by ID within a case
    [HttpGet("{taskId:guid}")]
    public async Task<IActionResult> GetTask(string caseNumber, Guid taskId, CancellationToken ct)
    {
        try
        {
            var task = await _handler.GetTaskByIdAsync(taskId, ct);
            return Ok(task);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // Updates task metadata (title, description, priority, due date, assignment)
    [HttpPut("{taskId:guid}")]
    [Authorize(Roles = "Admin,Supervisor,CareCoordinator")]
    public async Task<IActionResult> UpdateTask(string caseNumber, Guid taskId, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        try
        {
            var task = await _handler.UpdateTaskAsync(taskId, dto, ct);
            return Ok(task);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (FluentValidation.ValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // Changes task status (any authenticated user — allows assignees to update their own tasks)
    [HttpPost("{taskId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(string caseNumber, Guid taskId, [FromBody] TaskStatusChangeDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var task = await _handler.ChangeTaskStatusAsync(taskId, dto, userId, ct);
            return Ok(task);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (FluentValidation.ValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // Soft-cancels a task
    [HttpDelete("{taskId:guid}")]
    [Authorize(Roles = "Admin,Supervisor,CareCoordinator")]
    public async Task<IActionResult> DeleteTask(string caseNumber, Guid taskId, CancellationToken ct)
    {
        try
        {
            await _handler.DeleteTaskAsync(taskId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

// Separate controller for "my tasks" endpoint — not case-scoped
[ApiController]
[Route("api/tasks")]
[Authorize]
public class MyTasksController : ControllerBase
{
    private readonly ITaskDataHandler _handler;

    public MyTasksController(ITaskDataHandler handler)
    {
        _handler = handler;
    }

    // Returns all active tasks assigned to the current user
    [HttpGet("my")]
    public async Task<IActionResult> GetMyTasks(CancellationToken ct)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId)) return Unauthorized();

        var tasks = await _handler.GetMyTasksAsync(userId, ct);
        return Ok(tasks);
    }
}
