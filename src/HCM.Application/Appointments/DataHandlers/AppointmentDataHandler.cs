using FluentValidation;
using HCM.Application.Appointments.DTOs;
using HCM.Application.Interfaces;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Appointments.DataHandlers;

public class AppointmentDataHandler : IAppointmentDataHandler
{
    private readonly IApplicationDbContext _db;

    public AppointmentDataHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(List<AppointmentDto> Items, int TotalCount)> GetAllAppointmentsAsync(
        string? status, string? patientMrn, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Include(a => a.Case)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrWhiteSpace(patientMrn))
            query = query.Where(a => a.Patient.MRN == patientMrn);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.ScheduledStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.Select(a => MapToDto(a)).ToList(), totalCount);
    }

    public async Task<List<AppointmentDto>> GetAppointmentsByPatientMrnAsync(string patientMrn, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MRN == patientMrn && p.IsActive, ct)
            ?? throw new KeyNotFoundException($"Patient {patientMrn} not found.");

        var appointments = await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Provider)
            .Include(a => a.Case)
            .Where(a => a.PatientId == patient.Id)
            .OrderByDescending(a => a.ScheduledStart)
            .ToListAsync(ct);

        return appointments.Select(a => MapToDto(a, patient)).ToList();
    }

    public async Task<AppointmentDto> GetAppointmentByIdAsync(Guid id, CancellationToken ct = default)
    {
        var appointment = await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Include(a => a.Case)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        return MapToDto(appointment);
    }

    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto, Guid createdBy, CancellationToken ct = default)
    {
        ValidateAppointmentTimes(dto.ScheduledStart, dto.ScheduledEnd);

        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MRN == dto.PatientMrn && p.IsActive, ct)
            ?? throw new KeyNotFoundException($"Patient {dto.PatientMrn} not found.");

        var provider = await _db.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.ProviderId && p.IsActive, ct)
            ?? throw new KeyNotFoundException($"Provider {dto.ProviderId} not found.");

        Guid? caseId = null;
        if (!string.IsNullOrWhiteSpace(dto.CaseNumber))
        {
            var caseEntity = await _db.Cases
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CaseNumber == dto.CaseNumber && c.IsActive, ct)
                ?? throw new KeyNotFoundException($"Case {dto.CaseNumber} not found.");
            caseId = caseEntity.Id;
        }

        // Provider double-booking check (FR-31)
        var conflict = await _db.Appointments
            .AsNoTracking()
            .AnyAsync(a =>
                a.ProviderId == dto.ProviderId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.ScheduledStart < dto.ScheduledEnd &&
                a.ScheduledEnd > dto.ScheduledStart, ct);

        if (conflict)
            throw new ValidationException("Provider already has an appointment during this time slot.");

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            CaseId = caseId,
            ProviderId = dto.ProviderId,
            AppointmentType = dto.AppointmentType,
            Status = AppointmentStatus.Scheduled,
            ScheduledStart = dto.ScheduledStart,
            ScheduledEnd = dto.ScheduledEnd,
            Location = dto.Location?.Trim(),
            Notes = dto.Notes?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(ct);

        return await GetAppointmentByIdAsync(appointment.Id, ct);
    }

    public async Task<AppointmentDto> UpdateAppointmentAsync(Guid id, UpdateAppointmentDto dto, CancellationToken ct = default)
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new ValidationException("Cannot edit a completed or cancelled appointment.");

        var newStart = dto.ScheduledStart ?? appointment.ScheduledStart;
        var newEnd = dto.ScheduledEnd ?? appointment.ScheduledEnd;
        ValidateAppointmentTimes(newStart, newEnd);

        if (dto.AppointmentType != null) appointment.AppointmentType = dto.AppointmentType;
        if (dto.ScheduledStart.HasValue) appointment.ScheduledStart = dto.ScheduledStart.Value;
        if (dto.ScheduledEnd.HasValue) appointment.ScheduledEnd = dto.ScheduledEnd.Value;
        if (dto.Location != null) appointment.Location = dto.Location.Trim();
        if (dto.Notes != null) appointment.Notes = dto.Notes.Trim();

        appointment.UpdatedAt = DateTime.UtcNow;
        _db.Appointments.Update(appointment);
        await _db.SaveChangesAsync(ct);

        return await GetAppointmentByIdAsync(appointment.Id, ct);
    }

    public async Task<AppointmentDto> ChangeAppointmentStatusAsync(Guid id, AppointmentStatusChangeDto dto, CancellationToken ct = default)
    {
        if (!AppointmentStatus.AllStatuses.Contains(dto.NewStatus))
            throw new ValidationException($"Invalid appointment status: {dto.NewStatus}.");

        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        appointment.Status = dto.NewStatus;
        appointment.UpdatedAt = DateTime.UtcNow;
        _db.Appointments.Update(appointment);
        await _db.SaveChangesAsync(ct);

        return await GetAppointmentByIdAsync(appointment.Id, ct);
    }

    public async Task<List<ProviderDto>> GetProvidersAsync(CancellationToken ct = default)
    {
        var providers = await _db.Providers
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return providers.Select(p => new ProviderDto
        {
            Id = p.Id,
            UserId = p.UserId,
            Name = p.Name,
            Specialty = p.Specialty,
            IsActive = p.IsActive
        }).ToList();
    }

    private static void ValidateAppointmentTimes(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new ValidationException("Appointment end time must be after start time.");
        if (start < DateTime.UtcNow.AddMinutes(-5))
            throw new ValidationException("Cannot schedule appointments in the past.");
    }

    private static AppointmentDto MapToDto(Appointment a, Patient? patient = null) => new()
    {
        Id = a.Id,
        PatientId = a.PatientId,
        PatientName = patient != null ? patient.FullName : (a.Patient != null ? a.Patient.FullName : ""),
        PatientMrn = patient != null ? patient.MRN : (a.Patient != null ? a.Patient.MRN : ""),
        CaseId = a.CaseId,
        CaseNumber = a.Case?.CaseNumber,
        ProviderId = a.ProviderId,
        ProviderName = a.Provider?.Name ?? "",
        AppointmentType = a.AppointmentType,
        Status = a.Status,
        ScheduledStart = a.ScheduledStart,
        ScheduledEnd = a.ScheduledEnd,
        Location = a.Location,
        Notes = a.Notes,
        CreatedBy = a.CreatedBy,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
