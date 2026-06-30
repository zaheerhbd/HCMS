using HCM.Application.Cases.DTOs;

namespace HCM.Application.Cases.DataHandlers;

public interface ICaseDataHandler
{
    Task<(List<CaseListItemDto> Items, int TotalCount)> GetAllCasesAsync(string? statusFilter, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<CaseDto> GetCaseByCaseNumberAsync(string caseNumber, CancellationToken ct = default);
    Task<(List<CaseListItemDto> Items, int TotalCount)> GetCasesByPatientMrnAsync(string patientMrn, string? statusFilter, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<CaseDto> CreateCaseAsync(string patientMrn, CreateCaseDto dto, Guid createdBy, Guid assignedTo, CancellationToken ct = default);
    Task<CaseDto> ChangeCaseStatusAsync(string caseNumber, CaseStatusChangeDto dto, Guid changedBy, CancellationToken ct = default);
    Task<CaseDto> UpdateCaseAsync(string caseNumber, UpdateCaseDto dto, CancellationToken ct = default);
    Task CloseCaseAsync(string caseNumber, Guid closedBy, CancellationToken ct = default);
}
