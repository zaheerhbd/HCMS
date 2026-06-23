# HCMS — Architecture

**Last Updated:** 2026-06-23
**Stack:** Angular 17 · .NET 8 · EF Core · SQL Server · Azure

> Auto-update this file when the architecture changes (new layers, new Azure services, new patterns).

---

## 0. Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 17+, Angular Material (MDC), NgRx, Chart.js (ng2-charts), ngx-json-viewer |
| Backend | .NET 8 Web API, Clean Architecture (Domain / Application / Infrastructure / API) |
| CQRS | MediatR + FluentValidation pipeline behaviors |
| ORM | Entity Framework Core → SQL Server / Azure SQL |
| Auth | JWT (HS256) + Refresh Token rotation — no Azure AD |
| Documents | Azure Blob Storage (private container), 1-hour SAS URLs |
| Secrets | Azure Key Vault (Managed Identity in Azure; `az login` locally) |
| FHIR | Firely .NET SDK (`Hl7.Fhir.R4` NuGet) |
| Logging | Serilog → Azure Application Insights |
| Email | SendGrid (Phase 4+, not yet wired) |
| CI/CD | GitHub Actions → Azure App Service (.NET API) + Static Web Apps (Angular) |

---

## 0b. Repository Layout

```
HCMS/
├── CLAUDE.md                   ← project memory for Claude (rules, commands, phase, links)
├── .env.example                ← committed; template for all env vars
├── .env                        ← NOT committed; local dev values
├── .gitignore
├── README.md
│
├── docs/
│   ├── product-requirements.md ← full FR/NFR list, DB schema, API contracts (source of truth)
│   ├── phase-tracker.md        ← 8-phase task checklist; update as tasks complete
│   ├── architecture.md         ← you are here
│   ├── changelog.md            ← chronological change log; add entry on every merge to main
│   ├── project-status.md       ← current phase, what's done, what's next (update weekly)
│   └── feature-reference.md    ← module-by-module feature index with FR links
│
├── .claude/
│   └── settings.json           ← Claude Code hooks (Stop hook for doc auto-updates)
│
├── prototype/                  ← static HTML/JS prototype (reference only — do not modify)
│   ├── index.html
│   ├── app.js
│   ├── data.js
│   └── style.css
│
└── html-docs/                  ← rendered HTML versions of docs (do not edit directly)
```

> **Source projects not yet scaffolded** (Phase 1):
> `src/HCM.Domain/`, `src/HCM.Application/`, `src/HCM.Infrastructure/`, `src/HCM.API/`, `src/hcm-web/`

---

## 1. High-Level Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        GitHub Actions CI/CD                          │
│  Build → Test → Publish → Deploy (dev → staging → prod)             │
└────────────────────────┬────────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        ▼                ▼                 ▼
┌──────────────────┐  ┌─────────────┐  ┌──────────────────┐
│ Azure Static     │  │ Azure App   │  │  Azure SQL        │
│ Web Apps         │  │ Service     │  │  Database         │
│ (Angular SPA)    │  │ (.NET 8 API)│  │  (S2 / LocalDB)   │
└────────┬─────────┘  └──────┬──────┘  └──────────────────┘
         │ HTTPS             │ HTTPS             ▲
         │                   │ (VNET             │
         │                   │  Integration)     │ EF Core
         │                   ▼                   │
         │           ┌──────────────────┐        │
         │           │  Azure Key Vault  │        │
         └──────────►│  - JWT secret     │        │
                     │  - SQL conn str   │────────┘
                     │  - Blob conn str  │
                     └──────────────────┘
                              │
              ┌───────────────┼────────────────┐
              ▼               ▼                 ▼
   ┌──────────────────┐  ┌──────────────┐  ┌───────────────┐
   │  Azure Blob      │  │  Application │  │  Azure         │
   │  Storage         │  │  Insights    │  │  Monitor       │
   │  (case-documents │  │  (Serilog)   │  │  (alerts)      │
   │   private)       │  │              │  │               │
   └──────────────────┘  └──────────────┘  └───────────────┘
```

---

## 2. Backend — Clean Architecture

### Layer Map

```
HCM.Domain
  └── Entities (Patient, Case, CaseNote, CaseTask, Document, Appointment, ...)
  └── Enums (CaseStatus, Priority, RiskLevel, AppointmentType, ...)
  └── Value Objects
  └── Domain Events
  └── No external dependencies

HCM.Application
  └── Commands / Queries (MediatR IRequest<T>)
  └── Command / Query Handlers
  └── DTOs (request + response)
  └── FluentValidation Validators (registered as MediatR pipeline behaviors)
  └── Interfaces (IRepository<T>, IBlobStorageService, IFhirMappingService, IEmailService, ...)
  └── Depends on: HCM.Domain only

HCM.Infrastructure
  └── ApplicationDbContext (EF Core DbContext)
  └── Entity Configurations (IEntityTypeConfiguration<T>)
  └── EF Core Migrations
  └── Repository implementations
  └── BlobStorageService (Azure.Storage.Blobs)
  └── FhirMappingService (Hl7.Fhir.R4 / Firely SDK)
  └── EmailService (SendGrid — Phase 4)
  └── Key Vault integration (DefaultAzureCredential)
  └── Depends on: HCM.Application, HCM.Domain

HCM.API
  └── Controllers (thin — dispatch only, no business logic)
  └── Program.cs / DI composition root
  └── Middleware (GlobalExceptionHandler, AuditMiddleware, RateLimiting)
  └── Filters (JWT authorization)
  └── Health checks (/health)
  └── Depends on: HCM.Application (interfaces), HCM.Infrastructure (DI registration only)
```

### Dependency Rule
```
Domain ← Application ← Infrastructure ← API
```
Nothing points inward against this direction. Infrastructure is only referenced at the DI composition root in `Program.cs`.

### Request Pipeline (per API call)

```
HTTP Request
  → Kestrel
  → Authentication Middleware (JWT Bearer)
  → Rate Limiting Middleware (auth endpoints only)
  → Audit Middleware (captures user identity + IP)
  → Controller Action
    → MediatR.Send(Command/Query)
      → FluentValidation Pipeline Behavior (validates DTO)
      → Logging Pipeline Behavior
      → Command/Query Handler (business logic)
        → EF Core / IBlobStorageService / etc.
  → Global Exception Handler (maps to ProblemDetails or OperationOutcome)
  → HTTP Response
```

### Key NuGet Packages

| Package | Purpose |
|---|---|
| `MediatR` | CQRS dispatcher |
| `FluentValidation.AspNetCore` | Request DTO validation |
| `Microsoft.EntityFrameworkCore.SqlServer` | ORM |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth |
| `Serilog.AspNetCore` + `Serilog.Sinks.ApplicationInsights` | Structured logging |
| `Azure.Storage.Blobs` | Blob upload / SAS generation |
| `Azure.Extensions.AspNetCore.Configuration.Secrets` | Key Vault config provider |
| `Azure.Identity` | DefaultAzureCredential |
| `Hl7.Fhir.R4` (Firely SDK) | FHIR R4 serialization and validation |
| `Microsoft.AspNetCore.RateLimiting` | Rate limiting (auth endpoints) |
| `CsvHelper` | CSV export (Phase 6) |
| `xunit` + `Moq` + `Microsoft.AspNetCore.Mvc.Testing` | Testing (Phase 8) |

---

## 3. Frontend — Angular Architecture

### Module Structure

```
src/app/
  core/                   # Singleton services, guards, interceptors (imported once in AppModule)
    services/
      AuthService         # login, logout, token refresh, role checks
      UserService         # current user profile
      NotificationService # unread count, polling
    interceptors/
      JwtInterceptor      # attach Bearer token
      ErrorInterceptor    # map API errors → user-facing toasts
      LoadingInterceptor  # global spinner
    guards/
      AuthGuard           # redirect to login if unauthenticated
      RoleGuard           # redirect if role not permitted
    models/               # shared TypeScript interfaces

  shared/                 # Reusable dumb components (imported in feature modules)
    components/
      StatusBadgeComponent      # color-coded badge for status/priority/risk
      ConfirmDialogComponent    # reusable confirm modal
      FileUploadComponent       # drag-drop + progress bar
      DataTableComponent        # paginated table wrapper (Angular CDK)
      PageHeaderComponent       # breadcrumb + title + action buttons
    pipes/
      DateFormatPipe            # YYYY-MM-DD → "Jun 23, 2026"
      TruncatePipe
      EnumLabelPipe
    directives/
      HasRoleDirective          # *hasRole="['Admin','Supervisor']"

  features/               # Lazy-loaded feature modules (one per domain area)
    auth/                 # Login, ChangePassword
    dashboard/            # CoordinatorDashboard, SupervisorDashboard, AdminDashboard
    patients/             # PatientList, PatientDetail, PatientForm
    cases/                # CaseList, CaseDetail, CaseForm, CaseStatusHistory
    case-notes/           # NoteList, NoteForm (embedded in CaseDetail)
    tasks/                # TaskList, MyTasks, TaskForm
    documents/            # DocumentList, DocumentUpload (embedded in CaseDetail/PatientDetail)
    appointments/         # AppointmentList, AppointmentForm, ProviderAvailability
    reports/              # CasesByStatus, CaseDuration, TasksByAssignee
    notifications/        # NotificationBell, NotificationList
    admin/                # UserList, UserForm, RoleAssignment, AuditLog
    fhir/                 # FhirExplorer, FhirImportComponent, FhirService
```

### Key Angular Patterns

| Pattern | Implementation |
|---|---|
| State management | NgRx Store — auth state + notifications; component RxJS for local state |
| HTTP | `HttpClient` typed services; `catchError` to global error handler |
| Forms | Reactive Forms; custom validators for MRN, date ranges, phone |
| Routing | Lazy-loaded feature modules; `canActivate: [AuthGuard, RoleGuard]` |
| UI Library | Angular Material (MDC) |
| Tables | Angular Material paginator + CDK virtual scroll for large lists |
| Charts | Chart.js via `ng2-charts` |
| File upload | Custom `FileUploadComponent` with `FormData` + `reportProgress` |
| JWT storage | `localStorage` (default); `sessionStorage` opt-in for sensitive roles |
| FHIR display | `ngx-json-viewer` for collapsible JSON tree |
| Role-based UI | `HasRoleDirective` structural directive |

### Angular Environment Files

```
src/environments/
  environment.ts           # development defaults (localhost API)
  environment.prod.ts      # production (Azure URLs, no debug tools)
```

---

## 4. Database Schema (Overview)

Full DDL is in [product-requirements.md](product-requirements.md) § 3.

### Schema Groups

```
Identity & Access
  Users · Roles · UserRoles · RefreshTokens

Patient Management
  Patients · PatientInsurance

Case Management
  CaseTypes · Cases · CaseTags · CaseCaseTags · CaseStatusHistory · CaseNotes

Care Team
  CareTeamMembers

Task Management
  CaseTasks

Document Management
  Documents

Appointments
  Providers · Appointments

Notifications
  Notifications

Audit
  AuditLogs  ← immutable, no FK to Users, no DELETE endpoint

FHIR Integration
  FhirImportLogs
```

### Key Design Decisions

- All PKs are `UNIQUEIDENTIFIER` using `NEWSEQUENTIALID()` — avoids hotspots, client-generatable.
- Soft deletes via `IsActive` / `IsDeleted` flags on Patients and Documents — preserves audit integrity.
- `AuditLogs` deliberately has no FK to `Users` so log entries survive user deletion.
- `CaseNotes.IsEditable` is set to `0` by a background `IHostedService` 24 hours after creation.
- `RefreshTokens.RevokedAt` implements token rotation — old tokens are revoked on each refresh.

---

## 5. API Route Map (Summary)

Full endpoint table is in [product-requirements.md](product-requirements.md) § 4.

```
/api/auth/          → AuthController         (login, refresh, logout, change-password)
/api/users/         → UsersController        (Admin only — user CRUD, role assignment)
/api/patients/      → PatientsController     (CRUD, search, cases/documents/appointments)
/api/cases/         → CasesController        (CRUD, status, notes, tasks, documents, team)
/api/tasks/         → TasksController        (my tasks, CRUD, status transitions)
/api/documents/     → DocumentsController    (upload, SAS download URL, soft-delete)
/api/appointments/  → AppointmentsController (schedule, status, provider availability)
/api/dashboard/     → DashboardController    (coordinator, supervisor, admin views)
/api/reports/       → ReportsController      (cases-by-status, case-duration, tasks-by-assignee)
/api/audit/         → AuditController        (Admin only — paginated, filterable)
/api/notifications/ → NotificationsController (list, unread-count, mark-read)

/fhir/R4/           → FhirController         (FHIR R4 facade — Patient, Encounter, Appointment, Task, DocumentReference)
/api/hl7/           → Hl7Controller          (stretch — ADT^A01, ORU^R01)

/health             → Health check (DB + Blob + Key Vault)
```

**Error formats:**
- `/api/` routes → RFC 7807 `ProblemDetails`
- `/fhir/R4/` routes → FHIR `OperationOutcome`

---

## 6. Authentication Flow

```
Login:
  POST /api/auth/login
    → validate credentials → check lockout (5 failed = lock)
    → generate JWT (60 min) + refresh token (30 days)
    → store refresh token hash in RefreshTokens table
    → return { accessToken, refreshToken, expiresIn }

Authenticated request:
  JwtInterceptor attaches "Authorization: Bearer {accessToken}"
  → JwtBearerMiddleware validates signature + expiry
  → Claims principal available in controllers

Token refresh (401 from any API call):
  ErrorInterceptor intercepts 401
  → POST /api/auth/refresh with refreshToken
  → old token revoked (RevokedAt set) → new pair issued
  → original request retried with new token

Logout:
  POST /api/auth/logout
  → RefreshToken.RevokedAt = now (server-side revocation)
  → Angular clears localStorage
```

---

## 7. Document Upload Flow

```
Frontend: FileUploadComponent
  → POST /api/documents/upload (multipart/form-data)
     fields: file, caseId, patientId, category, description

API: DocumentsController
  → validate MIME type (pdf, png, jpg, docx only)
  → validate size ≤ 20 MB
  → IBlobStorageService.UploadAsync(stream, blobName, containerName)
     blobName = {caseId}/{documentId}/{fileName}
  → INSERT Documents (metadata only — no blob URL stored)
  → AuditLog: action=Create, entityType=Document

Download:
  GET /api/documents/{id}/download-url
  → IBlobStorageService.GenerateSasUrl(blobName, expiry=1h)
  → return { url, expiresAt }
  → AuditLog: action=Download
  (SAS URL itself is NEVER logged)
```

---

## 8. FHIR Integration Architecture

```
HCM.Application
  └── IFhirMappingService
        MapPatientToFhir(Patient) → Hl7.Fhir.Model.Patient
        MapFhirToPatient(FhirPatient) → PatientEntity
        MapCaseToEncounter(Case) → Encounter
        MapAppointmentToFhirAppointment(Appointment) → FhirAppointment
        MapTaskToFhirTask(CaseTask) → FhirTask
        MapDocumentToDocumentReference(Document) → DocumentReference

HCM.Infrastructure
  └── FhirMappingService (implements above interface)
        Uses Firely SDK for serialization + validation

HCM.API
  └── FhirController (/fhir/R4/)
        Injects IFhirMappingService (via Application layer)
        Returns Content-Type: application/fhir+json

FHIR Resource Mapping:
  Patient    → Hl7.Fhir.Model.Patient
  Case       → Encounter   (Draft/Open → planned, InProgress → in-progress, Closed → finished)
  Appointment→ Appointment
  CaseTask   → Task
  Document   → DocumentReference

Patient/$everything Bundle:
  Patient + all Encounters + all Appointments + all Tasks + all DocumentReferences
  type: collection
```

---

## 9. Deployment Environments

| Environment | Backend | Frontend | Database | Purpose |
|---|---|---|---|---|
| `dev` | `localhost:7001` | `localhost:4200` | SQL Server LocalDB / Docker | Local development |
| `staging` | `hcm-staging-api.azurewebsites.net` | Azure SWA staging slot | Azure SQL Basic | Integration testing, demos |
| `prod` | `hcm-prod-api.azurewebsites.net` | Azure SWA production | Azure SQL S2 | Portfolio demo |

### Azure Resource Names

```
Resource Group:   rg-hcm-{env}
App Service Plan: asp-hcm-{env}
App Service:      hcm-{env}-api
Static Web App:   hcm-{env}-web
SQL Server:       hcm-{env}-sqlsrv
SQL Database:     hcm-{env}-db
Storage Account:  hcm{env}st         (no hyphens — storage account name restriction)
Key Vault:        hcm-{env}-kv
App Insights:     hcm-{env}-ai
Log Analytics:    hcm-{env}-law
```

---

## 10. CI/CD Pipeline

```
Trigger: push to main / PR to main

Jobs (parallel):
  build-api:
    dotnet restore → build → test → publish → upload artifact

  build-angular:
    npm ci → ng lint → ng test → ng build --configuration production → upload artifact

deploy-staging (needs both build jobs):
  Deploy API artifact → hcm-staging-api (App Service)
  Deploy Angular artifact → Static Web Apps staging slot
  Run smoke tests (curl /health, curl /api/auth/login with bad creds → 401)

deploy-prod (needs deploy-staging, manual approval gate):
  Deploy API → hcm-prod-api
  Deploy Angular → Static Web Apps production
```

---

## 11. Cross-Cutting Concerns

| Concern | Implementation |
|---|---|
| Logging | Serilog with Application Insights sink; structured properties only; no PHI |
| Global exception handling | `GlobalExceptionHandlerMiddleware` → ProblemDetails (REST) or OperationOutcome (FHIR) |
| Audit trail | `AuditMiddleware` captures user + IP per request; domain handlers write `AuditLogs` rows |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` on `/api/auth/login` — 10 req/min per IP |
| Health checks | `/health` checks DB, Blob Storage, Key Vault connectivity |
| CORS | Configured in `Program.cs`; origins from `Cors:AllowedOrigins` config key |
| Security headers | `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options` added in Phase 8 |
