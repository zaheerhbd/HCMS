using HCM.Application.Appointments.DataHandlers;
using HCM.Application.Appointments.DTOs;
using HCM.Application.Cases.DataHandlers;
using HCM.Application.Cases.DTOs;
using HCM.Application.Fhir.DTOs;
using HCM.Application.Interfaces;
using HCM.Application.Patients.DataHandlers;
using HCM.Application.Patients.DTOs;
using HCM.Application.Tasks.DataHandlers;
using HCM.Application.Tasks.DTOs;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HCM.Application.Fhir.DataHandlers;

public class FhirDataHandler : IFhirDataHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IFhirMappingService _mapping;
    private readonly IPatientDataHandler _patientHandler;
    private readonly ICaseDataHandler _caseHandler;
    private readonly IAppointmentDataHandler _appointmentHandler;
    private readonly ITaskDataHandler _taskHandler;
    private readonly ILogger<FhirDataHandler> _logger;

    public FhirDataHandler(
        IApplicationDbContext db,
        IFhirMappingService mapping,
        IPatientDataHandler patientHandler,
        ICaseDataHandler caseHandler,
        IAppointmentDataHandler appointmentHandler,
        ITaskDataHandler taskHandler,
        ILogger<FhirDataHandler> logger)
    {
        _db = db;
        _mapping = mapping;
        _patientHandler = patientHandler;
        _caseHandler = caseHandler;
        _appointmentHandler = appointmentHandler;
        _taskHandler = taskHandler;
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

    public async Task<FhirBundleImportResultDto> ImportTransactionBundleAsync(
        string bundleJson, string importedBy, CancellationToken ct = default)
    {
        var (isValid, parseError, parsedEntries) = _mapping.ParseTransactionBundle(bundleJson);
        if (!isValid)
            return new FhirBundleImportResultDto(false, [], [parseError!]);

        var createdByGuid = Guid.TryParse(importedBy, out var g) ? g : Guid.Empty;
        var results = new List<BundleEntryResultDto>();

        // urn:uuid: fullUrl → "ResourceType/AssignedId" for intra-bundle reference resolution
        var urnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var defaultCaseType = await _db.CaseTypes.OrderBy(t => t.Id).FirstOrDefaultAsync(ct);
        var defaultProvider = await _db.Providers.Where(p => p.IsActive).FirstOrDefaultAsync(ct);

        await using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            foreach (var entry in parsedEntries)
            {
                switch (entry.ResourceType)
                {
                    case "Patient":
                    {
                        var p = entry.Patient!;
                        var patient = await _patientHandler.CreatePatientAsync(new CreatePatientDto
                        {
                            FirstName = p.FirstName,
                            LastName = p.LastName,
                            DateOfBirth = p.DateOfBirth,
                            Gender = p.Gender,
                            Phone = p.Phone,
                            Email = p.Email,
                            Address = p.Address,
                            City = p.City,
                            State = p.State,
                            ZipCode = p.ZipCode
                        }, createdByGuid, ct);

                        results.Add(new BundleEntryResultDto("Patient", patient.MRN, "201 Created"));
                        if (!string.IsNullOrEmpty(entry.FullUrl))
                            urnMap[entry.FullUrl] = $"Patient/{patient.MRN}";
                        break;
                    }

                    case "Encounter":
                    {
                        var enc = entry.Encounter!;
                        var patientMrn = ResolveId("Patient", enc.SubjectRef, urnMap, results);

                        var caseTypeId = defaultCaseType?.Id ?? 1;
                        if (!string.IsNullOrEmpty(enc.TypeText))
                        {
                            var matched = await _db.CaseTypes.FirstOrDefaultAsync(t => t.Name == enc.TypeText, ct);
                            if (matched != null) caseTypeId = matched.Id;
                        }

                        // Resolve case-assigned-to extension to a user ID, falling back to the importer.
                        var assignedToGuid = createdByGuid;
                        if (!string.IsNullOrEmpty(enc.AssignedToUsername))
                        {
                            var assignee = await _db.Users.FirstOrDefaultAsync(
                                u => u.Username == enc.AssignedToUsername, ct);
                            if (assignee != null)
                                assignedToGuid = assignee.Id;
                        }

                        var createdCase = await _caseHandler.CreateCaseAsync(
                            patientMrn,
                            new CreateCaseDto { CaseTypeId = caseTypeId },
                            createdByGuid, assignedToGuid, ct);

                        results.Add(new BundleEntryResultDto("Encounter", createdCase.CaseNumber, "201 Created"));
                        if (!string.IsNullOrEmpty(entry.FullUrl))
                            urnMap[entry.FullUrl] = $"Encounter/{createdCase.CaseNumber}";
                        break;
                    }

                    case "Appointment":
                    {
                        var appt = entry.Appointment!;
                        var patientMrn = ResolveId("Patient", appt.PatientRef, urnMap, results);
                        var caseNumber = results.FirstOrDefault(r => r.ResourceType == "Encounter")?.AssignedId;

                        var providerId = defaultProvider?.Id ?? Guid.Empty;
                        if (!string.IsNullOrEmpty(appt.PractitionerRef))
                        {
                            var practId = appt.PractitionerRef.Contains('/') ? appt.PractitionerRef.Split('/').Last() : appt.PractitionerRef;
                            if (Guid.TryParse(practId, out var pGuid))
                            {
                                var p = await _db.Providers.FirstOrDefaultAsync(pr => pr.Id == pGuid, ct);
                                if (p != null) providerId = p.Id;
                            }
                        }

                        var createdAppt = await _appointmentHandler.CreateAppointmentAsync(new CreateAppointmentDto
                        {
                            PatientMrn = patientMrn,
                            CaseNumber = caseNumber,
                            ProviderId = providerId,
                            AppointmentType = appt.ServiceType ?? "General",
                            ScheduledStart = appt.Start ?? DateTime.UtcNow.AddDays(1),
                            ScheduledEnd = appt.End ?? DateTime.UtcNow.AddDays(1).AddHours(1)
                        }, createdByGuid, ct);

                        results.Add(new BundleEntryResultDto("Appointment", createdAppt.Id.ToString(), "201 Created"));
                        if (!string.IsNullOrEmpty(entry.FullUrl))
                            urnMap[entry.FullUrl] = $"Appointment/{createdAppt.Id}";
                        break;
                    }

                    case "Task":
                    {
                        var task = entry.Task!;
                        var caseNumber = results.FirstOrDefault(r => r.ResourceType == "Encounter")?.AssignedId
                            ?? throw new InvalidOperationException("A Task entry requires an Encounter entry in the same bundle.");

                        var priority = task.Priority?.ToLower() switch
                        {
                            "urgent" or "asap" => "High",
                            "routine" or "stat" => "Low",
                            _ => "Medium"
                        };

                        var createdTask = await _taskHandler.CreateTaskAsync(caseNumber, new CreateTaskDto
                        {
                            Title = task.Description ?? "FHIR imported task",
                            Priority = priority,
                            DueDate = task.DueDate
                        }, createdByGuid, ct);

                        results.Add(new BundleEntryResultDto("Task", createdTask.Id.ToString(), "201 Created"));
                        if (!string.IsNullOrEmpty(entry.FullUrl))
                            urnMap[entry.FullUrl] = $"Task/{createdTask.Id}";
                        break;
                    }
                }
            }

            // Write FhirImportLogs and AuditLog inside the transaction so the entire batch is atomic.
            var patientEntry = results.FirstOrDefault(r => r.ResourceType == "Patient");
            foreach (var r in results)
            {
                _db.FhirImportLogs.Add(new FhirImportLog
                {
                    ResourceType = r.ResourceType,
                    SourceSystem = "FHIR-Transaction",
                    ImportedBy = importedBy,
                    ImportedAt = DateTime.UtcNow,
                    Status = "Success",
                    InternalId = r.AssignedId
                });
            }

            if (patientEntry != null)
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    EntityType = "Patient",
                    EntityId = patientEntry.AssignedId,
                    Action = "FhirBundleImport",
                    ChangedBy = importedBy,
                    ChangedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new FhirBundleImportResultDto(true, results, []);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "FHIR Transaction Bundle import failed");
            return new FhirBundleImportResultDto(false, results, [ex.Message]);
        }
    }

    // Resolves a FHIR reference (urn:uuid: or ResourceType/Id) to the target resource's assigned ID.
    private static string ResolveId(
        string resourceType, string? reference,
        Dictionary<string, string> urnMap,
        List<BundleEntryResultDto> results)
    {
        if (!string.IsNullOrEmpty(reference))
        {
            if (urnMap.TryGetValue(reference, out var resolved))
                return resolved.Split('/').Last();

            if (reference.Contains('/'))
                return reference.Split('/').Last();
        }

        return results.FirstOrDefault(r => r.ResourceType == resourceType)?.AssignedId
            ?? throw new InvalidOperationException($"Cannot resolve {resourceType} reference '{reference}' — no matching entry found in bundle.");
    }
}
