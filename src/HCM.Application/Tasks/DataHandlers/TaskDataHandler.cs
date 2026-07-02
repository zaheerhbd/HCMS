using FluentValidation;
using HCM.Application.Interfaces;
using HCM.Application.Tasks.DTOs;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Tasks.DataHandlers;

public class TaskDataHandler : ITaskDataHandler
{
    private readonly IApplicationDbContext _db;

    public TaskDataHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<TaskDto>> GetTasksByCaseNumberAsync(string caseNumber, CancellationToken ct = default)
    {
        var caseEntity = await GetCaseByNumberAsync(caseNumber, ct);

        var tasks = await _db.CaseTasks
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Where(t => t.CaseId == caseEntity.Id)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        return tasks.Select(MapToDto).ToList();
    }

    public async Task<TaskDto> GetTaskByIdAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await _db.CaseTasks
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new KeyNotFoundException($"Task {taskId} not found.");

        return MapToDto(task);
    }

    public async Task<List<TaskDto>> GetMyTasksAsync(Guid userId, CancellationToken ct = default)
    {
        var tasks = await _db.CaseTasks
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Include(t => t.Case)
            .Where(t => t.AssignedTo == userId && t.Status != CaseTaskStatus.Done && t.Status != CaseTaskStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Priority)
            .ToListAsync(ct);

        return tasks.Select(MapToDto).ToList();
    }

    public async Task<TaskDto> CreateTaskAsync(string caseNumber, CreateTaskDto dto, Guid createdBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ValidationException("Task title is required.");

        var caseEntity = await GetCaseByNumberAsync(caseNumber, ct);

        if (dto.AssignedTo.HasValue)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == dto.AssignedTo.Value && u.IsActive, ct);
            if (!userExists)
                throw new KeyNotFoundException($"User {dto.AssignedTo} not found.");
        }

        var task = new CaseTask
        {
            CaseId = caseEntity.Id,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Status = CaseTaskStatus.Todo,
            Priority = dto.Priority ?? "Medium",
            AssignedTo = dto.AssignedTo,
            DueDate = dto.DueDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CaseTasks.Add(task);
        await _db.SaveChangesAsync(ct);

        return await GetTaskByIdAsync(task.Id, ct);
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, CancellationToken ct = default)
    {
        var task = await _db.CaseTasks
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new KeyNotFoundException($"Task {taskId} not found.");

        if (dto.Title != null) task.Title = dto.Title.Trim();
        if (dto.Description != null) task.Description = dto.Description.Trim();
        if (dto.Priority != null) task.Priority = dto.Priority;
        if (dto.AssignedTo.HasValue) task.AssignedTo = dto.AssignedTo;
        if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;

        task.UpdatedAt = DateTime.UtcNow;
        _db.CaseTasks.Update(task);
        await _db.SaveChangesAsync(ct);

        return await GetTaskByIdAsync(task.Id, ct);
    }

    public async Task<TaskDto> ChangeTaskStatusAsync(Guid taskId, TaskStatusChangeDto dto, Guid changedBy, CancellationToken ct = default)
    {
        if (!CaseTaskStatus.AllStatuses.Contains(dto.NewStatus))
            throw new ValidationException($"Invalid task status: {dto.NewStatus}.");

        var task = await _db.CaseTasks
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new KeyNotFoundException($"Task {taskId} not found.");

        task.Status = dto.NewStatus;

        if (dto.NewStatus == CaseTaskStatus.Done)
        {
            task.CompletedAt = DateTime.UtcNow;
            task.CompletedBy = changedBy;
        }

        task.UpdatedAt = DateTime.UtcNow;
        _db.CaseTasks.Update(task);
        await _db.SaveChangesAsync(ct);

        return await GetTaskByIdAsync(task.Id, ct);
    }

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await _db.CaseTasks
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new KeyNotFoundException($"Task {taskId} not found.");

        // Soft cancel instead of hard delete
        task.Status = CaseTaskStatus.Cancelled;
        task.UpdatedAt = DateTime.UtcNow;
        _db.CaseTasks.Update(task);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Case> GetCaseByNumberAsync(string caseNumber, CancellationToken ct)
    {
        return await _db.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");
    }

    private static TaskDto MapToDto(CaseTask t) => new()
    {
        Id = t.Id,
        CaseId = t.CaseId,
        CaseNumber = t.Case?.CaseNumber,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        Priority = t.Priority,
        AssignedTo = t.AssignedTo,
        AssignedToUserName = t.AssignedToUser != null ? $"{t.AssignedToUser.FirstName} {t.AssignedToUser.LastName}" : null,
        DueDate = t.DueDate,
        CompletedAt = t.CompletedAt,
        CreatedBy = t.CreatedBy,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
