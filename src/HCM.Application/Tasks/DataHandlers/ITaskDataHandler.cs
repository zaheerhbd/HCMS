using HCM.Application.Tasks.DTOs;

namespace HCM.Application.Tasks.DataHandlers;

public interface ITaskDataHandler
{
    Task<List<TaskDto>> GetTasksByCaseNumberAsync(string caseNumber, CancellationToken ct = default);
    Task<TaskDto> GetTaskByIdAsync(Guid taskId, CancellationToken ct = default);
    Task<List<TaskDto>> GetMyTasksAsync(Guid userId, CancellationToken ct = default);
    Task<TaskDto> CreateTaskAsync(string caseNumber, CreateTaskDto dto, Guid createdBy, CancellationToken ct = default);
    Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, CancellationToken ct = default);
    Task<TaskDto> ChangeTaskStatusAsync(Guid taskId, TaskStatusChangeDto dto, Guid changedBy, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid taskId, CancellationToken ct = default);
}
