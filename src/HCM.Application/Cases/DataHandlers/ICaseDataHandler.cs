using HCM.Application.Cases.DTOs;

namespace HCM.Application.Cases.DataHandlers;

public interface ICaseDataHandler
{
    Task<CaseDto> GetCaseByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<CaseListItemDto> Items, int TotalCount)> GetCasesByPatientAsync(Guid patientId, string? statusFilter, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<CaseDto> CreateCaseAsync(Guid patientId, CreateCaseDto dto, Guid createdBy, Guid assignedTo, CancellationToken ct = default);
    Task<CaseDto> ChangeCaseStatusAsync(Guid caseId, CaseStatusChangeDto dto, Guid changedBy, CancellationToken ct = default);
    Task<CaseDto> UpdateCaseAsync(Guid caseId, UpdateCaseDto dto, CancellationToken ct = default);
    Task CloseCaseAsync(Guid caseId, Guid closedBy, CancellationToken ct = default);
}
