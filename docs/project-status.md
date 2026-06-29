# HCMS — Project Status

**Last Updated:** 2026-06-29 (Phase 2 nearly complete: backend fully done including audit, seeding, users, background job; Angular frontend done)
**Current Phase:** Phase 2 — Patient & Case Core (Backend + Frontend complete; SharedModule done)
**Overall Progress:** 58 / 185 tasks complete (~31%)

> **Local dev confirmed working:** API starts, DB created, migrations applied, seed runs. Run with `dotnet run` from `src/HCM.API`. Login: `admin` / `Admin@123!`.

> Update this file at the start of each working session. It should answer: where are we, what's blocking us, what's next.

---

## Phase Overview

| # | Phase | Status | Tasks | Done | Remaining |
|---|---|---|---|---|---|
| 1 | Foundation & Infrastructure | `Complete` | ~29 | 29 | 0 |
| 2 | Patient & Case Core | `In Progress` | ~38 | 34 | 4 |
| 3 | Documents, Tasks & Notes | `Not Started` | ~22 | 0 | 22 |
| 4 | Appointments & Scheduling | `Not Started` | ~16 | 0 | 16 |
| 5 | FHIR/HL7 Integration | `Not Started` | ~24 | 0 | 24 |
| 6 | Dashboards & Reports | `Not Started` | ~20 | 0 | 20 |
| 7 | Notifications & Audit UI | `Not Started` | ~14 | 0 | 14 |
| 8 | Testing, Security & Polish | `Not Started` | ~22 | 0 | 22 |

---

## Current Phase Detail — Phase 2: Patient & Case Core

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

### Frontend Checklist (Phase 2) — Complete

- [x] `PatientService` — all HTTP calls to `/api/patients`
- [x] `PatientListComponent` — server-side paginated table with debounced search, role-gated create button
- [x] `PatientDetailComponent` — demographics, insurance accordion, cases tab, documents placeholder, breadcrumb
- [x] `PatientFormComponent` — reactive form (create + edit), datepicker, validation
- [x] Update `patients.routes.ts` — `''`, `new`, `:id`, `:id/edit`
- [x] `CaseService` — all HTTP calls to `/api/cases` (CRUD, status, team, notes)
- [x] `CaseListComponent` — status filter + paginated table
- [x] `CaseDetailComponent` — header card with inline status transition, tabs (Notes, Team, History, Tasks placeholder, Docs placeholder)
- [x] `CaseFormComponent` — patient picker + case type select
- [x] `CaseStatusHistoryComponent` — timeline of status changes
- [x] `CareTeamComponent` — member list + add/remove with ConfirmDialog
- [x] `CaseNotesComponent` — post notes + locked badge after 24h
- [x] Update `cases.routes.ts` — `''`, `new`, `:id`
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

**Pending: confirm login works end-to-end**

Three fixes were deployed this session — verify all are live:
1. Key Vault code removed (`f566766`) — API should start without `AKV10032` crash.
2. `angular.json` `fileReplacements` added (`0f5b07a`) — SPA now bundles the Azure API URL instead of `localhost:5144`.
3. `Jwt__Secret` in App Service must be **≥ 32 characters** — the original value was 16 chars (128 bits), causing `IDX10720` on every login attempt. Update via App Service → Configuration → `Jwt__Secret`.

Steps to verify:
- Hit `/health` — should return `{"status":"Healthy"}`.
- Hit `/swagger` → try POST `/api/auth/login` with `admin` / `Admin@123!` → should return 200 + tokens.
- Open `https://jolly-bay-072046c10.7.azurestaticapps.net/login` → login → should reach the dashboard.

**Pending: GitHub branch protection on `main`**

GitHub repo → Settings → Branches → Add rule → Branch name pattern: `main` → Require status checks before merging → select `build-and-deploy` (API) and `build-and-deploy` (Angular).

**Resolved: Key Vault tenant mismatch**

Key Vault (`hcm-prod-kv`) was created in West Europe under a different Azure AD tenant than the App Service, causing `AKV10032 Invalid issuer` on every startup. Decision: remove Key Vault integration; supply secrets directly via App Service Application Settings.

**Resolved: Angular prod build using wrong environment file**

`angular.json` was missing `fileReplacements` in the production configuration, so `environment.prod.ts` was never used. Fixed in commit `0f5b07a`.

---

## Decisions Made

| Date | Decision | Rationale |
|---|---|---|
| 2026-06-22 | JWT + refresh token (not Azure AD) | Sufficient for portfolio; avoids Azure AD cost and complexity |
| 2026-06-22 | Soft deletes only (no hard deletes) | HIPAA-aligned audit integrity |
| 2026-06-22 | Firely SDK for FHIR (not hand-rolled) | Official .NET FHIR SDK; handles serialization + validation |
| 2026-06-22 | EF Core only (no raw SQL) | Prevents SQL injection; easier migrations |
| 2026-06-22 | Angular Material as UI library | Enterprise look + WCAG 2.1 AA built-in |
| 2026-06-27 | DataHandler pattern (no MediatR) | Simpler than MediatR for this project scope; handlers combine validation + business logic; injected directly into controllers as scoped dependencies |
| 2026-06-23 | .env deferred (not native to .NET/Angular) | .NET uses appsettings.Development.json; Angular uses environment.ts; .env.example kept for reference and CI/CD |
| 2026-06-24 | Single prod environment only (no staging) | Portfolio project — staging adds cost and complexity with no benefit; deploy directly from `main` to prod (`hcm-prod-rg`). Azure resources use `hcm-prod-*` naming. |
| 2026-06-25 | Skip Azure Key Vault; use App Service Application Settings for secrets | Key Vault was provisioned in a different Azure AD tenant (West Europe, `adf10e2b-...`) from the App Service (`397f0f29-...`), causing `AKV10032` on every startup. Fixing the tenant would require re-provisioning Key Vault in the correct region/tenant; not worth it for a portfolio project. Secrets go directly into App Service config using double-underscore notation. |

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
