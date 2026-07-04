using HCM.Application.Fhir.DTOs;
using HCM.Application.Interfaces;
using HCM.Application.Patients.DataHandlers;
using HCM.Application.Patients.DTOs;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HCM.Application.Fhir.DataHandlers;

public class FhirDataHandler : IFhirDataHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IFhirMappingService _mapping;
    private readonly IPatientDataHandler _patientHandler;
    private readonly ILogger<FhirDataHandler> _logger;

    public FhirDataHandler(
        IApplicationDbContext db,
        IFhirMappingService mapping,
        IPatientDataHandler patientHandler,
        ILogger<FhirDataHandler> logger)
    {
        _db = db;
        _mapping = mapping;
        _patientHandler = patientHandler;
        _logger = logger;
    }

    public Task<string> GetCapabilityStatementAsync(string baseUrl, CancellationToken ct = default)
        => Task.FromResult(_mapping.GetCapabilityStatement(baseUrl));

    public async Task<string> SearchPatientsAsync(
        string? id, string? name, string? birthdate, string? identifier,
        string baseUrl, CancellationToken ct = default)
    {
        var query = _db.Patients.Where(p => p.IsActive).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(id))
            query = query.Where(p => p.MRN == id);

        if (!string.IsNullOrWhiteSpace(name))
        {
            var lower = name.ToLower();
            query = query.Where(p => p.FirstName.ToLower().Contains(lower) || p.LastName.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(birthdate) && DateTime.TryParse(birthdate, out var dob))
            query = query.Where(p => p.DateOfBirth.Date == dob.Date);

        if (!string.IsNullOrWhiteSpace(identifier))
        {
            // FHIR clients may send "system|value" (e.g. "urn:hcms:mrn|MRN-2026-00001"); strip the system prefix.
            var mrnValue = identifier.Contains('|') ? identifier.Split('|').Last() : identifier;
            query = query.Where(p => p.MRN == mrnValue);
        }

        var patients = await query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(ct);
        return _mapping.MapPatientsBundle(patients, patients.Count, $"{baseUrl}/fhir/R4/Patient");
    }

    public async Task<string> GetPatientAsync(string mrn, string baseUrl, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MRN == mrn && p.IsActive, ct)
            ?? throw new KeyNotFoundException($"Patient '{mrn}' not found.");

        return _mapping.MapPatientToJson(patient);
    }

    public async Task<string> GetEverythingAsync(string mrn, string baseUrl, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MRN == mrn && p.IsActive, ct)
            ?? throw new KeyNotFoundException($"Patient '{mrn}' not found.");

        var cases = await _db.Cases
            .AsNoTracking()
            .Include(c => c.CaseType)
            .Where(c => c.PatientId == patient.Id && c.IsActive)
            .ToListAsync(ct);

        var caseIds = cases.Select(c => c.Id).ToHashSet();

        var appointments = await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Provider)
            .Where(a => a.PatientId == patient.Id)
            .ToListAsync(ct);

        var tasks = await _db.CaseTasks
            .AsNoTracking()
            .Where(t => caseIds.Contains(t.CaseId))
            .ToListAsync(ct);

        var documents = await _db.Documents
            .AsNoTracking()
            .Where(d => d.PatientId == patient.Id && !d.IsDeleted)
            .ToListAsync(ct);

        // Log FHIR export to audit trail (FR requirement)
        _db.AuditLogs.Add(new AuditLog
        {
            EntityType = "Patient",
            EntityId = patient.MRN,
            Action = "FhirExport",
            ChangedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return _mapping.MapEverythingBundle(
            patient, cases, appointments, tasks, documents,
            $"{baseUrl}/fhir/R4/Patient/{mrn}/$everything");
    }

    public async Task<FhirImportResultDto> ImportPatientAsync(
        string fhirJson, string importedBy, CancellationToken ct = default)
    {
        var importLog = new FhirImportLog
        {
            ResourceType = "Patient",
            SourceSystem = "FHIR-HTTP",
            ImportedBy = importedBy,
            ImportedAt = DateTime.UtcNow,
            RawPayload = fhirJson
        };

        try
        {
            var (parsedPatient, parseError) = _mapping.ParsePatientFromFhir(fhirJson);

            if (parsedPatient == null)
            {
                importLog.Status = "Failed";
                importLog.ErrorMessage = parseError;
                _db.FhirImportLogs.Add(importLog);
                await _db.SaveChangesAsync(ct);
                return new FhirImportResultDto { Success = false, Errors = [parseError ?? "Failed to parse FHIR Patient."] };
            }

            var createdByGuid = Guid.TryParse(importedBy, out var g) ? g : Guid.Empty;
            var createDto = new CreatePatientDto
            {
                FirstName = parsedPatient.FirstName,
                LastName = parsedPatient.LastName,
                DateOfBirth = parsedPatient.DateOfBirth,
                Gender = parsedPatient.Gender,
                Phone = parsedPatient.Phone,
                Email = parsedPatient.Email,
                Address = parsedPatient.Address,
                City = parsedPatient.City,
                State = parsedPatient.State,
                ZipCode = parsedPatient.ZipCode
            };

            var created = await _patientHandler.CreatePatientAsync(createDto, createdByGuid, ct);

            importLog.Status = "Success";
            importLog.FhirResourceId = parsedPatient.MRN; // FHIR id from the incoming resource
            importLog.InternalId = created.MRN;
            _db.FhirImportLogs.Add(importLog);
            await _db.SaveChangesAsync(ct);

            return new FhirImportResultDto
            {
                Success = true,
                Mrn = created.MRN,
                Message = $"Patient imported successfully. MRN: {created.MRN}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FHIR import failed for patient");
            importLog.Status = "Failed";
            importLog.ErrorMessage = ex.Message;
            _db.FhirImportLogs.Add(importLog);
            await _db.SaveChangesAsync(ct);
            return new FhirImportResultDto { Success = false, Errors = [ex.Message] };
        }
    }

    public async Task<string> SearchEncountersAsync(
        string? patientMrn, string? status, string baseUrl, CancellationToken ct = default)
    {
        var query = _db.Cases
            .AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.CaseType)
            .Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(patientMrn))
            query = query.Where(c => c.Patient.MRN == patientMrn);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.CurrentStatus == status);

        var cases = await query.OrderByDescending(c => c.OpenedAt).ToListAsync(ct);
        return _mapping.MapEncountersBundle(cases, cases.Count, $"{baseUrl}/fhir/R4/Encounter");
    }

    public async Task<string> GetEncounterAsync(string caseNumber, string baseUrl, CancellationToken ct = default)
    {
        var c = await _db.Cases
            .AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.CaseType)
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, ct)
            ?? throw new KeyNotFoundException($"Encounter '{caseNumber}' not found.");

        return _mapping.MapEncounterToJson(c);
    }

    public async Task<string> SearchAppointmentsAsync(
        string? id, string? patientMrn, string? status,
        string baseUrl, CancellationToken ct = default)
    {
        var query = _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .AsQueryable();

        // _id: match by internal GUID
        if (!string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out var idGuid))
            query = query.Where(a => a.Id == idGuid);

        // patient: filter by patient MRN
        if (!string.IsNullOrWhiteSpace(patientMrn))
            query = query.Where(a => a.Patient != null && a.Patient.MRN == patientMrn);

        // status: map FHIR status back to domain status
        if (!string.IsNullOrWhiteSpace(status))
        {
            string[] domainStatuses = status.ToLower() switch
            {
                "booked"    => ["Scheduled", "Confirmed"],
                "fulfilled" => ["Completed"],
                "noshow"    => ["NoShow"],
                "cancelled" => ["Cancelled"],
                _           => []
            };
            if (domainStatuses.Length > 0)
                query = query.Where(a => domainStatuses.Contains(a.Status));
        }

        var appointments = await query.OrderBy(a => a.ScheduledStart).ToListAsync(ct);
        return _mapping.MapAppointmentsBundle(appointments, appointments.Count, $"{baseUrl}/fhir/R4/Appointment");
    }

    public async Task<string> GetAppointmentAsync(string id, string baseUrl, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new KeyNotFoundException($"Appointment '{id}' not found.");

        var appt = await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == guid, ct)
            ?? throw new KeyNotFoundException($"Appointment '{id}' not found.");

        return _mapping.MapAppointmentToJson(appt);
    }

    public async Task<string> SearchTasksAsync(
        string? id, string? patientMrn, string? focus, string? status,
        string baseUrl, CancellationToken ct = default)
    {
        var query = _db.CaseTasks
            .AsNoTracking()
            .Include(t => t.Case).ThenInclude(c => c!.Patient)
            .AsQueryable();

        // _id: match by internal GUID
        if (!string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out var idGuid))
            query = query.Where(t => t.Id == idGuid);

        // patient: filter by patient MRN (via Case → Patient)
        if (!string.IsNullOrWhiteSpace(patientMrn))
            query = query.Where(t => t.Case != null && t.Case.Patient != null && t.Case.Patient.MRN == patientMrn);

        // focus: filter by case number (FHIR Task.focus references the Encounter/Case)
        if (!string.IsNullOrWhiteSpace(focus))
        {
            // Accept "CASE-2026-00001" or "Encounter/CASE-2026-00001" formats
            var caseNumber = focus.Contains('/') ? focus.Split('/').Last() : focus;
            query = query.Where(t => t.Case != null && t.Case.CaseNumber == caseNumber);
        }

        // status: map FHIR status back to domain status
        if (!string.IsNullOrWhiteSpace(status))
        {
            var domainStatus = status.ToLower() switch
            {
                "requested"  => "Todo",
                "in-progress" => "InProgress",
                "on-hold"    => "Blocked",
                "completed"  => "Done",
                "cancelled"  => "Cancelled",
                _ => null
            };
            if (domainStatus != null)
                query = query.Where(t => t.Status == domainStatus);
        }

        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
        return _mapping.MapTasksBundle(tasks, tasks.Count, $"{baseUrl}/fhir/R4/Task");
    }

    public async Task<string> GetTaskAsync(string id, string baseUrl, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new KeyNotFoundException($"Task '{id}' not found.");

        var task = await _db.CaseTasks
            .AsNoTracking()
            .Include(t => t.Case)
            .FirstOrDefaultAsync(t => t.Id == guid, ct)
            ?? throw new KeyNotFoundException($"Task '{id}' not found.");

        return _mapping.MapTaskToJson(task);
    }

    public async Task<string> GetDocumentReferenceAsync(string id, string baseUrl, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new KeyNotFoundException($"DocumentReference '{id}' not found.");

        var doc = await _db.Documents
            .AsNoTracking()
            .Include(d => d.Patient)
            .FirstOrDefaultAsync(d => d.Id == guid && !d.IsDeleted, ct)
            ?? throw new KeyNotFoundException($"DocumentReference '{id}' not found.");

        return _mapping.MapDocumentReferenceToJson(doc);
    }
}
