# HCMS — Architecture

**Last Updated:** 2026-11-27 (Synced with code: DataHandler pattern, Angular 19 standalone, no Key Vault, HL7 v2 removed, actual CI/CD)
**Stack:** Angular 19 · .NET 8 · EF Core 8 · SQL Server · Azure

> Auto-update this file when the architecture changes (new layers, new Azure services, new patterns).
> This file describes what is **in the code today**. Planned work lives in [phase-tracker.md](phase-tracker.md).

---

## 0. Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 19 (standalone components), Angular Material 19, Chart.js 4 (used directly, no wrapper), ngx-json-viewer |
| Backend | .NET 8 Web API, Clean Architecture (Domain / Application / Infrastructure / API) |
| Business Logic | DataHandler pattern — one interface + implementation per feature, validation + logic together (no MediatR) |
| Validation | `ValidationException` thrown from DataHandlers (FluentValidation types) → mapped to ProblemDetails by `ExceptionHandlerMiddleware` |
| ORM | Entity Framework Core 8 → SQL Server (local) / Azure SQL (prod) |
| Auth | JWT (HS256) issued by `TokenService`; BCrypt password hashing; refresh token **not yet implemented** ([issue #5](https://github.com/zaheerhbd/HCMS/issues/5)) |
| Documents | Azure Blob Storage (private `case-documents` container), 1-hour SAS URLs; Azurite locally |
| Secrets | `dotnet user-secrets` / `appsettings.Development.json` locally; **Azure App Service Application Settings** in prod (Key Vault was dropped — see [project-status.md](project-status.md) decision log) |
| FHIR | Firely .NET SDK (`Hl7.Fhir.R4` NuGet) |
| Logging | Serilog → Azure Application Insights |
| Testing | xUnit + FluentAssertions + EF Core InMemory + `WebApplicationFactory` (backend); Jasmine/Karma (frontend) |
| CI/CD | GitHub Actions → Azure App Service (API) + Azure Static Web Apps (Angular) |

---

## 0b. Repository Layout

```
HCMS/
├── CLAUDE.md                     ← project memory for Claude (rules, commands, phase, links)
├── README.md                     ← (empty — Phase 8 task)
├── .github/workflows/
│   ├── build-api.yml             ← restore → build → test → publish → deploy hcm-prod-api
│   └── build-angular.yml         ← install → lint → prod build → deploy Static Web App
├── .claude/
│   ├── settings.json             ← Claude Code hooks (test gate on commit, docs check on stop, phase index)
│   └── scripts/                  ← hook scripts
├── docs/                         ← requirements, tracker, status, changelog, phase reports, guides
│   ├── phases/phase-N.md         ← per-phase task checklists
│   └── testing-and-audit/        ← Phase 8 test docs and reports
├── prototype/                    ← static HTML/JS prototype (reference only — do not modify)
├── src/
│   ├── HCM.slnx
│   ├── HCM.Domain/               ← entities, enums (no dependencies)
│   ├── HCM.Application/          ← DataHandlers, DTOs, interfaces, per feature folder
│   ├── HCM.Infrastructure/       ← DbContext, configs, migrations, seed, services, background jobs
│   ├── HCM.API/                  ← controllers, middleware, Program.cs (DI root)
│   ├── HCM.Application.Tests/    ← 43 xUnit unit tests
│   └── HCM.API.Tests/            ← 26 xUnit integration tests
└── src-ng/hcm-web/               ← Angular 19 SPA
```

---

## 1. High-Level Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        GitHub Actions CI/CD                          │
│        push to main → build → test (API only) → deploy to prod       │
└────────────────────────┬────────────────────────────────────────────┘
                         │
        ┌────────────────┼──────────────────┐
        ▼                ▼                   ▼
┌──────────────────┐  ┌──────────────┐  ┌──────────────────┐
│ Azure Static     │  │ Azure App    │  │  Azure SQL        │
│ Web Apps         │─►│ Service      │─►│  Database         │
│ (Angular SPA)    │  │ hcm-prod-api │  │                   │
└──────────────────┘  └──────┬───────┘  └──────────────────┘
        HTTPS + JWT          │  secrets from App Service
                             │  Application Settings
              ┌──────────────┴───────────────┐
              ▼                              ▼
   ┌──────────────────┐            ┌──────────────────┐
   │  Azure Blob      │            │  Application     │
   │  Storage         │            │  Insights        │
   │  (case-documents │            │  (Serilog sink)  │
   │   private)       │            │                  │
   └──────────────────┘            └──────────────────┘
```

---

## 2. Backend — Clean Architecture

### Layer Map

```
HCM.Domain
  └── Entities (Patient, Case, CaseNote, CaseTask, Document, Appointment, Notification, AuditLog, FhirImportLog, ...)
  └── Enums (CaseStatus, Priority, RiskLevel, TaskStatus, AppointmentStatus, ...)
  └── No external dependencies

HCM.Application                       (one folder per feature: Auth, Patients, Cases, CaseNotes, Tasks, ...)
  └── [Feature]/DataHandlers/         I[Feature]DataHandler + [Feature]DataHandler (validation + business logic)
  └── [Feature]/DTOs/                 request + response DTOs
  └── Interfaces                      IApplicationDbContext, IBlobStorageService, IFhirMappingService, ITokenService, ...
  └── DependencyInjection.cs          services.AddScoped<IXDataHandler, XDataHandler>()
  └── Depends on: HCM.Domain only

HCM.Infrastructure
  └── Persistence/ApplicationDbContext   EF Core DbContext; SaveChanges override writes AuditLogs
  └── Persistence/Configurations         IEntityTypeConfiguration<T>
  └── Persistence/Seed/DataSeeder        roles, admin user, demo data (idempotent)
  └── Migrations/                        EF Core migrations
  └── Identity/TokenService              JWT generation
  └── Services/BlobStorageService        Azure.Storage.Blobs upload + SAS
  └── Services/FhirMappingService        Firely SDK mapping + bundle parsing
  └── BackgroundJobs/CaseNoteEditLockService       hourly: lock notes older than 24h
  └── BackgroundServices/NotificationOverdueService daily: overdue-task notifications
  └── Depends on: HCM.Application, HCM.Domain

HCM.API
  └── Controllers (thin — call the DataHandler, return the result)
  └── Middleware/ExceptionHandlerMiddleware   exceptions → ProblemDetails / OperationOutcome
  └── Program.cs (DI composition root, JWT, CORS, rate limiting, health checks, Swagger)
  └── Depends on: HCM.Application (interfaces), HCM.Infrastructure (DI registration only)
```

### Dependency Rule
```
Domain ← Application ← Infrastructure ← API
```
Infrastructure is only referenced at the DI composition root in `Program.cs`.

### Request Pipeline (per API call)

```
HTTP Request
  → ExceptionHandlerMiddleware (wraps everything below)
  → CORS ("Angular" policy)
  → Authentication (JWT Bearer) → Authorization ([Authorize(Roles=...)])
  → Rate Limiter ("login" policy on /api/auth/login)
  → Controller Action
    → I[Feature]DataHandler.MethodAsync(...)
        validate → throw ValidationException on bad input
        business logic → EF Core / IBlobStorageService / IFhirMappingService
    → ApplicationDbContext.SaveChangesAsync → builds AuditLog rows from ChangeTracker
  → HTTP Response
```

### Key NuGet Packages (in use)

| Package | Purpose |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | ORM |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth |
| `BCrypt.Net-Next` | Password hashing |
| `FluentValidation.DependencyInjectionExtensions` | `ValidationException` / validation types used by DataHandlers |
| `Serilog.AspNetCore` + `Serilog.Sinks.ApplicationInsights` | Structured logging |
| `Azure.Storage.Blobs` | Blob upload / SAS generation |
| `Hl7.Fhir.R4` (Firely SDK) | FHIR R4 serialization and validation |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | `/health` DB check |
| `Swashbuckle.AspNetCore` | Swagger UI |
| `xunit`, `FluentAssertions`, `Microsoft.EntityFrameworkCore.InMemory`, `Microsoft.AspNetCore.Mvc.Testing` | Tests |

> Unused `MediatR` and Key Vault (`Azure.Extensions.AspNetCore.Configuration.Secrets`, `Azure.Identity`) package references were removed on 2026-11-27.

---

## 3. Frontend — Angular Architecture

All components are **standalone** (no `NgModule`). Every feature area is lazy-loaded from `app.routes.ts` via `loadComponent` / `loadChildren` (feature `*.routes.ts` files).

### Folder Structure

```
src-ng/hcm-web/src/app/
  app.config.ts / app.routes.ts   # providers (HttpClient + interceptors), top-level lazy routes
  shell/app-shell/                # layout: sidenav, topbar, notification bell, router-outlet
  core/
    services/      AuthService, UserService, NotificationService, FhirService
    interceptors/  jwtInterceptor (Bearer header + 401 → refresh → retry), errorInterceptor
    guards/        authGuard, roleGuard
    components/    notification-bell
    models/        auth, patient, case, appointment, notification models
  shared/
    status-badge/      StatusBadgeComponent (status/priority/risk colors)
    confirm-dialog/    ConfirmDialogComponent
    directives/        HasRoleDirective   *hasRole="['Admin','Supervisor']"
  features/
    auth/          login
    dashboard/     coordinator / supervisor / admin dashboards (role-picked hub)
    patients/      list, detail, form
    cases/         list, detail, form, status history, notes, care team, tasks, documents
    tasks/         MyTasks
    appointments/  list, detail, form
    documents/     documents page (UI currently hidden — backend complete)
    reports/       cases-by-status, case-duration, tasks-by-assignee
    fhir/          FHIR explorer, FHIR import (single patient + bundle upload)
    admin/         audit-log (user-management UI not built yet)
    unauthorized/
```

### Key Angular Patterns

| Pattern | Implementation |
|---|---|
| State management | Service-level RxJS `BehaviorSubject`s (`AuthService.isAuthenticated$`, `NotificationService` unread count). (NgRx packages were removed on 2026-11-27 — never used.) |
| HTTP | Typed `HttpClient` services per feature |
| Forms | Reactive Forms throughout |
| Routing | Lazy `loadComponent` / `loadChildren`; `canActivate: [authGuard]`, `roleGuard` on restricted routes |
| UI Library | Angular Material 19 |
| Template syntax | Angular 19 built-in control flow (`@if` / `@for`) in newer components |
| Charts | Chart.js 4 directly; `ChangeDetectorRef.detectChanges()` before building charts inside `@if` blocks |
| JWT storage | `localStorage` (access + refresh token keys) |
| Notifications | 60-second polling of `/api/notifications/unread-count` |
| FHIR display | `ngx-json-viewer` collapsible JSON tree |
| Role-based UI | `HasRoleDirective` structural directive (in addition to route guards) |

---

## 4. Database Schema (Overview)

Full DDL is in [product-requirements.md](product-requirements.md) § 3.

```
Identity & Access     Users · Roles · UserRoles · RefreshTokens (table exists; refresh flow not wired)
Patient Management    Patients · PatientInsurance
Case Management       CaseTypes · Cases · CaseTags · CaseCaseTags · CaseStatusHistory · CaseNotes
Care Team             CareTeamMembers
Task Management       CaseTasks
Document Management   Documents
Appointments          Providers · Appointments
Notifications         Notifications
Audit                 AuditLogs  ← immutable, no FK to Users, no DELETE/UPDATE endpoint
FHIR Integration      FhirImportLogs
```

### Key Design Decisions
- PKs are sequential GUIDs (`NEWSEQUENTIALID()`) — avoids index hotspots. Patients and cases are navigated by MRN / case number in URLs.
- Soft deletes via `IsActive` / `IsDeleted` on Patients and Documents.
- `AuditLogs` has no FK to `Users` so log entries survive user deletion. Rows are built in `ApplicationDbContext.BuildAuditEntries()` for Patient, Case, CaseNote and User changes.
- `CaseNotes.IsEditable` is set to `false` by `CaseNoteEditLockService` (runs hourly) once a note is 24h old. *Note: there is no edit-note endpoint yet, so notes are currently append-only from creation.*

---

## 5. API Route Map (Summary)

Full endpoint table is in [product-requirements.md](product-requirements.md) § 4.

```
/api/auth/          → AuthController          login (refresh + logout are TODO stubs — issue #5)
/api/users/         → UsersController         Admin only — user CRUD, role assignment
/api/patients/      → PatientsController      CRUD, search, related cases/documents/appointments
/api/cases/         → CasesController         CRUD, status transitions, history
/api/cases/{caseNumber}/notes      → CaseNotesController   list, add (no edit endpoint)
/api/cases/{caseNumber}/team       → CareTeamController    list, add, remove member
/api/cases/{caseNumber}/tasks      → TasksController       CRUD, status transitions
/api/tasks/my                      → TasksController       current user's tasks
/api/cases/{caseNumber}/documents  → DocumentsController   list, upload, SAS download URL, soft delete
/api/appointments/  → AppointmentsController  CRUD, status, by patient MRN, providers
/api/dashboard/     → DashboardController     coordinator | supervisor | admin
/api/reports/       → ReportsController       cases-by-status | case-duration | tasks-by-assignee (?format=csv)
/api/audit/         → AuditController         Admin only — paginated, filterable
/api/notifications/ → NotificationsController list, unread-count, mark-read, mark-all-read
/fhir/R4/           → FhirController          metadata, Patient/Encounter/Appointment/Task/DocumentReference
                                              read + search, Patient/$everything, Patient import,
                                              POST / (transaction Bundle), POST /$import (bundle file upload)
/health             → Health check (database)
```

**Error formats:** `/api/` → RFC 7807 `ProblemDetails`; `/fhir/R4/` → FHIR `OperationOutcome`.

---

## 6. Authentication Flow

```
Login (implemented):
  POST /api/auth/login  [rate limited: 10 req / 60 s per IP]
    → validate credentials (BCrypt) → lockout after 5 failed attempts
    → TokenService issues JWT (HS256, ClockSkew = 0)
    → return { accessToken, refreshToken, expiresIn }
    → Angular stores tokens in localStorage

Authenticated request:
  jwtInterceptor attaches "Authorization: Bearer {accessToken}"
  → JwtBearer middleware validates signature + expiry → roles from claims

Token refresh / Logout (NOT YET IMPLEMENTED — issue #5):
  Angular side is ready: jwtInterceptor catches 401 → AuthService.refreshToken()
  → retries the request, logs out on refresh failure.
  API side: POST /api/auth/refresh and /api/auth/logout are // TODO stubs.
```

---

## 7. Document Upload Flow

```
Upload:  POST /api/cases/{caseNumber}/documents/upload (multipart/form-data)
  → DocumentDataHandler: validate MIME type (pdf, png, jpg, docx) and size ≤ 20 MB
  → IBlobStorageService.UploadAsync → blob "{caseId}/{documentId}/{fileName}"
  → INSERT Documents (metadata only — no blob URL stored)

Download: GET /api/cases/{caseNumber}/documents/{documentId}/download-url
  → IBlobStorageService.GenerateSasUrl(blobName, expiry = 1h)
  → return { url, expiresAt }   (SAS URL is NEVER logged)
```

---

## 8. FHIR Integration Architecture

```
HCM.Application
  └── IFhirMappingService            entity ↔ FHIR mapping + bundle parsing
  └── IFhirDataHandler / FhirDataHandler
        search/read, $everything, single-patient import,
        ImportTransactionBundleAsync (atomic: one DB transaction)
  └── Parsed* records (ParsedFhirPatient, ParsedFhirEncounter, ...) — no Firely types leak out of Infrastructure

HCM.Infrastructure
  └── FhirMappingService (Firely SDK)

HCM.API
  └── FhirController (/fhir/R4/) — Content-Type: application/fhir+json

Resource mapping:
  Patient → Patient · Case → Encounter · Appointment → Appointment
  CaseTask → Task · Document → DocumentReference

Transaction Bundle import (POST /fhir/R4/ or POST /fhir/R4/$import):
  BEGIN TRANSACTION
    Patient → Encounter (Case) → Appointment → Task   (urn:uuid references resolved in order)
    Encounter extension "case-assigned-to" (username) → assigned coordinator
    FhirImportLogs per entry + one AuditLog (FhirBundleImport)
  COMMIT  — or ROLLBACK everything on any failure → OperationOutcome
  Returns a transaction-response Bundle
```

HL7 v2 (ADT/ORU via NHapi) was built and then **removed** on 2026-07-05; bundle file upload replaced it.

---

## 9. Deployment Environments

| Environment | Backend | Frontend | Database | Status |
|---|---|---|---|---|
| `dev` | `https://localhost:7198` / `http://localhost:5144` | `http://localhost:4200` (`ng serve`) | SQL Server LocalDB | In use |
| `prod` | `hcm-prod-api` (App Service) | Azure Static Web App | Azure SQL | In use — deployed on every push to `main` |
| `staging` | — | — | — | Dropped by decision (2026-06-24) — deploy straight from `main` to prod |

### Azure Resource Names

```
App Service:      hcm-{env}-api
Static Web App:   hcm-{env}-web
SQL Server / DB:  hcm-{env}-sqlsrv / hcm-{env}-db
Storage Account:  hcm{env}st         (no hyphens — storage account naming restriction)
App Insights:     hcm-{env}-ai
```

Secrets in prod are App Service Application Settings using double-underscore keys (`Jwt__Secret`, `ConnectionStrings__DefaultConnection`).

---

## 10. CI/CD Pipeline (as implemented)

```
Trigger: push / PR to main

build-api.yml:
  dotnet restore → build (Release) → dotnet test → publish → deploy to hcm-prod-api

build-angular.yml:
  npm install → lint → ng build --configuration production → deploy to Static Web App
```

**Not yet in place (Phase 8):** Angular unit tests in CI, a manual approval gate before prod, smoke tests, coverage and dependency scanning. (No staging environment by design — see decision log in [project-status.md](project-status.md).)

---

## 11. Cross-Cutting Concerns

| Concern | Implementation |
|---|---|
| Logging | Serilog with Application Insights sink; entity IDs only, no PHI |
| Global exception handling | `ExceptionHandlerMiddleware` → ProblemDetails (REST) or OperationOutcome (FHIR) |
| Audit trail | `ApplicationDbContext` SaveChanges override writes `AuditLogs` with before/after JSON |
| Rate limiting | ASP.NET Core rate limiter, fixed window "login" policy — 10 req / 60 s per IP |
| Health checks | `/health` — custom `DatabaseHealthCheck` |
| CORS | "Angular" policy; origins from `Cors:AllowedOrigins` |
| Security headers | Not yet added (Phase 8 task) |
| Background jobs | `CaseNoteEditLockService` (hourly), `NotificationOverdueService` (daily) |
