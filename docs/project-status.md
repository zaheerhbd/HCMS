# HCMS — Project Status

**Last Updated:** 2026-11-27 (Docs synced with code — stale tech/HL7/Key Vault references removed; Phase 4/5 status corrected; blockers refreshed)
**Current Phase:** Phase 8 — Testing, Security & Polish (In Progress)
**Overall Progress:** 162 / 205 tasks complete (~79%)

> **Local dev confirmed working:** API starts, DB created, migrations applied, seed runs. Run with `dotnet run` from `src/HCM.API`. Login: `admin` / `Admin@123!`.

> Update this file at the start of each working session. It should answer: where are we, what's blocking us, what's next.

---

## Phase Overview

| # | Phase | Status | Tasks | Done | Remaining |
|---|---|---|---|---|---|
| 1 | Foundation & Infrastructure | `Complete` | ~29 | 29 | 0 |
| 2 | Patient & Case Core | `Complete` | ~38 | 38 | 0 |
| 3 | Documents, Tasks & Notes | `Complete` | ~22 | 22 | 0 |
| 4 | Appointments & Scheduling | `In Progress` | ~16 | 12 | 4 |
| 5 | FHIR Integration | `Build complete` (DoD checks pending) | ~32 | 32 | 0 |
| 6 | Dashboards & Reports | `Complete` | ~20 | 20 | 0 |
| 7 | Notifications & Audit UI | `Complete` | ~14 | 14 | 0 |
| 8 | Testing, Security & Polish | `In Progress` | ~22 | 5 | 17 |
| 9 | Advanced FHIR & Interoperability | `Complete` | ~12 | 12 | 0 |

---

## Current Phase Detail — Phase 8: Testing, Security & Polish

**Goal:** Production-quality bar — automated test coverage on key backend flows, security hardening, and deploy polish.

**Status:** In Progress — backend unit + integration testing done; frontend testing, security hardening, and production polish remaining.

### Completed in Phase 8 (this session)
- `src/HCM.Application.Tests` — 43 xUnit unit tests covering `AuthDataHandler` (login, lockout after 5 attempts, validation), `PatientDataHandler` (MRN generation, soft delete, search), `CaseDataHandler` (full status state-machine, table-driven valid/invalid transitions), `DocumentDataHandler` (upload validation, SAS URL expiry, soft delete), `NotificationDataHandler` (create/read/mark-read/mark-all-read). Runs against EF Core's InMemory provider through the real `ApplicationDbContext` rather than hand-mocking `DbSet<T>`, so production entity configs/keys/relationships are exercised as-is.
- `src/HCM.API.Tests` — 26 xUnit integration tests booting the real HCM.API pipeline via `WebApplicationFactory<Program>`: auth flow (login, bad password, protected-route access, 5-failed-attempt lockout), patient CRUD + RBAC, case creation + status transitions + RBAC, and a dedicated RBAC verification suite across `ReadOnly`/`CareCoordinator`/`Admin`.
- Docker's daemon wasn't running in this dev environment, so the integration suite substitutes EF Core InMemory for Testcontainers SQL Server and a fake `IBlobStorageService` for Azurite — DI overrides only; the controllers/middleware/JWT auth under test are unchanged production code. `DataSeeder.SeedAsync` now branches on `Database.IsInMemory()`.
- Found a real product gap while testing: `AuthController.Refresh` and `Logout` are unimplemented `// TODO` stubs — filed as [GitHub issue #5](https://github.com/zaheerhbd/HCMS/issues/5).
- `docs/testing-and-audit/` — one markdown doc per Phase 8 test section (unit, integration, coverage/CI, Angular unit, E2E, Lighthouse, security headers, PHI audit, ZAP, RBAC, Azure/CI-CD, README/demo, definition of done), a navigable `test-results.html` report, and plain-language `unit-and-integration-testing-explained.html` (backend) / `angular-testing-explained.html` (frontend) guides.
- Angular unit tests (Jasmine/Karma) — 32 tests across `src-ng/hcm-web/src/app/**/*.spec.ts`: `AuthService` (login/logout/token storage), `JwtInterceptor` (Bearer header attach + 401 refresh-and-retry + infinite-loop guard), `NotificationService` (60s polling via `fakeAsync`), `CaseFormComponent`/`PatientFormComponent` (Reactive Form validation), `HasRoleDirective` (role-gated rendering). Verified via `ng test --code-coverage`; coverage confirms each spec exercises its real source file.

### Remaining in Phase 8
- Code coverage report (target ≥ 70% on Application layer) + OWASP dependency-check in CI
- Playwright E2E flows (login, create patient, create case, upload document)
- Lighthouse accessibility audit + bundle size analysis
- Security headers, PHI/SAS-URL log audit, OWASP ZAP scan
- Production deploy polish: manual approval gate, App Insights alerts, budget alert, README + demo video

---

## Completed Phase Detail — Phase 9: Advanced FHIR & Interoperability

**Goal:** Atomic FHIR Transaction Bundle import (Patient + Encounter + Appointment + Task), via JSON body or file upload. (HL7 v2 ADT/ORU ingestion was built, then removed on 2026-07-05.)

**Status:** Complete — all backend and frontend tasks done, builds clean (0 errors).

### Completed in Phase 9
- `FhirBundleImportResultDto` + `BundleEntryResultDto` DTOs (Application layer)
- `ParsedBundleEntry` / `ParsedFhirPatient` / `ParsedFhirEncounter` / `ParsedFhirAppointment` / `ParsedFhirTask` records (Application layer — no Firely SDK types leak out of Infrastructure)
- `IApplicationDbContext.BeginTransactionAsync` — new interface method for atomic EF Core transactions
- `ParseTransactionBundle` + `MapTransactionResponse` added to `IFhirMappingService` / `FhirMappingService`
- `ImportTransactionBundleAsync` in `FhirDataHandler` — open SQL Server transaction → Patient → Encounter → Appointment → Task → FhirImportLogs + AuditLog → commit; rollback on any exception
- `urn:uuid:` reference resolution via `urnMap` + `ResolveId` helper
- `POST /fhir/R4/` endpoint in `FhirController` — returns `transaction-response` Bundle on success, `OperationOutcome` on failure
- `POST /fhir/R4/$import` — multipart/form-data file-upload endpoint; reads `IFormFile bundleFile` server-side, delegates to `ImportTransactionBundleAsync`
- Angular: `uploadBundleFile(file)` added to `FhirService`; Import Bundle tab rebuilt with drag-and-drop upload zone as primary path; "or paste JSON" textarea as fallback
- Case assignment in bundles: Encounter extension `http://hcms.local/fhir/StructureDefinition/case-assigned-to` (`valueString`: username) resolved to coordinator `Guid` in `FhirDataHandler`; falls back to importer if username not found
- `docs/phase-9-advanced-fhir-interoperability.html` completion report generated

---

## Previous Phase Detail — Phase 6: Dashboards & Reports

**Goal:** Role-based dashboards with live data and Chart.js charts; three report types with CSV export.

**Status:** Complete — all backend and frontend tasks done, builds clean (0 errors).

### Completed in Phase 6
- `DashboardDataHandler` — coordinator (open cases, overdue tasks, today's appointments, inactive cases); supervisor (team caseloads, escalated, inactive); admin (user/patient/case totals, login trend, FHIR imports)
- `ReportDataHandler` — cases-by-status, case-duration (client-side computation avoids SQL Server-specific EF), tasks-by-assignee; all three CSV exports (RFC 4180 via `StringBuilder`)
- `DashboardController` at `/api/dashboard/{coordinator|supervisor|admin}` with RBAC
- `ReportsController` at `/api/reports/{cases-by-status|case-duration|tasks-by-assignee}?format=csv`
- Registered `IDashboardDataHandler` and `IReportDataHandler` in `DependencyInjection.cs`
- Angular `DashboardService` + `ReportService`
- Role-based dashboard hub (`DashboardComponent`) — picks correct sub-dashboard by role
- `CoordinatorDashboardComponent` — doughnut chart (Chart.js) + overdue tasks table + today's appointments table
- `SupervisorDashboardComponent` — grouped bar chart + escalated/inactive case tables
- `AdminDashboardComponent` — line chart (login trend) + bar chart (users by role) + stat cards
- Three report components (each lazy-loaded): `CasesByStatusComponent`, `CaseDurationComponent`, `TasksByAssigneeComponent`; each has a Chart.js chart + data table + CSV download button
- Chart.js 4.5.1 installed directly (no ng2-charts wrapper)
- Reports feature uses `REPORTS_ROUTES` with `mat-tab-nav-bar` hub
- Fixed `ApplicationDbContext.BeginTransactionAsync` missing implementation

---

## Previous Phase Detail — Phase 7: Notifications & Audit UI

**Goal:** In-app notification bell + audit log viewer for admins.

**Status:** Complete — all backend and frontend tasks done, builds clean.

### Completed in Phase 7
- `Notification` entity + `AddNotifications` EF migration applied
- `NotificationDataHandler` — create, list, unread count, mark read, mark all read
- Notification triggers wired: `CaseAssigned`, `TaskAssigned` (create + reassign), `CaseEscalated`
- `NotificationOverdueService` (BackgroundService) — daily overdue task scan, deduped per day
- `NotificationsController` at `GET/POST /api/notifications`
- `AuditDataHandler` + `AuditController` at `GET /api/audit` (Admin-only), filters by entityType, action, changedBy, date range
- Angular `NotificationService` with 60s polling + `BehaviorSubject` unread count
- `NotificationBellComponent` in topbar — badge, dropdown with mark-read per item + mark all
- `AuditLogComponent` — paginated Material table, expandable Before/After JSON diff, color-coded action badges
- Admin routing refactored to lazy-loaded `ADMIN_ROUTES`; `/admin/audit` child route added

---

## Previous Phase Detail — Phase 5: FHIR Integration

**Goal:** Expose a FHIR R4-compliant REST API at `/fhir/R4/`; support Patient, Encounter (Case), Appointment, Task, DocumentReference read + search; `$everything` Bundle operation; single-Patient import from FHIR JSON (parse-and-preview UI); atomic Transaction Bundle import (Patient + Encounter + Appointment + Task in one call).

**Status:** Build complete — Transaction Bundle import (FR-55–FR-58) was delivered in Phase 9. Only the Definition of Done verification checks remain (see [phase-5.md](phases/phase-5.md)).

### Completed in Phase 5
- FHIR CapabilityStatement (Firely `TypeElement` fix for search params)
- Patient, Encounter, Appointment, Task, DocumentReference read + search
- `$everything` Bundle for full patient export
- Single-patient FHIR import (`POST /fhir/R4/Patient`) with FhirImportLogs
- Angular `FhirExplorerComponent` + `FhirImportComponent`
- "Export as FHIR Bundle" button on PatientDetail
- MRN generation off-by-one bug fixed

### Remaining in Phase 5
- Verify the six Definition of Done checks (CapabilityStatement validity, search Bundles, `$everything`, UI import, export JSON validity, 401/OperationOutcome) — fits naturally into the Phase 8 Playwright/E2E work

---

## Completed Phase Detail — Phase 2: Patient & Case Core

**Goal:** Complete patient registration and the full case lifecycle. The primary user flow (create patient → open case → change status → add care team member) works end-to-end.

**Status:** Nearly complete — all backend and frontend tasks done; 4 minor items remain (UsersController UI, breadcrumb polish, phase-3 tasks moved up)

### Backend Checklist (Phase 2) — Complete

- [x] Add EF Core entities: `Patients`, `PatientInsurance`, `CaseTypes`, `Cases`, `CaseTags`, `CaseCaseTags`, `CaseStatusHistory`, `CareTeamMembers`, `CaseNotes`
- [x] Create and apply EF Core migration (`AddPatientCaseCaseManagement`)
- [x] Create DTOs for all features (Patients, Cases, CareTeam, CaseNotes)
- [x] Implement `PatientDataHandler`: CRUD, MRN generation (`MRN-{YYYY}-{NNNNN}`), paginated search
- [x] Implement `CaseDataHandler`: CRUD, case number generation (`CASE-{YYYY}-{NNNNN}`), status state machine
- [x] Implement `CareTeamDataHandler`: add/remove members (soft removal preserves history), list team
- [x] Implement `CaseNoteDataHandler`: add notes (≤5000 chars), list with pagination, soft delete
- [x] Register all DataHandlers in `DependencyInjection.cs`
- [x] Implement `PatientsController`: full CRUD + paginated search with RBAC
- [x] Implement `CasesController`: CRUD + status transitions with history, RBAC
- [x] Implement `CareTeamController`: add/remove members, list team, RBAC
- [x] Implement `CaseNotesController`: add/list notes, RBAC
- [x] Seed CaseTypes (5 types) and CaseTags (10 tags) in `DataSeeder`
- [x] `AuditInterceptor` — `SaveChangesAsync` override capturing Patient/Case/CaseNote/User changes; migration `AddAuditLogs` applied
- [x] `UsersController` + `IUserDataHandler`/`UserDataHandler` (Admin only): list, create, update, role assignment, activate/deactivate
- [x] `CaseNoteEditLockService` (BackgroundService): locks `IsEditable = false` after 24h
- [x] All API routes use MRN/CaseNumber business keys instead of GUIDs (IDOR hardening); `NEWSEQUENTIALID()` default applied to all 5 entity Id columns via migration `UseSequentialGuids`

### Frontend Checklist (Phase 2) — Complete

- [x] `PatientService` — all HTTP calls to `/api/patients`
- [x] `PatientListComponent` — server-side paginated table with debounced search, role-gated create button
- [x] `PatientDetailComponent` — demographics, insurance accordion, cases tab, documents placeholder, breadcrumb
- [x] `PatientFormComponent` — reactive form (create + edit), datepicker, validation
- [x] Update `patients.routes.ts` — `''`, `new`, `:mrn`, `:mrn/edit`
- [x] `CaseService` — all HTTP calls to `/api/cases` (CRUD, status, team, notes)
- [x] `CaseListComponent` — status filter + paginated table
- [x] `CaseDetailComponent` — header card with inline status transition, tabs (Notes, Team, History, Tasks placeholder, Docs placeholder)
- [x] `CaseFormComponent` — patient picker + case type select
- [x] `CaseStatusHistoryComponent` — timeline of status changes
- [x] `CareTeamComponent` — member list + add/remove with ConfirmDialog
- [x] `CaseNotesComponent` — post notes + locked badge after 24h
- [x] Update `cases.routes.ts` — `''`, `new`, `:caseNumber`
- [x] `StatusBadgeComponent` — color-coded chip per status
- [x] `HasRoleDirective` — `*appHasRole` structural directive
- [x] `ConfirmDialogComponent` — reusable confirm modal
- [x] Breadcrumb navigation in PatientDetail and CaseDetail

---

## Previous Phase Detail — Phase 1: Foundation & Infrastructure

**Goal:** A deployable, authenticated skeleton. Login works end-to-end in Azure. CI/CD pipeline is green.

### Status: In Progress — Azure resources live; verifying end-to-end login

### Backend Checklist (Phase 1)

- [x] Solution: `HCM.Domain`, `HCM.Application`, `HCM.Infrastructure`, `HCM.API`
- [x] `appsettings.json` + environment overrides
- [-] Azure Key Vault via `DefaultAzureCredential` ← deferred (tenant mismatch; using App Settings instead)
- [x] EF Core + SQL Server provider + `ApplicationDbContext`
- [x] `Users`, `Roles`, `UserRoles`, `RefreshTokens` entities + first migration (`InitialCreate`)
- [x] JWT auth: `AuthController` (`/login`, `/refresh`, `/logout`)
- [x] Refresh token rotation with sliding expiration
- [x] Account lockout after 5 failed attempts
- [x] Global exception handler → RFC 7807 ProblemDetails
- [x] Serilog request logging (Application Insights sink installed)
- [x] `/health` endpoint (DB connectivity check)
- [x] Rate limiting on `/api/auth/login` (10 req/min per IP)
- [x] Database seed: Admin user + all roles

### Frontend Checklist (Phase 1)

- [x] Scaffold Angular 19 project (`ng new hcm-web --routing --style=scss`)
- [x] Install Angular Material
- [x] Core services + interceptors + guards in `core/` (standalone pattern)
- [x] Login page with Reactive Form (`features/auth/login/`)
- [x] `AuthService` — login, logout, token storage, refresh-on-401
- [x] `JwtInterceptor` — attach Bearer token + auto-refresh on 401
- [x] `ErrorInterceptor` — API errors → Material snack bar toasts
- [x] `authGuard` + `roleGuard`
- [x] `AppShellComponent` — sidebar nav, top bar, notification placeholder
- [x] Placeholder stubs for all feature modules (Dashboard, Patients, Cases, Tasks, Documents, Appointments, Reports, Admin)
- [x] Environment files: `environment.ts`, `environment.prod.ts`

### Infrastructure Checklist (Phase 1)

- [ ] GitHub branch protection on `main` ← do in GitHub UI
- [x] Azure prod resources provisioned (Resource Group, App Service Plan, App Service, Azure SQL, App Insights, Static Web Apps)
- [-] Managed Identity on App Service + Key Vault role ← deferred (Key Vault skipped)
- [-] Secrets in Key Vault ← deferred; secrets set directly in App Service Application Settings (`Jwt__Secret`, `ConnectionStrings__DefaultConnection`)
- [x] `AZURE_WEBAPP_PUBLISH_PROFILE` secret added to GitHub repo
- [x] `AZURE_STATIC_WEB_APPS_API_TOKEN` secret added to GitHub repo
- [x] GitHub Actions: `build-api.yml`
- [x] GitHub Actions: `build-angular.yml`
- [x] CORS configured (in `Program.cs` via `appsettings.json`)
- [x] `.github/dependabot.yml`

### Definition of Done — Phase 1

- [ ] User can log in on the deployed prod URL (`https://jolly-bay-072046c10.7.azurestaticapps.net/login`) ← pending API startup fix
- [ ] JWT refresh works transparently (no logout on token expiry)
- [x] GitHub Actions pipeline is green on push to `main` (both `build-api.yml` and `build-angular.yml`)
- [ ] `/health` returns 200 from prod App Service ← verify after Key Vault fix deploys
- [x] Angular SPA loads from `https://jolly-bay-072046c10.7.azurestaticapps.net`
- [x] No secrets in source code (verified by GitHub secret scanning)

---

## Blockers

**Open: Token refresh and logout not implemented** — [issue #5](https://github.com/zaheerhbd/HCMS/issues/5)

`AuthController.Refresh` and `Logout` are `// TODO` stubs. The Angular `jwtInterceptor` already calls refresh on 401, so an expired access token currently logs the user out. Blocks the Phase 8 "login → refresh → logout" integration test and the Phase 1 DoD item "JWT refresh works transparently".

**Unverified: production login end-to-end**

The June deployment fixes (Key Vault removal, `angular.json` `fileReplacements`, `Jwt__Secret` ≥ 32 chars) have not been recorded as verified. To check:
- `/health` on `hcm-prod-api` returns `{"status":"Healthy"}`
- `POST /api/auth/login` with `admin` / `Admin@123!` returns 200 + tokens
- `https://jolly-bay-072046c10.7.azurestaticapps.net/login` reaches the dashboard

**Pending: GitHub branch protection on `main`**

GitHub repo → Settings → Branches → Add rule → `main` → require the `build-and-deploy` status checks (API and Angular) before merging.

**Known gaps (not blockers)**
- No edit-note endpoint — case notes are append-only from creation; `CaseNoteEditLockService` has nothing to protect yet.
- Admin user-management UI not built (backend complete).
- Document UI hidden (backend complete).
- Phase 4: `ProviderAvailabilityComponent`, appointments section on Case Detail, and the < 24h appointment reminder notification are not built.

**Resolved: Key Vault tenant mismatch** — Key Vault removed; secrets supplied via App Service Application Settings.

**Resolved: Angular prod build using wrong environment file** — `fileReplacements` added in commit `0f5b07a`.

---

## Decisions Made

| Date | Decision | Rationale |
|---|---|---|
| 2026-06-22 | JWT + refresh token (not Azure AD) — refresh not yet implemented, issue #5 | Sufficient for portfolio; avoids Azure AD cost and complexity |
| 2026-06-22 | Soft deletes only (no hard deletes) | HIPAA-aligned audit integrity |
| 2026-06-22 | Firely SDK for FHIR (not hand-rolled) | Official .NET FHIR SDK; handles serialization + validation |
| 2026-06-22 | EF Core only (no raw SQL) | Prevents SQL injection; easier migrations |
| 2026-06-22 | Angular Material as UI library | Enterprise look + WCAG 2.1 AA built-in |
| 2026-06-27 | DataHandler pattern (no MediatR) | Simpler than MediatR for this project scope; handlers combine validation + business logic; injected directly into controllers as scoped dependencies |
| 2026-06-23 | .env deferred (not native to .NET/Angular) | .NET uses appsettings.Development.json; Angular uses environment.ts; .env.example kept for reference and CI/CD |
| 2026-06-24 | Single prod environment only (no staging) | Portfolio project — staging adds cost and complexity with no benefit; deploy directly from `main` to prod (`hcm-prod-rg`). Azure resources use `hcm-prod-*` naming. |
| 2026-06-25 | Skip Azure Key Vault; use App Service Application Settings for secrets | Key Vault was provisioned in a different Azure AD tenant (West Europe, `adf10e2b-...`) from the App Service (`397f0f29-...`), causing `AKV10032` on every startup. Fixing the tenant would require re-provisioning Key Vault in the correct region/tenant; not worth it for a portfolio project. Secrets go directly into App Service config using double-underscore notation. |
| 2026-07-05 | Remove HL7 v2 ingestion; use FHIR bundle file upload instead | Bundle upload (`POST /fhir/R4/$import`) covers the intake use case with one standard format; dropping NHapi removed a second parsing stack. FR-53/54 withdrawn. |
| 2026-11-27 (recorded) | Service-level RxJS `BehaviorSubject` for auth + notification state instead of NgRx | What the code actually does; state is small (auth flag, unread count). NgRx packages removed 2026-11-27. |

---

## What's Next (After Phase 1)

Phase 2 starts once all Phase 1 Definition of Done items are checked off.

Phase 2 adds: Patient registration, full case lifecycle, care team management, case notes, user management (Admin), and the audit interceptor.

---

## Key Links

| Resource | URL / Path |
|---|---|
| Phase task checklist | [docs/phase-tracker.md](phase-tracker.md) |
| Full requirements | [docs/product-requirements.md](product-requirements.md) |
| Architecture | [docs/architecture.md](architecture.md) |
| Feature reference | [docs/feature-reference.md](feature-reference.md) |
| Changelog | [docs/changelog.md](changelog.md) |
| Prototype (reference UI) | [prototype/index.html](../prototype/index.html) |
