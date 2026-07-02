using HCM.Application.Appointments.DTOs;

namespace HCM.Application.Appointments.DataHandlers;

public interface IAppointmentDataHandler
{
    Task<(List<AppointmentDto> Items, int TotalCount)> GetAllAppointmentsAsync(string? status, string? patientMrn, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<List<AppointmentDto>> GetAppointmentsByPatientMrnAsync(string patientMrn, CancellationToken ct = default);
    Task<AppointmentDto> GetAppointmentByIdAsync(Guid id, CancellationToken ct = default);
    Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto, Guid createdBy, CancellationToken ct = default);
    Task<AppointmentDto> UpdateAppointmentAsync(Guid id, UpdateAppointmentDto dto, CancellationToken ct = default);
    Task<AppointmentDto> ChangeAppointmentStatusAsync(Guid id, AppointmentStatusChangeDto dto, CancellationToken ct = default);
    Task<List<ProviderDto>> GetProvidersAsync(CancellationToken ct = default);
}
