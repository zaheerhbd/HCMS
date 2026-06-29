using HCM.Application.Patients.DTOs;

namespace HCM.Application.Patients.DataHandlers;

public interface IPatientDataHandler
{
    Task<PatientDto> GetPatientByIdAsync(Guid id, CancellationToken ct = default);
    Task<PatientSearchDto> SearchPatientsAsync(string? search, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<PatientDto> CreatePatientAsync(CreatePatientDto dto, Guid createdBy, CancellationToken ct = default);
    Task<PatientDto> UpdatePatientAsync(Guid id, UpdatePatientDto dto, CancellationToken ct = default);
    Task DeletePatientAsync(Guid id, CancellationToken ct = default);
}
