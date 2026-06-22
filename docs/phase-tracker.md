# Healthcare Case Management System — Phase Tracker

**Project:** Enterprise Healthcare Case Management System (Portfolio)  
**Last Updated:** 2026-06-21  
**Status Key:** `[ ]` Not Started · `[~]` In Progress · `[x]` Complete · `[-]` Deferred

---

## Phase Overview

| Phase | Name | Duration | Status | Deliverable |
|---|---|---|---|---|
| 1 | Foundation & Infrastructure | 2 weeks | `[ ]` | Deployable skeleton with auth |
| 2 | Patient & Case Core | 3 weeks | `[ ]` | Full case lifecycle working |
| 3 | Documents, Tasks & Notes | 2 weeks | `[ ]` | Blob upload + task management |
| 4 | Appointments & Scheduling | 2 weeks | `[ ]` | Calendar and booking |
| 5 | FHIR/HL7 Integration | 2 weeks | `[ ]` | FHIR R4 API, resource mapping, Bundle export/import |
| 6 | Dashboard & Reports | 2 weeks | `[ ]` | All dashboards + CSV export |
| 7 | Notifications & Audit UI | 1 week | `[ ]` | Notification bell + audit log viewer |
| 8 | Testing, Security & Polish | 2 weeks | `[ ]` | Production-ready quality bar |

**Total estimated duration:** ~16 weeks (part-time) / ~8 weeks (full-time)

---

## Phase 1 — Foundation & Infrastructure

**Goal:** A deployable, authenticated skeleton. Login works end-to-end in Azure. CI/CD pipeline is green. Nothing else.

**Duration:** 2 weeks

### Backend Tasks

- [ ] Create solution with Clean Architecture projects: `HCM.Domain`, `HCM.Application`, `HCM.Infrastructure`, `HCM.API`
- [ ] Configure `appsettings.json` with environment-specific overrides (`appsettings.Development.json`, `appsettings.Production.json`)
- [ ] Wire Azure Key Vault via `DefaultAzureCredential` (Managed Identity in Azure; local dev uses `az login`)
- [ ] Add EF Core with SQL Server provider; create initial `ApplicationDbContext`
- [ ] Create `Users`, `Roles`, `UserRoles`, `RefreshTokens` entities and first migration
- [ ] Implement JWT authentication: `AuthController` with `/login`, `/refresh`, `/logout`
- [ ] Implement refresh token rotation with sliding expiration
- [ ] Add account lockout after 5 failed login attempts
- [ ] Implement global exception handler middleware returning RFC 7807 ProblemDetails
- [ ] Add request logging middleware (Serilog → Application Insights)
- [ ] Add `/health` endpoint checking DB connectivity
- [ ] Add rate limiting on `/api/auth/login` (10 req/min per IP)
- [ ] Seed database: Admin user, all roles

### Frontend Tasks

- [ ] Scaffold Angular project with `ng new hcm-web --routing --style=scss`
- [ ] Install and configure Angular Material
- [ ] Set up `CoreModule` (singleton services), `SharedModule` (reusable components)
- [ ] Implement lazy-loaded `AuthModule`: Login page with Reactive Form
- [ ] Implement `AuthService`: login, logout, token storage, refresh on 401
- [ ] Implement `JwtInterceptor`: attach Bearer token to all API requests
- [ ] Implement `ErrorInterceptor`: map API errors to user-facing toasts
- [ ] Implement `AuthGuard` and `RoleGuard`
- [ ] Create `AppShellComponent`: sidebar nav, top bar, notification placeholder
- [ ] Create placeholder route-guarded stubs for all feature modules (empty components)
- [ ] Configure environment files: `environment.ts`, `environment.prod.ts`

### Infrastructure Tasks

- [ ] Create GitHub repository with branch protection on `main` (require PR + passing CI)
- [ ] Provision Azure resources (staging env): Resource Group, App Service Plan, App Service, Azure SQL, Key Vault, Application Insights, Storage Account, Static Web Apps
- [ ] Configure Managed Identity on App Service; grant Key Vault Secrets User role
- [ ] Store secrets in Key Vault: DB connection string, JWT secret
- [ ] Create GitHub Actions workflow: `build-api.yml` — restore, build, test, publish, deploy to staging App Service
- [ ] Create GitHub Actions workflow: `build-angular.yml` — npm ci, lint, test, build prod, deploy to Static Web Apps
- [ ] Configure CORS on API to accept Angular SWA origin
- [ ] Add `.github/dependabot.yml` for dependency update PRs
- [ ] Add `.gitignore` entries: `appsettings.*.json` (local overrides), `*.user`, `bin/`, `obj/`, `node_modules/`

### Definition of Done — Phase 1
- [ ] User can log in on the deployed staging URL
- [ ] JWT refresh works transparently (no logout on token expiry)
- [ ] GitHub Actions pipeline is green on push to `main`
- [ ] `/health` returns 200 from staging App Service
- [ ] No secrets in source code (verified by GitHub secret scanning)

---

## Phase 2 — Patient & Case Core

**Goal:** Complete patient registration and the full case lifecycle. The primary user flow (create patient → open case → change status → add care team member) works end-to-end.

**Duration:** 3 weeks

### Backend Tasks

- [ ] Add EF Core entities: `Patients`, `PatientInsurance`, `CaseTypes`, `Cases`, `CaseTags`, `CaseCaseTags`, `CaseStatusHistory`, `CareTeamMembers`, `CaseNotes`
- [ ] Create and apply EF Core migration
- [ ] Implement `PatientsController`: full CRUD + paginated search (name, DOB, MRN, insurance ID)
- [ ] Auto-generate MRN on patient create (e.g. `MRN-{year}-{sequence:D5}`)
- [ ] Implement `CasesController`: create, read, update, paginated list with filters
- [ ] Implement case status state machine: validate allowed transitions server-side
- [ ] Auto-generate `CaseNumber` on case create (e.g. `CASE-{year}-{sequence:D5}`)
- [ ] Implement `PUT /api/cases/{id}/status` with status history record
- [ ] Implement care team endpoints: add/remove members, list team
- [ ] Implement `POST /api/cases/{id}/notes` and `GET /api/cases/{id}/notes` (paginated)
- [ ] Background job (Hosted Service): lock `CaseNotes.IsEditable = 0` after 24 hours
- [ ] Implement `UsersController` (Admin only): list, create, update, role assignment, activate/deactivate
- [ ] Add `AuditInterceptor` (EF Core `SaveChanges` override) for Patient, Case, CaseNote, User entities
- [ ] Seed: default CaseTypes (Chronic Disease, Post-Surgery, Mental Health, Preventive, Behavioral), CaseTags

### Frontend Tasks

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

### Definition of Done — Phase 2
- [ ] Admin can create users and assign roles
- [ ] Coordinator can register a patient with insurance info
- [ ] Coordinator can open a case, assign it to themselves, transition through all statuses
- [ ] Care team members can be added and removed; action appears in audit log
- [ ] Notes can be added; notes older than 24h show as read-only
- [ ] Patient list search returns correct results with pagination

---

## Phase 3 — Documents, Tasks & Notes

**Goal:** File upload to Azure Blob Storage is working. Task management within cases is complete.

**Duration:** 2 weeks

### Backend Tasks

- [ ] Add `Documents`, `CaseTasks` EF Core entities; create migration
- [ ] Implement `DocumentsController`:
  - [ ] `POST /upload` — receive multipart form, validate file type and size (max 20 MB), upload to Blob, save metadata to DB
  - [ ] `GET /{id}/download-url` — generate 1-hour SAS URL; log download to audit
  - [ ] `DELETE /{id}` — soft-delete metadata; physical blob deletion deferred
- [ ] Configure Azure Blob Storage client via `DefaultAzureCredential` (Managed Identity); container: `case-documents`
- [ ] Implement `IBlobStorageService` abstraction in Application layer; concrete implementation in Infrastructure
- [ ] Validate allowed MIME types: `application/pdf`, `image/png`, `image/jpeg`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- [ ] Implement `TasksController` (task CRUD, status transitions, `GET /my` for current user's tasks)
- [ ] Validate task status transitions server-side
- [ ] Add task `DueDate` index for overdue query performance

### Frontend Tasks

- [ ] Implement `DocumentsModule` components (embedded in Case Detail and Patient Detail tabs):
  - [ ] `DocumentListComponent`: table with name, category, uploader, date; download button (calls SAS URL endpoint)
  - [ ] `DocumentUploadComponent`: drag-and-drop or file picker, category selector, progress bar
  - [ ] `DocumentService`: upload with `HttpClient` `reportProgress`, download URL fetch
- [ ] Implement `TasksModule` (lazy-loaded):
  - [ ] `MyTasksComponent`: tasks assigned to current user with overdue highlighting
  - [ ] `TaskListComponent`: embedded in Case Detail tasks tab
  - [ ] `TaskFormComponent`: create/edit task, assignee picker, due date picker
  - [ ] `TaskStatusChipComponent`: clickable status chip triggering transition dialog
  - [ ] `TaskService`: all HTTP calls
- [ ] Add `FileUploadComponent` to `SharedModule` (reusable, emits `File` object to parent)
- [ ] Display overdue task count badge in sidebar nav

### Definition of Done — Phase 3
- [ ] User can upload a PDF to a case; file appears in document list
- [ ] Download generates a working SAS URL that expires (verify by waiting or inspecting expiry)
- [ ] Task can be created with an assignee and due date; status transitions work
- [ ] Overdue tasks are highlighted in red in `MyTasksComponent`
- [ ] Deleted documents do not appear in the list but remain in audit log

---

## Phase 4 — Appointments & Scheduling

**Goal:** Appointments can be scheduled, confirmed, and completed. Conflict detection prevents double-booking.

**Duration:** 2 weeks

### Backend Tasks

- [ ] Add `Providers`, `Appointments` EF Core entities; create migration
- [ ] Implement `AppointmentsController`:
  - [ ] `POST /` — create appointment; validate provider has no overlapping appointment
  - [ ] `GET /` — paginated list filtered by patient, provider, status, date range
  - [ ] `PUT /{id}` — reschedule (re-validate conflicts)
  - [ ] `PUT /{id}/status` — Confirm, Complete, Cancel, NoShow
  - [ ] `GET /providers/{providerId}/availability` — return busy slots for a date range
- [ ] Seed initial providers (linked to clinician users)
- [ ] Background Hosted Service: query appointments starting in 24h with `ReminderSentAt IS NULL`; create `Notifications` rows; mark `ReminderSentAt`

### Frontend Tasks

- [ ] Implement `AppointmentsModule` (lazy-loaded):
  - [ ] `AppointmentListComponent`: paginated table, filter by patient/provider/status/date
  - [ ] `AppointmentFormComponent`: patient picker, provider picker, type selector, date/time range picker, conflict warning
  - [ ] `AppointmentDetailComponent`: status actions (Confirm, Complete, Cancel, NoShow)
  - [ ] `ProviderAvailabilityComponent`: simple slot grid for selected provider + date
  - [ ] `AppointmentService`: all HTTP calls
- [ ] Add upcoming appointments section to Case Detail

### Definition of Done — Phase 4
- [ ] Appointment can be scheduled and confirmed
- [ ] Attempting to book a provider at an occupied time shows a conflict error
- [ ] Status transitions (NoShow, Complete, Cancel) work
- [ ] A notification row is created for appointments starting in < 24h (verify via DB query)

---

## Phase 5 — FHIR/HL7 Integration

**Goal:** A FHIR R4 API facade is live. Recruiters and reviewers can query `/fhir/R4/metadata`, retrieve a patient as a FHIR resource, and download a full FHIR Bundle. Patient import from a FHIR JSON payload works. HL7 v2 ADT receiver is a stretch goal.

**Duration:** 2 weeks

### Backend Tasks

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

### Frontend Tasks

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

### Definition of Done — Phase 5
- [ ] `GET /fhir/R4/metadata` returns a valid FHIR CapabilityStatement (validate with Firely or online FHIR validator)
- [ ] `GET /fhir/R4/Patient?name=Smith` returns a FHIR searchset Bundle with correct entries
- [ ] `GET /fhir/R4/Patient/{id}/$everything` returns a Bundle containing at least Patient + Encounter resources
- [ ] Importing a FHIR Patient JSON via the UI creates a new patient in the system with a generated MRN
- [ ] The download from "Export as FHIR Bundle" is valid JSON parseable as a FHIR R4 Bundle
- [ ] FHIR endpoints return 401 when called without a JWT; errors return OperationOutcome (not ProblemDetails)

---

## Phase 6 — Dashboards & Reports

**Goal:** All three dashboards are populated with live data. CSV export works for all three report types.

**Duration:** 2 weeks

### Backend Tasks

- [ ] Implement `DashboardController`:
  - [ ] `GET /coordinator` — my open cases (by status), my overdue tasks, today's appointments, cases with no activity in 7 days
  - [ ] `GET /supervisor` — team caseload per user, escalated cases, inactive cases
  - [ ] `GET /admin` — active user count, logins last 7 days, error rate from App Insights (or simple DB counts)
- [ ] Implement `ReportsController`:
  - [ ] `GET /cases-by-status` — JSON + CSV via `format=csv` query param; use `CsvHelper` NuGet
  - [ ] `GET /case-duration` — average days open→closed by case type
  - [ ] `GET /tasks-by-assignee` — done vs. overdue vs. open per user
- [ ] Add DB indexes to support dashboard queries efficiently (case status + coordinator, task due date + assignee)

### Frontend Tasks

- [ ] Implement `DashboardModule` (lazy-loaded):
  - [ ] `CoordinatorDashboardComponent`: stat cards (open cases, overdue tasks, today's appointments), cases-by-status donut chart, overdue task list
  - [ ] `SupervisorDashboardComponent`: team caseload bar chart, escalated cases table, inactive cases table
  - [ ] `AdminDashboardComponent`: user counts, login trend line chart, top error list
- [ ] Implement `ReportsModule` (lazy-loaded):
  - [ ] `CasesByStatusReportComponent`: bar chart + table + CSV export button
  - [ ] `CaseDurationReportComponent`: bar chart per case type + CSV export
  - [ ] `TasksByAssigneeReportComponent`: stacked bar chart + CSV export
- [ ] Install and configure `ng2-charts` + `Chart.js`
- [ ] Route dashboard based on user's primary role (default landing page post-login)

### Definition of Done — Phase 6
- [ ] Coordinator dashboard shows accurate counts matching DB state
- [ ] CSV export downloads a valid file with correct headers and data
- [ ] Charts render without console errors
- [ ] Supervisor can see their team's caseloads

---

## Phase 7 — Notifications & Audit UI

**Goal:** In-app notification bell works. Audit log is browsable by admins.

**Duration:** 1 week

### Backend Tasks

- [ ] Ensure `Notifications` entity and migration exist (may be done in Phase 4)
- [ ] Wire notification creation in application layer for all trigger events: case assigned, task assigned, task overdue (daily check via Hosted Service), escalation, appointment reminder
- [ ] Implement `NotificationsController`: list, unread count, mark read, mark all read
- [ ] Implement `AuditController` (Admin only): paginated audit log with filters (entity type, user, date range, action)

### Frontend Tasks

- [ ] Implement `NotificationsModule`:
  - [ ] `NotificationBellComponent`: icon in top bar; polls `GET /notifications/unread-count` every 60s; badge
  - [ ] `NotificationListComponent`: dropdown/panel showing latest 20 notifications with mark-read action
  - [ ] `NotificationService`: HTTP calls + polling
- [ ] Implement `AdminModule` → `AuditLogComponent`:
  - [ ] Filterable, paginated table of all audit events
  - [ ] Filter by: entity type, user (autocomplete), date range, action
  - [ ] Row expand: show OldValues / NewValues JSON diff (basic formatted JSON view)

### Definition of Done — Phase 7
- [ ] Notification bell updates without page refresh
- [ ] Notifications are created when a case is assigned
- [ ] Admin can filter audit log by user and see JSON old/new values
- [ ] Non-admin users cannot access `/api/audit` (403 returned)

---

## Phase 8 — Testing, Security & Polish

**Goal:** Production-quality build. Key flows covered by tests. Security hardened.

**Duration:** 2 weeks

### Backend Testing
- [ ] Unit tests (xUnit + Moq): all Application layer use cases / command handlers
- [ ] Integration tests (xUnit + `WebApplicationFactory` + Testcontainers SQL Server):
  - [ ] Auth flow: login → access protected route → refresh → logout
  - [ ] Patient CRUD
  - [ ] Case status machine: valid and invalid transitions
  - [ ] Document upload and SAS URL generation (mock Blob)
- [ ] Code coverage report: target ≥ 70% on Application layer
- [ ] `dotnet-security-scan` or OWASP dependency check in CI pipeline

### Frontend Testing
- [ ] Unit tests (Jasmine/Karma): `AuthService`, `JwtInterceptor`, form validators
- [ ] E2E tests (Playwright or Cypress): login flow, create patient, create case, upload document
- [ ] Run `ng build --configuration production` with `--stats-json`; analyse bundle size
- [ ] Accessibility audit: run Lighthouse on Login, Case List, Case Detail pages; target ≥ 90 accessibility score

### Security Hardening
- [ ] Verify no PHI (names, DOB, MRN) appears in Application Insights traces or logs
- [ ] Add `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options` headers to API responses
- [ ] Rotate JWT secret in Key Vault; confirm app picks up new version without redeploy (Key Vault reference auto-refresh)
- [ ] Confirm SAS URLs are not logged (check Application Insights live stream during a download)
- [ ] Run OWASP ZAP quick scan against staging API; resolve any Medium+ findings
- [ ] Confirm `AuditController` returns 403 when called with a non-Admin JWT

### Production Deploy & Polish
- [ ] Provision prod Azure resources (separate Resource Group from staging)
- [ ] Configure manual approval gate in GitHub Actions for prod deploy
- [ ] Set up Application Insights alerts: error rate > 5%, availability < 99%, p95 > 2s
- [ ] Add budget alert on Azure subscription ($20/month threshold for portfolio)
- [ ] Update `README.md`: architecture diagram, local setup steps, environment variable guide, screenshots
- [ ] Record a 5-minute demo walkthrough video for portfolio

### Definition of Done — Phase 8
- [ ] CI pipeline runs all tests; build fails if tests fail
- [ ] Lighthouse accessibility score ≥ 90 on primary pages
- [ ] OWASP ZAP scan shows no Medium or Critical findings
- [ ] Prod deployment is behind a manual approval gate
- [ ] Application Insights shows live traces from prod environment
- [ ] `README.md` is complete with setup instructions

---

## Backlog (Post-Phase 7)

These items are intentionally deferred to keep the project shippable within the 14-week plan.

| Item | Notes |
|---|---|
| Email notifications via SendGrid | Phase 4 laid the groundwork; wire SMTP in Phase 7+ |
| Patient self-service portal | Separate Angular app or module; requires patient user role |
| SMART on FHIR authentication | Replace JWT with Azure AD OIDC + SMART app launch framework; out of scope for portfolio |
| Full bi-directional EHR sync | Phase 5 covers read/export/import; real-time sync with live EHR systems is out of scope |
| Multi-tenancy (multiple organizations) | Schema-per-tenant or row-level security |
| Azure Active Directory SSO | Replace JWT with Azure AD OIDC |
| Real-time notifications via SignalR | Replace 60s polling with WebSocket push |
| Mobile PWA | Progressive Web App manifest + offline mode |
| Physical blob deletion job | Azure Function triggered nightly to clean soft-deleted blobs |
| Two-factor authentication | TOTP or SMS-based |
| Document versioning | Track document versions with history |

---

## Progress Summary

| Phase | Tasks Total | Complete | Remaining |
|---|---|---|---|
| Phase 1 — Foundation | ~29 | 0 | 29 |
| Phase 2 — Patient & Case Core | ~38 | 0 | 38 |
| Phase 3 — Documents & Tasks | ~22 | 0 | 22 |
| Phase 4 — Appointments | ~16 | 0 | 16 |
| Phase 5 — FHIR/HL7 Integration | ~24 | 0 | 24 |
| Phase 6 — Dashboards & Reports | ~20 | 0 | 20 |
| Phase 7 — Notifications & Audit UI | ~14 | 0 | 14 |
| Phase 8 — Testing & Polish | ~22 | 0 | 22 |
| **Total** | **~185** | **0** | **185** |

_Update task counts as tasks are added or split during development._
