# HCMS — Changelog

> **How to update:** Add a new entry at the top of the relevant phase section on every merge to `main`.
> Format: `- [YYYY-MM-DD] Short description of what changed and why.`
> Link to PRs where relevant. Do not delete old entries.

---

## Unreleased

_Changes merged to `main` but not yet in a phase deliverable._

- [2026-06-27] Architecture refactor: Removed MediatR pattern and replaced with DataHandler pattern. Created `AuthDataHandler` (implements `IAuthDataHandler`) combining command logic, validation, and business logic in a single class. Updated `AuthController` to inject and call `IAuthDataHandler` directly. Removed `Commands/`, `Handlers/`, `Validators/`, and MediatR pipeline registration. Registered handler as scoped dependency: `services.AddScoped<IAuthDataHandler, AuthDataHandler>()`. Updated `CLAUDE.md` with new DataHandler coding standard: handlers live in `Application/[Feature]/DataHandlers/`, use interfaces for DI and testing, validate inline using FluentValidation exceptions.

---

## Phase 1 — Foundation & Infrastructure

_Deliverable: Deployable skeleton with auth working end-to-end in Azure._
_Target: In Progress_

- [2026-06-24] Scaffolded .NET 8 Clean Architecture solution (`HCM.Domain`, `HCM.Application`, `HCM.Infrastructure`, `HCM.API`) with project references wired per layer rules.
- [2026-06-24] Added EF Core + SQL Server provider; `ApplicationDbContext` with `Users`, `Roles`, `UserRoles`, `RefreshTokens` entities and EF Fluent configurations; created `InitialCreate` migration.
- [2026-06-24] Implemented JWT authentication: `AuthController` (`/login`, `/refresh`, `/logout`), MediatR commands/handlers, refresh token rotation, account lockout after 5 failed attempts.
- [2026-06-24] Added MediatR pipeline: `ValidationBehaviour` running FluentValidation on every command before handler execution.
- [2026-06-24] Added global exception handler middleware returning RFC 7807 `ProblemDetails`; Serilog request logging; `/health` endpoint with DB connectivity check; fixed-window rate limiting on `/api/auth/login` (10 req/min per IP).
- [2026-06-24] Seeded database with all 5 RBAC roles and default Admin user on startup in Development.
- [2026-06-24] Scaffolded Angular 19 standalone project with Angular Material; implemented `AuthService`, `JwtInterceptor` (with 401-refresh retry), `ErrorInterceptor`, `authGuard`, `roleGuard`, `LoginComponent`, `AppShellComponent` (sidebar nav + top bar).
- [2026-06-24] Created lazy-loaded route stubs for all Phase 2+ feature modules (Dashboard, Patients, Cases, Tasks, Documents, Appointments, Reports, Admin).
- [2026-06-24] Created GitHub Actions `build-api.yml` (restore → build → test → publish → deploy to Azure App Service) and `build-angular.yml` (npm ci → lint → build prod → deploy to Azure SWA). Added `dependabot.yml` for NuGet, npm, and GitHub Actions updates.
- [2026-06-24] Fixed runtime crash on `dotnet run`: removed `<InvariantGlobalization>true</InvariantGlobalization>` from `HCM.API.csproj` (default template setting incompatible with `Microsoft.Data.SqlClient`). Set JWT secret via `dotnet user-secrets`. Confirmed API starts cleanly: LocalDB created, `InitialCreate` migration applied, all 5 roles and admin user seeded successfully.
- [2026-06-24] Decision: single prod environment only (no staging). Updated `build-api.yml` deploy target to `hcm-prod-api`. Updated `project-status.md` blockers section with full `hcm-prod-*` az CLI provisioning commands and GitHub secrets checklist. Removed all `hcm-staging-*` references.
- [2026-06-24] Added `docs/azure-provisioning-guide.html` — self-contained portal UI walkthrough for all 9 Azure resources (resource group, SQL Server + DB, Key Vault, App Insights, App Service Plan, App Service, Static Web App, Managed Identity, GitHub secrets) with copy-paste values and an interactive Phase 1 Definition of Done checklist.
- [2026-06-24] Added `docs/angular-architecture-guide.html` — 17-section Angular architecture reference (lazy loading, standalone components, NgRx, interceptors, guards, change detection, interview Q&A) for portfolio prep.
- [2026-06-24] Fixed CI: downgraded all NgRx packages from `^21.1.1` to `^19.0.0` to match Angular 19; changed `npm ci` to `npm install --legacy-peer-deps` to handle peer dependency conflicts; removed Angular test step (no spec files exist yet). All three changes were needed to get `build-angular.yml` green.
- [2026-06-24] Fixed Angular prod environment: `environment.prod.ts` `apiUrl` was `/api` (relative) — changed to full Azure App Service URL so SPA can reach the API from SWA domain.
- [2026-06-24] Fixed CORS: added `https://jolly-bay-072046c10.7.azurestaticapps.net` (SWA domain) to `Cors:AllowedOrigins` in `appsettings.json`.
- [2026-06-24] Changed `DataSeeder.SeedAsync` and Swagger middleware to run in all environments (not just Development) so prod DB is seeded on first startup.
- [2026-06-26] Fixed Angular prod build: `angular.json` production configuration was missing `fileReplacements`, so `--configuration production` never swapped `environment.ts` for `environment.prod.ts`. Every SWA deploy was bundling `localhost:5144` as the API URL. Added `fileReplacements` block to the production config.
- [2026-06-26] Fixed login 500 error: `Jwt__Secret` App Service setting was only 16 characters (128 bits); HS256 requires ≥ 32 characters (256 bits). Updated the secret to a 48-byte Base64 value (384 bits).
- [2026-06-25] Decision: skip Azure Key Vault — Key Vault was provisioned in West Europe under a different Azure AD tenant (`adf10e2b-...`) causing `AKV10032 Invalid issuer` crash on every startup; App Service runs under tenant `397f0f29-...`. Removed `AddAzureKeyVault` block from `Program.cs` and `KeyVault` section from `appsettings.json`. Secrets (`Jwt__Secret`, `ConnectionStrings__DefaultConnection`) supplied directly via App Service Application Settings with double-underscore notation.

---

## Phase 2 — Patient & Case Core

_Deliverable: Full case lifecycle — create patient → open case → transition status → add care team._
_Target: In Progress_

- [2026-06-29] Created 10 domain entities for patient/case lifecycle: `Patient`, `PatientInsurance`, `CaseType`, `Case`, `CaseStatus` (constants), `CaseStatusHistory`, `CaseTag`, `CaseCaseTag`, `CareTeamMember`, `CaseNote`. All entities follow soft-delete pattern (IsActive/IsDeleted flags) for HIPAA compliance and audit integrity.
- [2026-06-29] Created 9 EF Core entity type configurations with fluent mappings, cascade delete rules, unique indexes (MRN, CaseNumber, CaseType.Name, CaseTag.Name), and required properties. Configurations auto-discovered by `ApplicationDbContext.OnModelCreating()` via `ApplyConfigurationsFromAssembly()`.
- [2026-06-29] Applied EF Core migration `AddPatientCaseCaseManagement`: creates 9 new tables (Patients, PatientInsurances, CaseTypes, Cases, CaseStatusHistories, CaseTags, CaseCaseTags, CareTeamMembers, CaseNotes) with all FKs, unique constraints, and indexes. Migration applied successfully to LocalDB.
- [2026-06-29] Created 16 DTOs for API contracts: `PatientDto`, `CreatePatientDto`, `UpdatePatientDto`, `PatientInsuranceDto`, `PatientSearchDto` (paginated response); `CaseDto`, `CreateCaseDto`, `UpdateCaseDto`, `CaseStatusChangeDto`, `CaseStatusHistoryDto`, `CaseListItemDto`; `CareTeamMemberDto`, `AddCareTeamMemberDto`; `CaseNoteDto`, `CreateCaseNoteDto`. DTOs include all required fields for frontend binding and support pagination, status transitions, and audit properties.
- [2026-06-29] Implemented 4 DataHandlers with full business logic and validation: `PatientDataHandler` (MRN generation `MRN-{YYYY}-{NNNNN}`, search by name/DOB/MRN/insurance ID with pagination), `CaseDataHandler` (case number generation `CASE-{YYYY}-{NNNNN}`, status state machine with 5 statuses and transition validation), `CareTeamDataHandler` (add/remove members with soft deletion preserving history), `CaseNoteDataHandler` (append-only notes, soft delete, 5000-char limit). All handlers registered in `DependencyInjection.cs`.
- [2026-06-29] Created 4 Controllers: `PatientsController` (CRUD + search), `CasesController` (CRUD + status transitions), `CareTeamController` (member management), `CaseNotesController` (note management). All endpoints include role-based authorization (RBAC) enforcing: CareCoordinator+ for create/modify, Supervisor+ for close/reassign, Clinician for notes. API builds cleanly, ready for testing.
- [2026-06-29] Added `AuditLog` domain entity and EF configuration. Added `AuditInterceptor` via `SaveChangesAsync` override in `ApplicationDbContext` — automatically captures Created/Updated/Deleted events for `Patient`, `Case`, `CaseNote`, and `User` entities with JSON old/new value diffs. Injected `IHttpContextAccessor` for current user attribution. Applied migration `AddAuditLogs`. Audit entries have no FK to Users (survives user deletion, per hard rule).
- [2026-06-29] Seeded default CaseTypes (Chronic Disease, Post-Surgery, Mental Health, Preventive, Behavioral) and 10 default CaseTags (Urgent, High Priority, Pediatric, Geriatric, etc.) in `DataSeeder.SeedAsync`. Seeds are idempotent — will not duplicate on restart.
- [2026-06-29] Implemented `UsersController` (Admin only) with `IUserDataHandler`/`UserDataHandler`: list users with search/pagination, get by ID, create with role assignment, update name/email, assign roles (replace set), activate/deactivate. Registered as scoped dependency in `Application/DependencyInjection.cs`.
- [2026-06-29] Implemented `CaseNoteEditLockService` (IHostedService): runs every hour, sets `IsEditable = false` on CaseNotes older than 24 hours via `ExecuteUpdateAsync`. Registered as a hosted service in `Infrastructure/DependencyInjection.cs`. Errors logged; cancellation handled cleanly.
- [2026-06-29] Implemented Angular PatientsModule: `PatientListComponent` (server-side paginated table with debounced search), `PatientDetailComponent` (demographics, insurance accordion, cases tab, documents placeholder, breadcrumb), `PatientFormComponent` (reactive form create/edit with datepicker). `PatientService` covers all HTTP calls.
- [2026-06-29] Implemented Angular CasesModule: `CaseListComponent` (status filter + paginated table), `CaseDetailComponent` (header card with inline status transition, tabs: Notes/Care Team/History/Tasks placeholder/Documents placeholder), `CaseFormComponent` (patient picker + case type select), `CaseStatusHistoryComponent` (timeline), `CareTeamComponent` (add/remove with ConfirmDialog), `CaseNotesComponent` (post notes + locked badge after 24h). `CaseService` covers all HTTP calls.
- [2026-06-29] Created shared standalone components: `StatusBadgeComponent` (color-coded mat-chip per case status), `HasRoleDirective` (structural directive `*appHasRole` hiding elements by role), `ConfirmDialogComponent` (reusable Material Dialog with title/message/confirm/cancel labels). Angular build: 0 errors, all lazy chunks generated.
- [2026-06-29] Replaced GUID route parameters with business keys to remove IDOR exposure: `PatientsController`/`CasesController`/`CareTeamController`/`CaseNotesController` now route and look up by MRN/CaseNumber instead of `Guid id`. Updated `IPatientDataHandler`, `ICaseDataHandler`, `ICareTeamDataHandler`, `ICaseNoteDataHandler` and their implementations to resolve entities by MRN/CaseNumber before any FK operations. Updated Angular routes (`:mrn`, `:caseNumber`), `PatientService`/`CaseService`, and all Patients/Cases components (list, detail, form, notes, care team) to navigate and call the API using MRN/CaseNumber.
- [2026-06-29] Seeded 5 demo users on startup (idempotent): `sarah.jones`, `michael.lee` (CareCoordinator), `dr.patel`, `dr.carter` (Clinician), `lisa.chen` (Supervisor) — all use password `Demo@123!`. Added `GET /api/users/assignable` endpoint (all authenticated roles) returning active users for dropdowns. Updated `CareTeamComponent` to replace the raw GUID text input with a `mat-select` dropdown populated from the assignable users endpoint, showing name and role.
- [2026-06-29] Fixed cases list screen showing no results: `GET /api/cases` endpoint was missing from `CasesController`. Added `GetAllCasesAsync` to `ICaseDataHandler` and `CaseDataHandler` (paginated, optional status filter, ordered by `OpenedAt` desc), and wired the `[HttpGet]` action on the controller returning `{ items, totalCount, page, pageSize }`.
- [2026-06-29] Added `NEWSEQUENTIALID()` as the SQL Server default value generator on `Patient`, `Case`, `CaseNote`, `CareTeamMember`, and `CaseStatusHistory` primary keys (migration `UseSequentialGuids`), and removed manual `Guid.NewGuid()` assignment from the corresponding DataHandlers — prevents B-tree index fragmentation from random GUID inserts. `CaseDataHandler.CreateCaseAsync` now saves the `Case` first to obtain its DB-generated Id before creating the initial `CaseStatusHistory` row.

---

## Phase 3 — Documents, Tasks & Notes

_Deliverable: Azure Blob upload + task management._
_Target: Not Started_

_(no entries yet)_

---

## Phase 4 — Appointments & Scheduling

_Deliverable: Calendar booking with conflict detection._
_Target: Not Started_

_(no entries yet)_

---

## Phase 5 — FHIR/HL7 Integration

_Deliverable: FHIR R4 API facade, Bundle export/import._
_Target: Not Started_

_(no entries yet)_

---

## Phase 6 — Dashboards & Reports

_Deliverable: All role dashboards live + CSV export._
_Target: Not Started_

_(no entries yet)_

---

## Phase 7 — Notifications & Audit UI

_Deliverable: Notification bell + admin audit log viewer._
_Target: Not Started_

_(no entries yet)_

---

## Phase 8 — Testing, Security & Polish

_Deliverable: Production-quality build — tests, security hardening, prod deploy._
_Target: Not Started_

_(no entries yet)_

---

## Pre-Phase (Project Setup)

- [2026-06-22] Created product-requirements.md — full FR/NFR list, DB schema, API contracts, Azure architecture.
- [2026-06-21] Created phase-tracker.md — 8-phase task checklist with definitions of done.
- [2026-06-23] Created CLAUDE.md, architecture.md, changelog.md, project-status.md, feature-reference.md.
- [2026-06-23] Created .env.example and .env (gitignored) for local dev configuration.
- [2026-06-23] Initial commit — repository structure, prototype (static SPA), docs, gitignore.
