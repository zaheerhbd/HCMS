# Phase 2 — Patient & Case Core

**Goal:** Complete patient registration and the full case lifecycle. The primary user flow (create patient → open case → change status → add care team member) works end-to-end.

**Duration:** 3 weeks  
**Status:** `[~]` In Progress  
**Last Updated:** 2026-06-29

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
- [ ] Seed: default CaseTypes (Chronic Disease, Post-Surgery, Mental Health, Preventive, Behavioral), CaseTags
- [ ] Implement `UsersController` (Admin only): list, create, update, role assignment, activate/deactivate
- [ ] Add `AuditInterceptor` (EF Core `SaveChanges` override) for Patient, Case, CaseNote, User entities
- [ ] Background job (Hosted Service): lock `CaseNotes.IsEditable = 0` after 24 hours

## Frontend Tasks

- [ ] Implement `PatientsModule` (lazy-loaded):
  - [ ] `PatientListComponent`: server-side paginated table, search bar, status filter
  - [ ] `PatientDetailComponent`: demographics, insurance accordion, cases tab, documents tab (placeholder)
  - [ ] `PatientFormComponent`: reactive form for create/edit
  - [ ] `PatientService`: all HTTP calls
- [ ] Implement `CasesModule` (lazy-loaded):
  - [ ] `CaseListComponent`: paginated table, filter by status/priority/coordinator/tag
  - [ ] `CaseDetailComponent`: header, status badge + transition button, tabs (Notes, Tasks, Documents, Team, History)
  - [ ] `CaseFormComponent`: create/edit form, patient picker (autocomplete), case type/priority/tags
  - [ ] `CaseStatusHistoryComponent`: timeline view of status changes
  - [ ] `CareTeamComponent`: member list with add/remove
  - [ ] `CaseNotesComponent`: note list + add note form
  - [ ] `CaseService`: all HTTP calls
- [ ] Add `StatusBadgeComponent` to `SharedModule` (color-coded by status value)
- [ ] Add `HasRoleDirective` to `SharedModule` (structural directive hiding elements by role)
- [ ] Add `ConfirmDialogComponent` to `SharedModule` (reusable confirm modal)
- [ ] Wire breadcrumb navigation: Patients → Patient Detail → Case Detail

## Definition of Done

- [ ] Admin can create users and assign roles
- [ ] Coordinator can register a patient with insurance info
- [ ] Coordinator can open a case, assign it to themselves, transition through all statuses
- [ ] Care team members can be added and removed; action appears in audit log
- [ ] Notes can be added; notes older than 24h show as read-only
- [ ] Patient list search returns correct results with pagination
