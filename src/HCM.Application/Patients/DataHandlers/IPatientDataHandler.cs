using HCM.Application.Patients.DTOs;

namespace HCM.Application.Patients.DataHandlers;

public interface IPatientDataHandler
{
    Task<PatientDto> GetPatientByMrnAsync(string mrn, CancellationToken ct = default);
    Task<PatientSearchDto> SearchPatientsAsync(string? search, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<PatientDto> CreatePatientAsync(CreatePatientDto dto, Guid createdBy, CancellationToken ct = default);
    Task<PatientDto> UpdatePatientAsync(string mrn, UpdatePatientDto dto, CancellationToken ct = default);
    Task DeletePatientAsync(string mrn, CancellationToken ct = default);
}
