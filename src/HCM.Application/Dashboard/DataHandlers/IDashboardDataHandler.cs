using HCM.Application.Dashboard.DTOs;

namespace HCM.Application.Dashboard.DataHandlers;

public interface IDashboardDataHandler
{
    Task<CoordinatorDashboardDto> GetCoordinatorDashboardAsync(Guid userId, CancellationToken ct = default);
    Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(CancellationToken ct = default);
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken ct = default);
}
