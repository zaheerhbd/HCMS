using HCM.Application.Audit.DTOs;

namespace HCM.Application.Audit.DataHandlers;

public interface IAuditDataHandler
{
    Task<AuditLogListDto> GetAuditLogsAsync(AuditLogFilterDto filter, CancellationToken ct = default);
}
