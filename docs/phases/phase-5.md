# Phase 5 — FHIR/HL7 Integration

**Goal:** A FHIR R4 API facade is live. Recruiters and reviewers can query `/fhir/R4/metadata`, retrieve a patient as a FHIR resource, and download a full FHIR Bundle. Patient import from a FHIR JSON payload works. HL7 v2 ADT receiver is a stretch goal.

**Duration:** 2 weeks  
**Status:** `[ ]` Not Started  
**Last Updated:** 2026-06-24

---

## Backend Tasks

- [ ] Add `Hl7.Fhir.R4` NuGet package (Firely .NET SDK) to `HCM.Infrastructure`
- [ ] Add `FhirImportLogs` EF Core entity; create and apply migration
- [ ] Create `IFhirMappingService` interface in `HCM.Application` with mapping methods per resource type
- [ ] Implement `FhirMappingService` in `HCM.Infrastructure`:
  - [ ] `Patient` → FHIR `Patient` (id, identifier/MRN, name, birthDate, gender, telecom, address)
  - [ ] `Case` → FHIR `Encounter` (id, identifier/CaseNumber, status mapping, priority, subject reference)
  - [ ] `Appointment` → FHIR `Appointment` (id, status, start, end, serviceType, participant references)
  - [ ] `CaseTask` → FHIR `Task` (id, status, priority, owner reference, executionPeriod)
  - [ ] `Document` → FHIR `DocumentReference` (id, category, content.attachment with title and contentType)
- [ ] Implement `FhirController` at `/fhir/R4/`:
  - [ ] `GET /metadata` — CapabilityStatement declaring supported resources and search params
  - [ ] `GET /Patient` — search with params `_id`, `name`, `birthdate`, `identifier`; return FHIR Bundle (type: searchset)
  - [ ] `GET /Patient/{id}` — read single Patient resource; 404 OperationOutcome if not found
  - [ ] `POST /Patient` — accept FHIR Patient JSON; validate with Firely; map to internal Patient; log to FhirImportLogs
  - [ ] `GET /Patient/{id}/$everything` — return FHIR Bundle (type: collection) with Patient + all related Encounters, Appointments, Tasks, DocumentReferences
  - [ ] `GET /Encounter` — search by `patient` and/or `status`
  - [ ] `GET /Encounter/{id}` — read single Encounter
  - [ ] `GET /Appointment/{id}` — read single Appointment
  - [ ] `GET /Task/{id}` — read single Task
  - [ ] `GET /DocumentReference/{id}` — read single DocumentReference
- [ ] Add global FHIR exception handler: map domain exceptions to HL7 `OperationOutcome` with correct HTTP status
- [ ] Ensure `/fhir/R4/` routes are protected by JWT auth (same as `/api/`)
- [ ] Log every FHIR Patient export ($everything or GET) to `AuditLogs` as action `FhirExport`
- [ ] *(stretch)* Add `Hl7Controller` at `/api/hl7/`:
  - [ ] `POST /adt` — receive raw HL7 v2 ADT^A01 text/plain; parse with NHapi; upsert Patient + FhirImportLogs
  - [ ] `POST /oru` — receive HL7 v2 ORU^R01; extract OBX observation; create read-only CaseNote on patient's active case

## Frontend Tasks

- [ ] Implement `FhirModule` (lazy-loaded):
  - [ ] `FhirExplorerComponent`: shows the raw FHIR JSON for any resource type + ID (uses `ngx-json-viewer`); useful for developer/admin view
  - [ ] `FhirImportComponent`: paste or upload a FHIR Patient JSON; submits to `POST /fhir/R4/Patient`; shows import result (success, errors from OperationOutcome)
  - [ ] `FhirService`: typed HTTP calls to `/fhir/R4/` endpoints
- [ ] Add **"Export as FHIR Bundle"** button to `PatientDetailComponent`:
  - Calls `GET /fhir/R4/Patient/{id}/$everything`
  - Downloads response as `patient-{mrn}-fhir-bundle.json`
  - Logs action in UI audit trail notification
- [ ] Add **"Import Patient from FHIR"** menu option on Patient List page (navigates to `FhirImportComponent`)
- [ ] Install `ngx-json-viewer` package for collapsible JSON tree display

## Definition of Done

- [ ] `GET /fhir/R4/metadata` returns a valid FHIR CapabilityStatement (validate with Firely or online FHIR validator)
- [ ] `GET /fhir/R4/Patient?name=Smith` returns a FHIR searchset Bundle with correct entries
- [ ] `GET /fhir/R4/Patient/{id}/$everything` returns a Bundle containing at least Patient + Encounter resources
- [ ] Importing a FHIR Patient JSON via the UI creates a new patient in the system with a generated MRN
- [ ] The download from "Export as FHIR Bundle" is valid JSON parseable as a FHIR R4 Bundle
- [ ] FHIR endpoints return 401 when called without a JWT; errors return OperationOutcome (not ProblemDetails)
