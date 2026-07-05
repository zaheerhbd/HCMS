using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using HCM.Application.Fhir.DTOs;
using HCM.Application.Interfaces;
using DomainPatient = HCM.Domain.Entities.Patient;
using DomainCase = HCM.Domain.Entities.Case;
using DomainAppointment = HCM.Domain.Entities.Appointment;
using DomainTask = HCM.Domain.Entities.CaseTask;
using DomainDocument = HCM.Domain.Entities.Document;
using FhirTask = Hl7.Fhir.Model.Task;
using FhirAppointment = Hl7.Fhir.Model.Appointment;

namespace HCM.Infrastructure.Services;

// Implements IFhirMappingService using the Firely .NET SDK (Hl7.Fhir.R4 v6).
// Returns serialized FHIR R4 JSON ready to send as application/fhir+json.
public class FhirMappingService : IFhirMappingService
{
    private static readonly FhirJsonSerializer Serializer = new();
    private static readonly FhirJsonDeserializer Deserializer = FhirJsonDeserializer.RECOVERABLE;

    // ── CapabilityStatement ─────────────────────────────────────────────────────

    public string GetCapabilityStatement(string baseUrl)
    {
        var cs = new CapabilityStatement
        {
            Id = "hcms-capability",
            Url = $"{baseUrl}/fhir/R4/metadata",
            Version = "1.0.0",
            Name = "HCMSCapabilityStatement",
            Title = "HCMS FHIR R4 Capability Statement",
            Status = PublicationStatus.Active,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Publisher = "HCMS Portfolio Project",
            Description = new Markdown("Healthcare Case Management System — FHIR R4 read/search/import facade."),
            Kind = CapabilityStatementKind.Instance,
            FhirVersion = FHIRVersion.N4_0_1,
            Format = ["json"],
            Rest =
            [
                new CapabilityStatement.RestComponent
                {
                    Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                    Resource =
                    [
                        BuildResourceComponent("Patient",
                            [CapabilityStatement.TypeRestfulInteraction.Read, CapabilityStatement.TypeRestfulInteraction.SearchType, CapabilityStatement.TypeRestfulInteraction.Create],
                            [("_id", SearchParamType.Token), ("name", SearchParamType.String), ("birthdate", SearchParamType.Date), ("identifier", SearchParamType.Token)]),
                        BuildResourceComponent("Encounter",
                            [CapabilityStatement.TypeRestfulInteraction.Read, CapabilityStatement.TypeRestfulInteraction.SearchType],
                            [("patient", SearchParamType.Reference), ("status", SearchParamType.Token)]),
                        BuildResourceComponent("Appointment",
                            [CapabilityStatement.TypeRestfulInteraction.Read, CapabilityStatement.TypeRestfulInteraction.SearchType],
                            [("_id", SearchParamType.Token), ("patient", SearchParamType.Reference), ("status", SearchParamType.Token), ("date", SearchParamType.Date)]),
                        BuildResourceComponent("Task",
                            [CapabilityStatement.TypeRestfulInteraction.Read, CapabilityStatement.TypeRestfulInteraction.SearchType],
                            [("_id", SearchParamType.Token), ("patient", SearchParamType.Reference), ("focus", SearchParamType.Reference), ("status", SearchParamType.Token)]),
                        BuildResourceComponent("DocumentReference",
                            [CapabilityStatement.TypeRestfulInteraction.Read], []),
                    ]
                }
            ]
        };

        return Serializer.SerializeToString(cs);
    }

    private static CapabilityStatement.ResourceComponent BuildResourceComponent(
        string type,
        IEnumerable<CapabilityStatement.TypeRestfulInteraction> interactions,
        IEnumerable<(string name, SearchParamType paramType)> searchParams)
    {
        return new CapabilityStatement.ResourceComponent
        {
            TypeElement = new Code(type),
            Interaction = interactions.Select(i => new CapabilityStatement.ResourceInteractionComponent { Code = i }).ToList(),
            // Explicitly set TypeElement (not the convenience Type property) so Firely v6 serializes it correctly.
            SearchParam = searchParams.Select(p => new CapabilityStatement.SearchParamComponent
            {
                Name = p.name,
                TypeElement = new Code<SearchParamType>(p.paramType)
            }).ToList()
        };
    }

    // ── Patient ─────────────────────────────────────────────────────────────────

    public string MapPatientToJson(DomainPatient patient)
        => Serializer.SerializeToString(BuildFhirPatient(patient));

    public string MapPatientsBundle(IEnumerable<DomainPatient> patients, int totalCount, string selfUrl)
    {
        var list = patients.ToList();
        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = Bundle.BundleType.Searchset,
            Total = totalCount,
            Timestamp = DateTimeOffset.UtcNow,
            Link = [new Bundle.LinkComponent { Relation = "self", Url = selfUrl }],
            Entry = list.Select(p => new Bundle.EntryComponent
            {
                FullUrl = $"{selfUrl}/{p.MRN}",
                Resource = BuildFhirPatient(p),
                Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
            }).ToList()
        };

        return Serializer.SerializeToString(bundle);
    }

    private static Patient BuildFhirPatient(DomainPatient p)
    {
        var fhirPatient = new Patient
        {
            Id = p.MRN,
            Meta = new Meta { LastUpdated = p.UpdatedAt },
            Identifier =
            [
                new Identifier
                {
                    System = "urn:hcms:mrn",
                    Value = p.MRN,
                    Use = Identifier.IdentifierUse.Official
                }
            ],
            Name =
            [
                new HumanName
                {
                    Use = HumanName.NameUse.Official,
                    Family = p.LastName,
                    Given = [p.FirstName]
                }
            ],
            BirthDate = p.DateOfBirth.ToString("yyyy-MM-dd"),
            Gender = MapGender(p.Gender),
            Active = p.IsActive
        };

        var telecom = new List<ContactPoint>();
        if (!string.IsNullOrWhiteSpace(p.Phone))
            telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = p.Phone, Use = ContactPoint.ContactPointUse.Home });
        if (!string.IsNullOrWhiteSpace(p.Email))
            telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Email, Value = p.Email });
        if (telecom.Count > 0) fhirPatient.Telecom = telecom;

        if (!string.IsNullOrWhiteSpace(p.Address) || !string.IsNullOrWhiteSpace(p.City))
        {
            fhirPatient.Address =
            [
                new Address
                {
                    Use = Address.AddressUse.Home,
                    Line = string.IsNullOrWhiteSpace(p.Address) ? [] : [p.Address],
                    City = p.City,
                    State = p.State,
                    PostalCode = p.ZipCode,
                    Country = "US"
                }
            ];
        }

        return fhirPatient;
    }

    private static AdministrativeGender? MapGender(string? gender) => gender switch
    {
        "Male" => AdministrativeGender.Male,
        "Female" => AdministrativeGender.Female,
        "NonBinary" => AdministrativeGender.Other,
        _ => AdministrativeGender.Unknown
    };

    // ── $everything Bundle ───────────────────────────────────────────────────────

    public string MapEverythingBundle(
        DomainPatient patient,
        IEnumerable<DomainCase> cases,
        IEnumerable<DomainAppointment> appointments,
        IEnumerable<DomainTask> tasks,
        IEnumerable<DomainDocument> documents,
        string selfUrl)
    {
        var entries = new List<Bundle.EntryComponent>
        {
            new() { FullUrl = $"Patient/{patient.MRN}", Resource = BuildFhirPatient(patient) }
        };

        foreach (var c in cases)
            entries.Add(new Bundle.EntryComponent { FullUrl = $"Encounter/{c.CaseNumber}", Resource = BuildFhirEncounter(c) });

        foreach (var a in appointments)
            entries.Add(new Bundle.EntryComponent { FullUrl = $"Appointment/{a.Id}", Resource = BuildFhirAppointment(a) });

        foreach (var t in tasks)
            entries.Add(new Bundle.EntryComponent { FullUrl = $"Task/{t.Id}", Resource = BuildFhirTask(t) });

        foreach (var d in documents)
            entries.Add(new Bundle.EntryComponent { FullUrl = $"DocumentReference/{d.Id}", Resource = BuildFhirDocumentReference(d) });

        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = Bundle.BundleType.Collection,
            Timestamp = DateTimeOffset.UtcNow,
            Total = entries.Count,
            Link = [new Bundle.LinkComponent { Relation = "self", Url = selfUrl }],
            Entry = entries
        };

        return Serializer.SerializeToString(bundle);
    }

    // ── Encounter (Case) ─────────────────────────────────────────────────────────

    public string MapEncounterToJson(DomainCase c)
        => Serializer.SerializeToString(BuildFhirEncounter(c));

    public string MapEncountersBundle(IEnumerable<DomainCase> cases, int totalCount, string selfUrl)
    {
        var list = cases.ToList();
        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = Bundle.BundleType.Searchset,
            Total = totalCount,
            Timestamp = DateTimeOffset.UtcNow,
            Link = [new Bundle.LinkComponent { Relation = "self", Url = selfUrl }],
            Entry = list.Select(c => new Bundle.EntryComponent
            {
                FullUrl = $"{selfUrl}/{c.CaseNumber}",
                Resource = BuildFhirEncounter(c),
                Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
            }).ToList()
        };

        return Serializer.SerializeToString(bundle);
    }

    private static Encounter BuildFhirEncounter(DomainCase c)
    {
        var encounter = new Encounter
        {
            Id = c.CaseNumber,
            Meta = new Meta { LastUpdated = c.UpdatedAt },
            Identifier =
            [
                new Identifier { System = "urn:hcms:case", Value = c.CaseNumber, Use = Identifier.IdentifierUse.Official }
            ],
            Status = MapCaseStatus(c.CurrentStatus),
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
            Subject = c.Patient != null
                ? new ResourceReference($"Patient/{c.Patient.MRN}", c.Patient.FullName)
                : new ResourceReference($"Patient/{c.PatientId}"),
            ReasonCode = c.CaseType != null
                ? [new CodeableConcept { Text = c.CaseType.Name }]
                : []
        };

        if (c.OpenedAt.HasValue)
            encounter.Period = new Period { StartElement = new FhirDateTime(c.OpenedAt.Value) };

        if (c.ClosedAt.HasValue && encounter.Period != null)
            encounter.Period.EndElement = new FhirDateTime(c.ClosedAt.Value);

        return encounter;
    }

    private static Encounter.EncounterStatus MapCaseStatus(string status) => status switch
    {
        "Open" => Encounter.EncounterStatus.Planned,
        "InProgress" => Encounter.EncounterStatus.InProgress,
        "OnHold" => Encounter.EncounterStatus.Onleave,
        "Closed" => Encounter.EncounterStatus.Finished,
        "Reopened" => Encounter.EncounterStatus.InProgress,
        _ => Encounter.EncounterStatus.Unknown
    };

    // ── Appointment ──────────────────────────────────────────────────────────────

    public string MapAppointmentToJson(DomainAppointment a)
        => Serializer.SerializeToString(BuildFhirAppointment(a));

    public string MapAppointmentsBundle(IEnumerable<DomainAppointment> appointments, int totalCount, string selfUrl)
    {
        var list = appointments.ToList();
        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = Bundle.BundleType.Searchset,
            Total = totalCount,
            Timestamp = DateTimeOffset.UtcNow,
            Link = [new Bundle.LinkComponent { Relation = "self", Url = selfUrl }],
            Entry = list.Select(a => new Bundle.EntryComponent
            {
                FullUrl = $"{selfUrl}/{a.Id}",
                Resource = BuildFhirAppointment(a),
                Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
            }).ToList()
        };

        return Serializer.SerializeToString(bundle);
    }

    private static FhirAppointment BuildFhirAppointment(DomainAppointment a)
    {
        var fhirAppt = new FhirAppointment
        {
            Id = a.Id.ToString(),
            Meta = new Meta { LastUpdated = a.UpdatedAt },
            Status = MapAppointmentStatus(a.Status),
            ServiceType = [new CodeableConcept { Text = a.AppointmentType }],
            Start = new DateTimeOffset(a.ScheduledStart, TimeSpan.Zero),
            End = new DateTimeOffset(a.ScheduledEnd, TimeSpan.Zero),
            Participant = []
        };

        if (a.Patient != null)
        {
            fhirAppt.Participant.Add(new FhirAppointment.ParticipantComponent
            {
                Actor = new ResourceReference($"Patient/{a.Patient.MRN}", a.Patient.FullName),
                Required = FhirAppointment.ParticipantRequired.Required,
                Status = ParticipationStatus.Accepted
            });
        }

        if (a.Provider != null)
        {
            fhirAppt.Participant.Add(new FhirAppointment.ParticipantComponent
            {
                Actor = new ResourceReference($"Practitioner/{a.Provider.Id}", a.Provider.Name),
                Required = FhirAppointment.ParticipantRequired.Required,
                Status = ParticipationStatus.Accepted
            });
        }

        if (!string.IsNullOrWhiteSpace(a.Location))
        {
            fhirAppt.Participant.Add(new FhirAppointment.ParticipantComponent
            {
                Actor = new ResourceReference(null, a.Location),
                Required = FhirAppointment.ParticipantRequired.Required,
                Status = ParticipationStatus.Accepted
            });
        }

        return fhirAppt;
    }

    private static FhirAppointment.AppointmentStatus MapAppointmentStatus(string status) => status switch
    {
        "Scheduled" => FhirAppointment.AppointmentStatus.Booked,
        "Confirmed" => FhirAppointment.AppointmentStatus.Booked,
        "Completed" => FhirAppointment.AppointmentStatus.Fulfilled,
        "NoShow" => FhirAppointment.AppointmentStatus.Noshow,
        "Cancelled" => FhirAppointment.AppointmentStatus.Cancelled,
        _ => FhirAppointment.AppointmentStatus.Booked
    };

    // ── Task ─────────────────────────────────────────────────────────────────────

    public string MapTaskToJson(DomainTask t)
        => Serializer.SerializeToString(BuildFhirTask(t));

    public string MapTasksBundle(IEnumerable<DomainTask> tasks, int totalCount, string selfUrl)
    {
        var list = tasks.ToList();
        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = Bundle.BundleType.Searchset,
            Total = totalCount,
            Timestamp = DateTimeOffset.UtcNow,
            Link = [new Bundle.LinkComponent { Relation = "self", Url = selfUrl }],
            Entry = list.Select(t => new Bundle.EntryComponent
            {
                FullUrl = $"{selfUrl}/{t.Id}",
                Resource = BuildFhirTask(t),
                Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
            }).ToList()
        };

        return Serializer.SerializeToString(bundle);
    }

    private static FhirTask BuildFhirTask(DomainTask t)
    {
        var fhirTask = new FhirTask
        {
            Id = t.Id.ToString(),
            Meta = new Meta { LastUpdated = t.UpdatedAt },
            Status = MapTaskStatus(t.Status),
            Intent = FhirTask.TaskIntent.Order,
            Priority = MapTaskPriority(t.Priority),
            Description = t.Title,
            For = t.Case != null ? new ResourceReference($"Patient/{t.Case.PatientId}") : null
        };

        if (t.AssignedTo.HasValue)
            fhirTask.Owner = new ResourceReference($"Practitioner/{t.AssignedTo}");

        if (t.DueDate.HasValue)
            fhirTask.ExecutionPeriod = new Period { EndElement = new FhirDateTime(t.DueDate.Value) };

        if (t.CompletedAt.HasValue)
            fhirTask.LastModifiedElement = new FhirDateTime(t.CompletedAt.Value);

        return fhirTask;
    }

    private static FhirTask.TaskStatus MapTaskStatus(string status) => status switch
    {
        "Todo" => FhirTask.TaskStatus.Requested,
        "InProgress" => FhirTask.TaskStatus.InProgress,
        "Blocked" => FhirTask.TaskStatus.OnHold,
        "Done" => FhirTask.TaskStatus.Completed,
        "Cancelled" => FhirTask.TaskStatus.Cancelled,
        _ => FhirTask.TaskStatus.Draft
    };

    private static RequestPriority MapTaskPriority(string priority) => priority switch
    {
        "High" => RequestPriority.Urgent,
        "Low" => RequestPriority.Routine,
        _ => RequestPriority.Routine
    };

    // ── DocumentReference ────────────────────────────────────────────────────────

    public string MapDocumentReferenceToJson(DomainDocument d)
        => Serializer.SerializeToString(BuildFhirDocumentReference(d));

    private static DocumentReference BuildFhirDocumentReference(DomainDocument d)
    {
        return new DocumentReference
        {
            Id = d.Id.ToString(),
            Meta = new Meta { LastUpdated = d.UploadedAt },
            Status = DocumentReferenceStatus.Current,
            Subject = d.Patient != null
                ? new ResourceReference($"Patient/{d.Patient.MRN}")
                : new ResourceReference($"Patient/{d.PatientId}"),
            Category = [new CodeableConcept { Text = d.Category }],
            Content =
            [
                new DocumentReference.ContentComponent
                {
                    Attachment = new Attachment
                    {
                        ContentType = d.ContentType,
                        Title = d.FileName,
                        Creation = new FhirDateTime(d.UploadedAt).ToString()
                    }
                }
            ]
        };
    }

    // ── Patient Import (FHIR → Domain) ───────────────────────────────────────────

    public (DomainPatient? patient, string? error) ParsePatientFromFhir(string fhirJson)
    {
        try
        {
            var fhirPatient = Deserializer.Deserialize<Patient>(fhirJson);

            var officialName = fhirPatient.Name.FirstOrDefault(n => n.Use == HumanName.NameUse.Official)
                ?? fhirPatient.Name.FirstOrDefault();

            if (officialName == null)
                return (null, "FHIR Patient must include at least one name.");

            var firstName = officialName.Given.FirstOrDefault()?.Trim();
            var lastName = officialName.Family?.Trim();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                return (null, "FHIR Patient name must include both given (first) and family (last) name.");

            if (string.IsNullOrWhiteSpace(fhirPatient.BirthDate) ||
                !DateTime.TryParse(fhirPatient.BirthDate, out var dob))
                return (null, "FHIR Patient must include a valid birthDate (YYYY-MM-DD).");

            var domain = new DomainPatient
            {
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dob,
                Gender = MapFhirGender(fhirPatient.Gender),
                MRN = string.Empty // Generated by PatientDataHandler
            };

            var phone = fhirPatient.Telecom?.FirstOrDefault(t => t.System == ContactPoint.ContactPointSystem.Phone)?.Value;
            var email = fhirPatient.Telecom?.FirstOrDefault(t => t.System == ContactPoint.ContactPointSystem.Email)?.Value;
            domain.Phone = phone;
            domain.Email = email;

            var addr = fhirPatient.Address?.FirstOrDefault();
            if (addr != null)
            {
                domain.Address = addr.Line?.FirstOrDefault();
                domain.City = addr.City;
                domain.State = addr.State;
                domain.ZipCode = addr.PostalCode;
            }

            return (domain, null);
        }
        catch (Exception ex)
        {
            return (null, $"FHIR parse error: {ex.Message}");
        }
    }

    private static string? MapFhirGender(AdministrativeGender? gender) => gender switch
    {
        AdministrativeGender.Male => "Male",
        AdministrativeGender.Female => "Female",
        AdministrativeGender.Other => "NonBinary",
        _ => "Prefer Not to Say"
    };

    // ── Transaction Bundle import ────��───────────────────────────────────────────

    public (bool isValid, string? error, List<ParsedBundleEntry> entries) ParseTransactionBundle(string bundleJson)
    {
        Bundle bundle;
        try
        {
            bundle = Deserializer.Deserialize<Bundle>(bundleJson);
        }
        catch (Exception ex)
        {
            return (false, $"Bundle parse error: {ex.Message}", []);
        }

        if (bundle.Type != Bundle.BundleType.Transaction)
            return (false, "Bundle.type must be 'transaction'.", []);

        var entries = new List<ParsedBundleEntry>();

        foreach (var entry in bundle.Entry ?? [])
        {
            if (entry.Resource == null) continue;
            var fullUrl = entry.FullUrl;

            switch (entry.Resource)
            {
                case Patient fhirPatient:
                    var (parsed, patientError) = ExtractPatientFields(fhirPatient);
                    if (parsed == null)
                        return (false, $"Patient entry: {patientError}", []);
                    entries.Add(new ParsedBundleEntry { ResourceType = "Patient", FullUrl = fullUrl, Patient = parsed });
                    break;

                case Encounter fhirEncounter:
                    var assignedToExt = fhirEncounter.GetExtensionValue<FhirString>(
                        "http://hcms.local/fhir/StructureDefinition/case-assigned-to");
                    entries.Add(new ParsedBundleEntry
                    {
                        ResourceType = "Encounter",
                        FullUrl = fullUrl,
                        Encounter = new ParsedFhirEncounter(
                            fhirEncounter.Subject?.Reference,
                            fhirEncounter.Type?.FirstOrDefault()?.Text,
                            assignedToExt?.Value)
                    });
                    break;

                case FhirAppointment fhirAppt:
                    // Identify patient participant: first actor that is not a Practitioner reference.
                    var patientParticipant = fhirAppt.Participant
                        .FirstOrDefault(p => p.Actor?.Reference != null &&
                                             !p.Actor.Reference.StartsWith("Practitioner/", StringComparison.OrdinalIgnoreCase));
                    var practitionerParticipant = fhirAppt.Participant
                        .FirstOrDefault(p => p.Actor?.Reference?.StartsWith("Practitioner/", StringComparison.OrdinalIgnoreCase) == true);
                    var dueDateStr = fhirAppt.End?.ToString();
                    entries.Add(new ParsedBundleEntry
                    {
                        ResourceType = "Appointment",
                        FullUrl = fullUrl,
                        Appointment = new ParsedFhirAppointment(
                            patientParticipant?.Actor?.Reference,
                            fhirAppt.Start?.UtcDateTime,
                            fhirAppt.End?.UtcDateTime,
                            fhirAppt.ServiceType?.FirstOrDefault()?.Text,
                            practitionerParticipant?.Actor?.Reference)
                    });
                    break;

                case FhirTask fhirTask:
                    var restrictionEnd = fhirTask.Restriction?.Period?.End;
                    var executionEnd = fhirTask.ExecutionPeriod?.End;
                    var taskDueDateStr = restrictionEnd ?? executionEnd;
                    DateTime? taskDueDate = taskDueDateStr != null && DateTime.TryParse(taskDueDateStr, out var td) ? td : null;
                    entries.Add(new ParsedBundleEntry
                    {
                        ResourceType = "Task",
                        FullUrl = fullUrl,
                        Task = new ParsedFhirTask(
                            fhirTask.Description,
                            fhirTask.For?.Reference,
                            fhirTask.Focus?.Reference,
                            taskDueDate,
                            fhirTask.Priority?.ToString()?.ToLower())
                    });
                    break;
            }
        }

        return (true, null, entries);
    }

    public string MapTransactionResponse(List<BundleEntryResultDto> results)
    {
        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = Bundle.BundleType.TransactionResponse,
            Timestamp = DateTimeOffset.UtcNow,
            Entry = results.Select(r => new Bundle.EntryComponent
            {
                Response = new Bundle.ResponseComponent
                {
                    Status = r.Status,
                    Location = $"{r.ResourceType}/{r.AssignedId}"
                }
            }).ToList()
        };
        return Serializer.SerializeToString(bundle);
    }

    // Extracts plain patient data from a Firely Patient object — reused by ParseTransactionBundle.
    private static (ParsedFhirPatient? patient, string? error) ExtractPatientFields(Patient fhirPatient)
    {
        var officialName = fhirPatient.Name.FirstOrDefault(n => n.Use == HumanName.NameUse.Official)
            ?? fhirPatient.Name.FirstOrDefault();
        if (officialName == null) return (null, "Patient must include at least one name.");

        var firstName = officialName.Given.FirstOrDefault()?.Trim();
        var lastName = officialName.Family?.Trim();
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return (null, "Patient name must include both given and family name.");

        if (string.IsNullOrWhiteSpace(fhirPatient.BirthDate) ||
            !DateTime.TryParse(fhirPatient.BirthDate, out var dob))
            return (null, "Patient must include a valid birthDate.");

        var phone = fhirPatient.Telecom?.FirstOrDefault(t => t.System == ContactPoint.ContactPointSystem.Phone)?.Value;
        var email = fhirPatient.Telecom?.FirstOrDefault(t => t.System == ContactPoint.ContactPointSystem.Email)?.Value;
        var addr = fhirPatient.Address?.FirstOrDefault();

        return (new ParsedFhirPatient(
            firstName, lastName, dob,
            MapFhirGender(fhirPatient.Gender),
            phone, email,
            addr?.Line?.FirstOrDefault(), addr?.City, addr?.State, addr?.PostalCode), null);
    }
}
