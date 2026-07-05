using HCM.Application.Hl7.DTOs;
using HCM.Application.Interfaces;
using HCM.Application.Patients.DataHandlers;
using HCM.Application.Patients.DTOs;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HCM.Application.Hl7.DataHandlers;

public class Hl7DataHandler : IHl7DataHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IHl7MappingService _hl7Mapping;
    private readonly IPatientDataHandler _patientHandler;
    private readonly ILogger<Hl7DataHandler> _logger;

    public Hl7DataHandler(
        IApplicationDbContext db,
        IHl7MappingService hl7Mapping,
        IPatientDataHandler patientHandler,
        ILogger<Hl7DataHandler> logger)
    {
        _db = db;
        _hl7Mapping = hl7Mapping;
        _patientHandler = patientHandler;
        _logger = logger;
    }

    public async Task<Hl7AdtResultDto> ProcessAdtA01Async(
        string hl7Message, string processedBy, CancellationToken ct = default)
    {
        Hl7PatientData parsed;
        try
        {
            parsed = _hl7Mapping.ParseAdtA01(hl7Message);
        }
        catch (Exception ex)
        {
            return new Hl7AdtResultDto(false, null, $"Parse error: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(parsed.FirstName) || string.IsNullOrWhiteSpace(parsed.LastName) || !parsed.DateOfBirth.HasValue)
            return new Hl7AdtResultDto(false, null, "ADT message is missing required patient demographics (name or date of birth).");

        var createdByGuid = Guid.TryParse(processedBy, out var g) ? g : Guid.Empty;

        try
        {
            var patient = await _patientHandler.CreatePatientAsync(new CreatePatientDto
            {
                FirstName = parsed.FirstName,
                LastName = parsed.LastName,
                DateOfBirth = parsed.DateOfBirth.Value,
                Gender = parsed.Gender,
                Phone = parsed.Phone
            }, createdByGuid, ct);

            _db.FhirImportLogs.Add(new FhirImportLog
            {
                ResourceType = "Patient",
                SourceSystem = "HL7v2-ADT",
                ImportedBy = processedBy,
                ImportedAt = DateTime.UtcNow,
                Status = "Success",
                InternalId = patient.MRN
            });
            await _db.SaveChangesAsync(ct);

            return new Hl7AdtResultDto(true, patient.MRN, $"Patient created from ADT^A01. MRN: {patient.MRN}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create patient from HL7 ADT^A01");
            return new Hl7AdtResultDto(false, null, ex.Message);
        }
    }

    public async Task<Hl7OruResultDto> ProcessOruR01Async(
        string hl7Message, string processedBy, CancellationToken ct = default)
    {
        Hl7ObservationData parsed;
        try
        {
            parsed = _hl7Mapping.ParseOruR01(hl7Message);
        }
        catch (Exception ex)
        {
            return new Hl7OruResultDto(false, null, $"Parse error: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(parsed.PatientId))
            return new Hl7OruResultDto(false, null, "ORU message is missing a patient identifier (PID-3).");

        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.MRN == parsed.PatientId && p.IsActive, ct);
        if (patient == null)
            return new Hl7OruResultDto(false, null, $"Patient '{parsed.PatientId}' not found.");

        var activeCase = await _db.Cases
            .Where(c => c.PatientId == patient.Id && c.IsActive && c.CurrentStatus != "Closed")
            .OrderByDescending(c => c.OpenedAt)
            .FirstOrDefaultAsync(ct);

        if (activeCase == null)
            return new Hl7OruResultDto(false, null, $"No active case found for patient {parsed.PatientId}.");

        var noteText = $"HL7 ORU^R01 Observation — {parsed.ObservationType}: {parsed.ObservationValue}" +
                       (string.IsNullOrEmpty(parsed.Units) ? "" : $" {parsed.Units}");

        var createdByGuid = Guid.TryParse(processedBy, out var g) ? g : Guid.Empty;
        var note = new CaseNote
        {
            Id = Guid.NewGuid(),
            CaseId = activeCase.Id,
            Content = noteText,
            CreatedBy = createdByGuid,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsEditable = false
        };

        _db.CaseNotes.Add(note);
        _db.FhirImportLogs.Add(new FhirImportLog
        {
            ResourceType = "Observation",
            SourceSystem = "HL7v2-ORU",
            ImportedBy = processedBy,
            ImportedAt = DateTime.UtcNow,
            Status = "Success",
            InternalId = note.Id.ToString()
        });
        await _db.SaveChangesAsync(ct);

        return new Hl7OruResultDto(true, note.Id.ToString(), $"Observation note created on case {activeCase.CaseNumber}.");
    }
}
