using FluentValidation;
using HCM.Application.Interfaces;
using HCM.Application.Patients.DTOs;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Patients.DataHandlers;

public class PatientDataHandler : IPatientDataHandler
{
    private readonly IApplicationDbContext _dbContext;

    public PatientDataHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PatientDto> GetPatientByIdAsync(Guid id, CancellationToken ct = default)
    {
        var patient = await _dbContext.Patients
            .AsNoTracking()
            .Include(p => p.Insurance)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Patient with ID {id} not found.");

        return MapToDto(patient);
    }

    public async Task<PatientSearchDto> SearchPatientsAsync(string? search, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _dbContext.Patients
            .Where(p => p.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(searchLower) ||
                p.LastName.ToLower().Contains(searchLower) ||
                p.MRN.Contains(search) ||
                p.Insurance.Any(pi => pi.MemberId.Contains(search))
            );
        }

        var totalCount = await query.CountAsync(ct);
        var patients = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Insurance)
            .ToListAsync(ct);

        return new PatientSearchDto
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Results = patients.Select(p => new PatientListItemDto
            {
                Id = p.Id,
                MRN = p.MRN,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                Phone = p.Phone,
                Email = p.Email
            }).ToList()
        };
    }

    public async Task<PatientDto> CreatePatientAsync(CreatePatientDto dto, Guid createdBy, CancellationToken ct = default)
    {
        ValidatePatientInput(dto);

        var mrn = await GenerateMrnAsync(ct);
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            MRN = mrn,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            ZipCode = dto.ZipCode?.Trim(),
            EmergencyContactName = dto.EmergencyContactName?.Trim(),
            EmergencyContactPhone = dto.EmergencyContactPhone?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(patient);
    }

    public async Task<PatientDto> UpdatePatientAsync(Guid id, UpdatePatientDto dto, CancellationToken ct = default)
    {
        ValidatePatientInput(dto);

        var patient = await _dbContext.Patients
            .Include(p => p.Insurance)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Patient with ID {id} not found.");

        patient.FirstName = dto.FirstName.Trim();
        patient.LastName = dto.LastName.Trim();
        patient.DateOfBirth = dto.DateOfBirth;
        patient.Gender = dto.Gender;
        patient.Phone = dto.Phone?.Trim();
        patient.Email = dto.Email?.Trim();
        patient.Address = dto.Address?.Trim();
        patient.City = dto.City?.Trim();
        patient.State = dto.State?.Trim();
        patient.ZipCode = dto.ZipCode?.Trim();
        patient.EmergencyContactName = dto.EmergencyContactName?.Trim();
        patient.EmergencyContactPhone = dto.EmergencyContactPhone?.Trim();
        patient.UpdatedAt = DateTime.UtcNow;

        _dbContext.Patients.Update(patient);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(patient);
    }

    public async Task DeletePatientAsync(Guid id, CancellationToken ct = default)
    {
        var patient = await _dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken: ct)
            ?? throw new KeyNotFoundException($"Patient with ID {id} not found.");

        patient.IsActive = false;
        patient.UpdatedAt = DateTime.UtcNow;

        _dbContext.Patients.Update(patient);
        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateMrnAsync(CancellationToken ct)
    {
        var currentYear = DateTime.UtcNow.Year;
        var lastMrn = await _dbContext.Patients
            .AsNoTracking()
            .Where(p => p.MRN.StartsWith($"MRN-{currentYear}-"))
            .OrderByDescending(p => p.MRN)
            .FirstOrDefaultAsync(cancellationToken: ct);

        int nextSequence = 1;
        if (lastMrn != null && int.TryParse(lastMrn.MRN.Substring(8), out int lastSequence))
        {
            nextSequence = lastSequence + 1;
        }

        return $"MRN-{currentYear}-{nextSequence:D5}";
    }

    private void ValidatePatientInput(CreatePatientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName))
            throw new ValidationException("First name is required.");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            throw new ValidationException("Last name is required.");
        if (dto.DateOfBirth >= DateTime.UtcNow.Date)
            throw new ValidationException("Date of birth must be in the past.");
        if (!string.IsNullOrEmpty(dto.Email) && !IsValidEmail(dto.Email))
            throw new ValidationException("Invalid email format.");
    }

    private void ValidatePatientInput(UpdatePatientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName))
            throw new ValidationException("First name is required.");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            throw new ValidationException("Last name is required.");
        if (dto.DateOfBirth >= DateTime.UtcNow.Date)
            throw new ValidationException("Date of birth must be in the past.");
        if (!string.IsNullOrEmpty(dto.Email) && !IsValidEmail(dto.Email))
            throw new ValidationException("Invalid email format.");
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private PatientDto MapToDto(Patient patient)
    {
        return new PatientDto
        {
            Id = patient.Id,
            MRN = patient.MRN,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Phone = patient.Phone,
            Email = patient.Email,
            Address = patient.Address,
            City = patient.City,
            State = patient.State,
            ZipCode = patient.ZipCode,
            EmergencyContactName = patient.EmergencyContactName,
            EmergencyContactPhone = patient.EmergencyContactPhone,
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt,
            Insurance = patient.Insurance?.Select(pi => new PatientInsuranceDto
            {
                Id = pi.Id,
                InsurancePlan = pi.InsurancePlan,
                MemberId = pi.MemberId,
                GroupNumber = pi.GroupNumber,
                SubscriberName = pi.SubscriberName,
                EffectiveDate = pi.EffectiveDate,
                TerminationDate = pi.TerminationDate,
                IsPrimary = pi.IsPrimary
            }).ToList() ?? []
        };
    }
}
