using HCM.Application.Hl7.DTOs;

namespace HCM.Application.Hl7.DataHandlers;

public interface IHl7DataHandler
{
    // Process ADT^A01 (Patient Admission) — creates a new Patient from HL7 demographics.
    Task<Hl7AdtResultDto> ProcessAdtA01Async(string hl7Message, string processedBy, CancellationToken ct = default);

    // Process ORU^R01 (Observation Result) — appends a read-only CaseNote to the patient's active case.
    Task<Hl7OruResultDto> ProcessOruR01Async(string hl7Message, string processedBy, CancellationToken ct = default);
}
