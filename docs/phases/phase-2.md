# Phase 2 — Patient & Case Core

**Goal:** Complete patient registration and the full case lifecycle. The primary user flow (create patient → open case → change status → add care team member) works end-to-end.

**Duration:** 3 weeks  
**Status:** `[~]` In Progress  
**Last Updated:** 2026-06-29 (session 2 — backend + frontend complete)

---

## Backend Tasks

- [x] Add EF Core entities: `Patients`, `PatientInsurance`, `CaseTypes`, `Cases`, `CaseTags`, `CaseCaseTags`, `CaseStatusHistory`, `CareTeamMembers`, `CaseNotes`
- [x] Create and apply EF Core migration
- [x] Implement `PatientDataHandler`: CRUD, MRN generation (`MRN-{YYYY}-{NNNNN}`), paginated search
- [x] Implement `CaseDataHandler`: CRUD, case number generation (`CASE-{YYYY}-{NNNNN}`), status state machine
- [x] Implement `CareTeamDataHandler`: add/remove members, list team
- [x] Implement `CaseNoteDataHandler`: add notes, list notes, soft delete
- [x] Register all DataHandlers in `DependencyInjection.cs`
- [x] Implement `PatientsController`: full CRUD + paginated search
- [x] Implement `CasesController`: CRUD + status transitions with history
- [x] Implement `CareTeamController`: add/remove members
- [x] Implement `CaseNotesController`: add/list notes
- [x] Seed: default CaseTypes (Chronic Disease, Post-Surgery, Mental Health, Preventive, Behavioral), CaseTags
- [x] Implement `UsersController` (Admin only): list, create, update, role assignment, activate/deactivate
- [x] Add `AuditInterceptor` (EF Core `SaveChanges` override) for Patient, Case, CaseNote, User entities
- [x] Background job (Hosted Service): lock `CaseNotes.IsEditable = 0` after 24 hours

## Frontend Tasks

- [x] Implement `PatientsModule` (lazy-loaded):
  - [x] `PatientListComponent`: server-side paginated table, debounced search bar
  - [x] `PatientDetailComponent`: demographics, insurance accordion, cases tab, documents tab (placeholder)
  - [x] `PatientFormComponent`: reactive form for create/edit with datepicker
  - [x] `PatientService`: all HTTP calls
- [x] Implement `CasesModule` (lazy-loaded):
  - [x] `CaseListComponent`: paginated table, filter by status
  - [x] `CaseDetailComponent`: header card, inline status transition, tabs (Notes, Team, History, Tasks placeholder, Documents placeholder)
  - [x] `CaseFormComponent`: create form with patient picker and case type select
  - [x] `CaseStatusHistoryComponent`: timeline view of status changes
  - [x] `CareTeamComponent`: member list with add/remove (ConfirmDialog)
  - [x] `CaseNotesComponent`: note list + add note form with locked badge after 24h
  - [x] `CaseService`: all HTTP calls
- [x] Add `StatusBadgeComponent` to shared (color-coded chip by status value)
- [x] Add `HasRoleDirective` to shared (structural directive `*appHasRole`)
- [x] Add `ConfirmDialogComponent` to shared (reusable Material Dialog)
- [x] Wire breadcrumb navigation: Patients → Patient Detail → Case Detail

## Definition of Done

- [ ] Admin can create users and assign roles
- [ ] Coordinator can register a patient with insurance info
- [ ] Coordinator can open a case, assign it to themselves, transition through all statuses
- [ ] Care team members can be added and removed; action appears in audit log
- [ ] Notes can be added; notes older than 24h show as read-only
- [ ] Patient list search returns correct results with pagination
