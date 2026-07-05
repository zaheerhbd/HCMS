using HCM.Application.Reports.DTOs;

namespace HCM.Application.Reports.DataHandlers;

public interface IReportDataHandler
{
    Task<CasesByStatusReportDto> GetCasesByStatusAsync(CancellationToken ct = default);
    Task<CaseDurationReportDto> GetCaseDurationAsync(CancellationToken ct = default);
    Task<TasksByAssigneeReportDto> GetTasksByAssigneeAsync(CancellationToken ct = default);

    Task<string> GetCasesByStatusCsvAsync(CancellationToken ct = default);
    Task<string> GetCaseDurationCsvAsync(CancellationToken ct = default);
    Task<string> GetTasksByAssigneeCsvAsync(CancellationToken ct = default);
}
