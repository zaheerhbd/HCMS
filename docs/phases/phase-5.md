# Phase 5 — FHIR/HL7 Integration

**Goal:** A FHIR R4 API facade is live. Recruiters and reviewers can query `/fhir/R4/metadata`, retrieve a patient as a FHIR resource, and download a full FHIR Bundle. Patient import from a FHIR JSON payload works. HL7 v2 ADT receiver is a stretch goal.

**Duration:** 2 weeks  
**Status:** `[~]` In Progress — Backend + Frontend complete; E2E testing pending  
**Last Updated:** 2026-07-04

---

## Backend Tasks

- [x] Add `Hl7.Fhir.R4` NuGet package (Firely .NET SDK) to `HCM.Infrastructure`
- [x] Add `FhirImportLogs` EF Core entity; create and apply migration
- [x] Create `IFhirMappingService` interface in `HCM.Application` with mapping methods per resource type
- [x] Implement `FhirMappingService` in `HCM.Infrastructure`:
  - [x] `Patient` → FHIR `Patient` (id, identifier/MRN, name, birthDate, gender, telecom, address)
  - [x] `Case` → FHIR `Encounter` (id, identifier/CaseNumber, status mapping, priority, subject reference)
  - [x] `Appointment` → FHIR `Appointment` (id, status, start, end, serviceType, participant references)
  - [x] `CaseTask` → FHIR `Task` (id, status, priority, owner reference, executionPeriod)
  - [x] `Document` → FHIR `DocumentReference` (id, category, content.attachment with title and contentType)
- [x] Implement `FhirController` at `/fhir/R4/`:
  - [x] `GET /metadata` — CapabilityStatement declaring supported resources and search params
  - [x] `GET /Patient` — search with params `_id`, `name`, `birthdate`, `identifier`; return FHIR Bundle (type: searchset)
  - [x] `GET /Patient/{id}` — read single Patient resource; 404 OperationOutcome if not found
  - [x] `POST /Patient` — accept FHIR Patient JSON; validate with Firely; map to internal Patient; log to FhirImportLogs
  - [x] `GET /Patient/{id}/$everything` — return FHIR Bundle (type: collection) with Patient + all related Encounters, Appointments, Tasks, DocumentReferences
  - [x] `GET /Encounter` — search by `patient` and/or `status`
  - [x] `GET /Encounter/{id}` — read single Encounter
  - [x] `GET /Appointment/{id}` — read single Appointment
  - [x] `GET /Task/{id}` — read single Task
  - [x] `GET /DocumentReference/{id}` — read single DocumentReference
- [x] Add global FHIR exception handler: map domain exceptions to HL7 `OperationOutcome` with correct HTTP status
- [x] Ensure `/fhir/R4/` routes are protected by JWT auth (same as `/api/`)
- [x] Log every FHIR Patient export ($everything or GET) to `AuditLogs` as action `FhirExport`
- [x] Add `GET /fhir/R4/Appointment` — search by `_id` (GUID), `patient` (MRN), `status` (FHIR→domain status map)
- [x] Add `GET /fhir/R4/Task` — search by `_id` (GUID), `patient` (MRN), `focus` (case number), `status`
- [x] Fix `CapabilityStatement` search param serialization — use `TypeElement = new Code<SearchParamType>(...)` instead of `Type` property (Firely v6 API surface)
- [x] Fix MRN generation off-by-one in `PatientDataHandler` — use `mrnPrefix.Length` (9) instead of hardcoded `Substring(8)`
- [ ] *(stretch)* Add `Hl7Controller` at `/api/hl7/`:
  - [ ] `POST /adt` — receive raw HL7 v2 ADT^A01 text/plain; parse with NHapi; upsert Patient + FhirImportLogs
  - [ ] `POST /oru` — receive HL7 v2 ORU^R01; extract OBX observation; create read-only CaseNote on patient's active case

## Phase 5 Extension — FHIR Transaction Bundle Import

**Added:** 2026-07-04 | **FRs:** FR-55 – FR-58

These tasks extend Phase 5 with atomic multi-resource import via a FHIR Transaction Bundle. See [Phase 9](phase-9.md) for the full implementation plan.

### Backend Tasks

- [ ] Add `POST /fhir/R4/` (root FHIR endpoint) in `FhirController` — accepts Bundle with `type = transaction`
- [ ] Add `ImportTransactionBundleAsync(string bundleJson, string importedBy, CancellationToken ct)` to `IFhirDataHandler` / `FhirDataHandler`
- [ ] Implement entry dispatch in `FhirDataHandler.ImportTransactionBundleAsync`:
  - [ ] Parse and validate Bundle with Firely SDK (`Bundle.Type == BundleType.Transaction`, at least 1 entry)
  - [ ] Open a DB transaction (`IDbContextTransaction`)
  - [ ] Process entries in order: Patient → Encounter (Case) → Appointment → Task
  - [ ] Resolve `Patient/{{MRN}}` references in downstream entries using the just-created patient's MRN
  - [ ] Rollback on any failure; return `OperationOutcome` with per-entry diagnostics
  - [ ] Write `FhirImportLogs` per resource type and `AuditLogs` entry (`FhirBundleImport`) on success
- [ ] Add `MapTransactionResponse(List<BundleEntryResult> results)` to `IFhirMappingService` / `FhirMappingService`

### Frontend Tasks

- [ ] Extend `FhirImportComponent` — add "Import Bundle (Patient + Case + Appointments)" tab alongside single-patient import
- [ ] Add sample Transaction Bundle JSON to the import UI help tooltip / example panel

## Frontend Tasks

- [x] Implement `FhirModule` (lazy-loaded):
  - [x] `FhirExplorerComponent`: shows the raw FHIR JSON for any resource type + ID (uses `ngx-json-viewer`); useful for developer/admin view
  - [x] `FhirImportComponent`: paste or upload a FHIR Patient JSON; submits to `POST /fhir/R4/Patient`; shows import result (success, errors from OperationOutcome)
  - [x] `FhirService`: typed HTTP calls to `/fhir/R4/` endpoints
- [x] Add **"Export as FHIR Bundle"** button to `PatientDetailComponent`:
  - Calls `GET /fhir/R4/Patient/{id}/$everything`
  - Downloads response as `patient-{mrn}-fhir-bundle.json`
  - Logs action in UI audit trail notification
- [x] Add **"Import Patient from FHIR"** menu option on Patient List page (navigates to `FhirImportComponent`)
- [x] Install `ngx-json-viewer` package for collapsible JSON tree display

## Definition of Done

- [ ] `GET /fhir/R4/metadata` returns a valid FHIR CapabilityStatement (validate with Firely or online FHIR validator)
- [ ] `GET /fhir/R4/Patient?name=Smith` returns a FHIR searchset Bundle with correct entries
- [ ] `GET /fhir/R4/Patient/{id}/$everything` returns a Bundle containing at least Patient + Encounter resources
- [ ] Importing a FHIR Patient JSON via the UI creates a new patient in the system with a generated MRN
- [ ] The download from "Export as FHIR Bundle" is valid JSON parseable as a FHIR R4 Bundle
- [ ] FHIR endpoints return 401 when called without a JWT; errors return OperationOutcome (not ProblemDetails)
