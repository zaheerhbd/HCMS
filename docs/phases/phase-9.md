# Phase 9 — Advanced FHIR & Interoperability

**Goal:** Extend the FHIR R4 facade with atomic Transaction Bundle import (multi-resource in one call) and bundle file upload. An external EHR system can send a complete patient intake — Patient + Case + Appointments + Tasks — as a single request and have it land correctly or not at all.

**Duration:** 1 week  
**Status:** `[x]` Complete (2026-07-05)  
**Last Updated:** 2026-11-27  
**FRs:** FR-55 – FR-58, FR-55a (Transaction Bundle + file upload). FR-53/54 (HL7 v2) withdrawn.

---

## Background

Real EHR systems rarely push one resource at a time. A patient referral from an external hospital typically arrives as a bundle: demographic data, the reason for referral (case), any pre-booked appointments, and initial care tasks — all in one payload. FHIR Transaction Bundles are the standard mechanism for this; they guarantee atomicity (all-or-nothing).

Phase 5 covered single-resource import (`POST /fhir/R4/Patient`). Phase 9 adds the full intake workflow.

---

## Backend Tasks

### Transaction Bundle (`POST /fhir/R4/`)

- [x] Add `POST /fhir/R4/` root endpoint in `FhirController`
  - Accept `application/fhir+json` body; read entire stream
  - Reject non-Bundle or non-transaction types with `OperationOutcome`
- [x] Add interface method to `IFhirDataHandler`:
  ```csharp
  Task<FhirBundleImportResultDto> ImportTransactionBundleAsync(
      string bundleJson, string importedBy, CancellationToken ct = default);
  ```
- [x] Implement `FhirDataHandler.ImportTransactionBundleAsync`:
  - Parse with Firely SDK (`FhirJsonParser`); validate `Bundle.Type == BundleType.Transaction`
  - Open `IDbContextTransaction` (rollback scope)
  - Process entries in declaration order:
    1. `Patient` entry → call `_patientHandler.CreatePatientAsync`; capture generated MRN
    2. `Encounter` entry → call `CaseDataHandler.CreateCaseAsync`; resolve `Patient/` reference using MRN from step 1
    3. `Appointment` entries → call `AppointmentDataHandler.CreateAppointmentAsync`; resolve `Patient/` reference
    4. `Task` entries → call task creation; resolve `Patient/` and `Encounter/` references
  - On any exception: rollback + return `OperationOutcome` with per-entry diagnostics
  - On success: commit; write `FhirImportLogs` per resource type; write `AuditLogs` entry (`FhirBundleImport`)
- [x] Add `FhirBundleImportResultDto` in `HCM.Application/Fhir/DTOs/`:
  ```csharp
  public record FhirBundleImportResultDto(
      bool Success,
      List<BundleEntryResultDto> Entries,
      List<string> Errors);
  public record BundleEntryResultDto(
      string ResourceType, string AssignedId, string Status);
  ```
- [x] Add `MapTransactionResponse(List<BundleEntryResultDto> results)` to `IFhirMappingService` / `FhirMappingService`
  - Returns FHIR Bundle of type `transaction-response`
  - One entry per input entry with `response.status = "201 Created"` and `response.location = ResourceType/AssignedId`

### HL7 v2 Ingestion (Stretch) — removed

- [-] HL7 v2 ADT^A01 / ORU^R01 endpoints (`Hl7Controller`, `Hl7DataHandler`, `Hl7MappingService`, NHapi) were built and then **removed** on 2026-07-05.

### Bundle File Upload (replacement)

- [x] `POST /fhir/R4/$import` — multipart/form-data `bundleFile`, delegates to `ImportTransactionBundleAsync`
- [x] Encounter extension `case-assigned-to` (username) → assigned coordinator, falls back to importer

---

## Frontend Tasks

- [x] Extend `FhirImportComponent` — add **"Import Bundle"** tab:
  - Textarea / file upload for Transaction Bundle JSON
  - On submit: `POST /fhir/R4/` with `Content-Type: application/fhir+json`
  - Display per-entry result table: resource type, assigned ID, status
  - On success: link to the newly created patient detail page
- [x] Add **sample Transaction Bundle** JSON in the help tooltip / example panel (collapsible)
- [x] Add `importTransactionBundle(bundleJson: string)` to `FhirService`

---

## Sample FHIR Transaction Bundle

```json
{
  "resourceType": "Bundle",
  "type": "transaction",
  "entry": [
    {
      "fullUrl": "urn:uuid:patient-1",
      "resource": {
        "resourceType": "Patient",
        "name": [{ "family": "Rivera", "given": ["Maria"] }],
        "gender": "female",
        "birthDate": "1978-04-12",
        "telecom": [
          { "system": "phone", "value": "555-0191" },
          { "system": "email", "value": "maria.rivera@example.com" }
        ]
      },
      "request": { "method": "POST", "url": "Patient" }
    },
    {
      "fullUrl": "urn:uuid:encounter-1",
      "resource": {
        "resourceType": "Encounter",
        "status": "planned",
        "class": { "code": "IMP", "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode" },
        "subject": { "reference": "urn:uuid:patient-1" },
        "type": [{ "text": "Chronic Disease Management" }],
        "priority": { "text": "routine" }
      },
      "request": { "method": "POST", "url": "Encounter" }
    },
    {
      "fullUrl": "urn:uuid:appointment-1",
      "resource": {
        "resourceType": "Appointment",
        "status": "booked",
        "serviceType": [{ "text": "Care Coordination" }],
        "start": "2026-07-15T09:00:00Z",
        "end": "2026-07-15T09:30:00Z",
        "participant": [
          { "actor": { "reference": "urn:uuid:patient-1" }, "status": "accepted" }
        ]
      },
      "request": { "method": "POST", "url": "Appointment" }
    },
    {
      "fullUrl": "urn:uuid:task-1",
      "resource": {
        "resourceType": "Task",
        "status": "requested",
        "intent": "order",
        "description": "Initial care assessment",
        "for": { "reference": "urn:uuid:patient-1" },
        "focus": { "reference": "urn:uuid:encounter-1" },
        "restriction": {
          "period": { "end": "2026-07-20T00:00:00Z" }
        }
      },
      "request": { "method": "POST", "url": "Task" }
    }
  ]
}
```

---

## Definition of Done

> Build complete and builds clean; these acceptance checks have not been formally recorded as verified.

- [ ] `POST /fhir/R4/` with the sample bundle above creates Patient + Case + Appointment + Task atomically
- [ ] If any entry fails (e.g., duplicate patient name + DOB), no resources are created and the response is an `OperationOutcome` with per-entry diagnostics
- [ ] Response is a valid FHIR Bundle of type `transaction-response` with `response.location` for each entry
- [ ] `FhirImportLogs` contains one row per resource type; `AuditLogs` contains one row with `Action = FhirBundleImport`
- [ ] Angular UI "Import Bundle" tab successfully submits the sample bundle and displays the created resource IDs
- [-] *(stretch)* HL7 ADT ingestion — removed
