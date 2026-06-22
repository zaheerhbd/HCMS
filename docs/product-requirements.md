# Healthcare Case Management System — Product Requirements

**Project Type:** Enterprise Portfolio Project  
**Stack:** Angular · .NET 8 Web API · Clean Architecture · EF Core · SQL Server / Azure SQL  
**Last Updated:** 2026-06-22

---

## 1. Product Requirements

### 1.1 Vision

A cloud-native, HIPAA-aligned Healthcare Case Management System (HCMS) that enables care coordinators, clinicians, and administrators to manage patient cases from intake through discharge — with full audit trails, document handling, and real-time dashboards.

### 1.2 Target Users

| Persona | Role | Primary Need |
|---|---|---|
| Care Coordinator | Manages patient case lifecycle | Create/update cases, assign tasks, track status |
| Clinician / Provider | Delivers patient care | View case notes, update clinical findings |
| Case Supervisor | Oversees team workload | Monitor caseloads, reassign, approve escalations |
| Administrator | Manages system configuration | User/role management, audit logs, reports |
| Patient (future phase) | Receives care | Self-service portal, appointment visibility |

### 1.3 Functional Requirements

#### Authentication & Authorization
- FR-01: Users must authenticate via username/password (JWT) with refresh token rotation.
- FR-02: Role-based access control (RBAC): Admin, Supervisor, CareCoordinator, Clinician, ReadOnly.
- FR-03: All actions must be authorized at the API layer; UI hides unauthorized elements.
- FR-04: Passwords must meet complexity rules; accounts lock after 5 failed attempts.

#### Patient Management
- FR-05: Register new patients with demographic, contact, and insurance information.
- FR-06: Search patients by name, date of birth, MRN (Medical Record Number), or insurance ID.
- FR-07: View complete patient profile including cases, documents, and appointment history.
- FR-08: Soft-delete patients (deactivate, not hard-delete) to preserve audit integrity.

#### Case Management
- FR-09: Create a case linked to a patient with a case type, priority, and assigned coordinator.
- FR-10: Case status lifecycle: Draft → Open → In Progress → Pending Review → Closed / Escalated.
- FR-11: Supervisors can reassign cases and approve escalations.
- FR-12: Case history log captures every status change with timestamp and acting user.
- FR-13: Support multiple concurrent open cases per patient.
- FR-14: Cases can be tagged (e.g., Chronic Disease, Post-Surgery, Mental Health).

#### Case Notes
- FR-15: Coordinators and clinicians can add timestamped notes to any case.
- FR-16: Notes support plain text; rich-text formatting is a stretch goal.
- FR-17: Notes are immutable after 24 hours (append-only model); corrections require a new note.
- FR-18: Notes are visible to all care team members assigned to the case.

#### Task Management
- FR-19: Tasks can be created within a case and assigned to any care team member.
- FR-20: Task statuses: Todo → In Progress → Blocked → Done → Cancelled.
- FR-21: Tasks have due dates; overdue tasks surface on the assignee's dashboard.
- FR-22: Completed tasks trigger a notification to the case coordinator.

#### Document Management
- FR-23: Upload documents (PDF, DOCX, PNG, JPG) up to 20 MB per file to a case.
- FR-24: Documents are stored in Azure Blob Storage; metadata (name, type, uploader) in SQL.
- FR-25: Generate time-limited (1-hour) SAS download URLs; never expose raw blob URLs.
- FR-26: Document categories: Referral, Lab Result, Consent Form, Insurance, Other.
- FR-27: Soft-delete documents; physical blob deletion is a background job (future).

#### Appointments & Scheduling
- FR-28: Schedule appointments linked to a patient and optionally to a case.
- FR-29: Appointment types: Initial Assessment, Follow-Up, Specialist Referral, Telehealth.
- FR-30: Appointment statuses: Scheduled → Confirmed → Completed → No-Show → Cancelled.
- FR-31: Prevent double-booking a provider for the same date/time slot.
- FR-32: Appointment reminders are queued 24 hours before (email/in-app, Phase 4+).

#### Care Team
- FR-33: Each case has a care team: one lead coordinator plus optional clinicians/specialists.
- FR-34: Team members can be added or removed; removals are logged in audit.
- FR-35: A user can be on multiple care teams simultaneously.

#### Dashboard & Reporting
- FR-36: Coordinator dashboard: my cases by status, overdue tasks, upcoming appointments.
- FR-37: Supervisor dashboard: team caseloads, escalated cases, cases with no activity in 7 days.
- FR-38: Admin dashboard: user activity, login history, system health.
- FR-39: Reports: Cases by status, Average case duration, Tasks by assignee (CSV export).

#### Audit & Compliance
- FR-40: Every create/update/delete on patient, case, note, document, and user records produces an audit log entry.
- FR-41: Audit logs are immutable; no API endpoint supports deletion.
- FR-42: Logs capture: entity type, entity ID, action, old value (JSON), new value (JSON), user ID, timestamp, IP address.

#### Notifications (Phase 4+)
- FR-43: In-app notification bell; unread count badge.
- FR-44: Notification triggers: case assigned, task assigned, task overdue, case escalated, appointment reminder.
- FR-45: Users can mark notifications read/dismissed.

#### FHIR/HL7 Integration (Phase 5)
- FR-46: The system shall expose a FHIR R4-compliant REST API at `/fhir/R4/` with a valid CapabilityStatement at `/fhir/R4/metadata`.
- FR-47: The FHIR API shall support read and search operations for Patient, Encounter (Case), Appointment, Task, and DocumentReference resources.
- FR-48: The `Patient/{id}/$everything` operation shall return a FHIR R4 Bundle containing all related resources for a patient (Encounters, Appointments, Tasks, DocumentReferences).
- FR-49: Users with CareCoordinator or Clinician role may import a patient by submitting a FHIR R4 Patient resource (JSON) via the FHIR Explorer screen or directly via `POST /fhir/R4/Patient`. The import follows a two-step flow: (1) **Parse & Preview** — the system validates the JSON, maps the FHIR fields to internal patient fields, and presents a field-mapping preview to the user before any data is saved; (2) **Confirm Import** — on user confirmation, the patient record is created, an internal MRN is auto-generated, the raw FHIR payload is stored in `FhirImportLogs`, and an audit entry is written to `AuditLogs`.
- FR-49a: The system shall validate that the submitted JSON is a well-formed FHIR R4 Patient resource (correct `resourceType`, at least one `name` entry); invalid submissions return an `OperationOutcome` with a descriptive error before any data is written.
- FR-49b: The following FHIR Patient fields shall be mapped to internal Patient entity fields:

  | FHIR Field | Internal Field | Notes |
  |---|---|---|
  | `name[0].family` | `LastName` | Required |
  | `name[0].given[0]` | `FirstName` | Required |
  | `gender` | `Gender` | Capitalised (male → Male) |
  | `birthDate` | `DateOfBirth` | ISO 8601 date |
  | `telecom[system=phone].value` | `Phone` | First phone entry |
  | `telecom[system=email].value` | `Email` | First email entry |
  | `address[0].text` | `Address` | Free-text address line |
  | `identifier[0].value` | `ExternalId` (stored in FhirImportLogs) | External MRN from source system |
  | `identifier[0].system` | `SourceSystem` (stored in FhirImportLogs) | e.g. `http://epic.com/mrn` |
  | *(auto-generated)* | `MRN` | Internal MRN format: `MRN-YYYY-NNNNN` |

- FR-49c: On successful import, the patient record shall be created with `RiskLevel = Medium` and `Status = Active` as defaults. The assigned coordinator shall default to the importing user.
- FR-49d: Every import attempt (success or failure) shall be logged in `FhirImportLogs` with the full raw FHIR payload, source system identifier, status (`Success` / `Failed`), and any error message.
- FR-49e: Successful imports shall also produce an `AuditLogs` entry with `Action = Create`, `EntityType = Patient`, and a detail note identifying the source system and external ID.
- FR-50: Any patient record may be exported as a downloadable FHIR R4 Bundle (JSON) from the patient detail page; the download is logged in audit.
- FR-51: FHIR search shall support standard parameters: Patient by `_id`, `name`, `birthdate`, `identifier`; Encounter by `patient`, `status`.
- FR-52: All FHIR API errors shall return HL7-compliant `OperationOutcome` resources; HTTP status codes shall follow FHIR REST semantics.
- FR-53 *(stretch)*: The system shall accept HL7 v2 ADT^A01 messages via an HTTP endpoint and auto-create or update the corresponding Patient record.
- FR-54 *(stretch)*: The system shall accept HL7 v2 ORU^R01 messages (lab observation results) and attach the payload as a read-only CaseNote on the patient's active case.

### 1.4 Non-Functional Requirements

| Category | Requirement |
|---|---|
| Security | HTTPS everywhere; secrets in Azure Key Vault; no secrets in source code |
| Security | OWASP Top 10 mitigated; input validation on all endpoints |
| Performance | API p95 response time < 400 ms under 100 concurrent users |
| Availability | 99.5% uptime SLA target (Azure App Service Standard tier) |
| Scalability | Stateless API; horizontal scale-out via App Service scale rules |
| Auditability | All PHI-touching operations logged with user identity |
| Observability | Structured logging to Application Insights; custom dashboards |
| Compliance | HIPAA-aligned design: minimum necessary data, audit trails, encryption at rest and in transit |
| Browser Support | Latest Chrome, Edge, Firefox, Safari |
| Accessibility | WCAG 2.1 AA for all primary user flows |

---

## 2. Main Modules

| # | Module | Responsibility |
|---|---|---|
| 1 | Identity & Access | Authentication, JWT, refresh tokens, RBAC, lockout |
| 2 | Patient Management | Registration, search, profile, demographics, insurance |
| 3 | Case Management | Case lifecycle, status machine, tags, history, escalation |
| 4 | Case Notes | Append-only clinical notes per case |
| 5 | Task Management | Case-scoped tasks, assignments, due dates, statuses |
| 6 | Document Management | Blob upload/download, metadata, SAS URLs, categories |
| 7 | Appointments | Scheduling, provider calendar, conflict detection |
| 8 | Care Team | Team composition per case, member roles |
| 9 | Dashboard & Reports | Coordinator/Supervisor/Admin views, CSV export |
| 10 | Audit Log | Immutable event log for compliance |
| 11 | Notifications | In-app alerts, trigger engine |
| 12 | Administration | User management, role assignment, system config |
| 13 | FHIR/HL7 Integration | FHIR R4 REST facade, resource mapping (Patient/Encounter/Appointment/Task/DocumentReference), Bundle export/import, HL7 v2 receiver (stretch) |

---

## 3. Database Tables

### Schema: `dbo`

#### Identity & Access

```sql
Users
  UserId          UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  Email           NVARCHAR(256) UNIQUE NOT NULL
  PasswordHash    NVARCHAR(512) NOT NULL
  FirstName       NVARCHAR(100) NOT NULL
  LastName        NVARCHAR(100) NOT NULL
  IsActive        BIT NOT NULL DEFAULT 1
  FailedLoginCount INT NOT NULL DEFAULT 0
  LockedUntil     DATETIME2 NULL
  LastLoginAt     DATETIME2 NULL
  CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()

Roles
  RoleId    INT PK IDENTITY
  Name      NVARCHAR(50) UNIQUE NOT NULL   -- Admin, Supervisor, CareCoordinator, Clinician, ReadOnly
  Description NVARCHAR(200) NULL

UserRoles
  UserId  UNIQUEIDENTIFIER FK -> Users.UserId
  RoleId  INT FK -> Roles.RoleId
  PK(UserId, RoleId)

RefreshTokens
  TokenId     UNIQUEIDENTIFIER PK
  UserId      UNIQUEIDENTIFIER FK -> Users.UserId
  Token       NVARCHAR(512) NOT NULL
  ExpiresAt   DATETIME2 NOT NULL
  RevokedAt   DATETIME2 NULL
  CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
```

#### Patient Management

```sql
Patients
  PatientId       UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  MRN             NVARCHAR(20) UNIQUE NOT NULL   -- System-generated Medical Record Number
  FirstName       NVARCHAR(100) NOT NULL
  LastName        NVARCHAR(100) NOT NULL
  DateOfBirth     DATE NOT NULL
  Gender          NVARCHAR(20) NULL              -- Male, Female, NonBinary, Prefer Not to Say
  Phone           NVARCHAR(20) NULL
  Email           NVARCHAR(256) NULL
  Address         NVARCHAR(500) NULL
  City            NVARCHAR(100) NULL
  State           NVARCHAR(50) NULL
  ZipCode         NVARCHAR(20) NULL
  EmergencyContactName    NVARCHAR(200) NULL
  EmergencyContactPhone   NVARCHAR(20) NULL
  IsActive        BIT NOT NULL DEFAULT 1
  CreatedBy       UNIQUEIDENTIFIER FK -> Users.UserId
  CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()

PatientInsurance
  InsuranceId     UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  PatientId       UNIQUEIDENTIFIER FK -> Patients.PatientId
  InsurancePlan   NVARCHAR(200) NOT NULL
  MemberId        NVARCHAR(100) NOT NULL
  GroupNumber     NVARCHAR(100) NULL
  SubscriberName  NVARCHAR(200) NULL
  EffectiveDate   DATE NOT NULL
  TerminationDate DATE NULL
  IsPrimary       BIT NOT NULL DEFAULT 1
  CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
```

#### Case Management

```sql
CaseTypes
  CaseTypeId  INT PK IDENTITY
  Name        NVARCHAR(100) UNIQUE NOT NULL   -- e.g. Chronic Disease, Post-Surgery, Mental Health
  Description NVARCHAR(500) NULL
  IsActive    BIT NOT NULL DEFAULT 1

Cases
  CaseId          UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  CaseNumber      NVARCHAR(20) UNIQUE NOT NULL   -- System-generated, e.g. CASE-2026-00001
  PatientId       UNIQUEIDENTIFIER FK -> Patients.PatientId
  CaseTypeId      INT FK -> CaseTypes.CaseTypeId
  Title           NVARCHAR(300) NOT NULL
  Description     NVARCHAR(2000) NULL
  Priority        NVARCHAR(20) NOT NULL DEFAULT 'Medium'  -- Low, Medium, High, Critical
  Status          NVARCHAR(50) NOT NULL DEFAULT 'Draft'   -- Draft, Open, InProgress, PendingReview, Closed, Escalated
  LeadCoordinatorId UNIQUEIDENTIFIER FK -> Users.UserId
  OpenedAt        DATETIME2 NULL
  ClosedAt        DATETIME2 NULL
  DueDate         DATE NULL
  IsActive        BIT NOT NULL DEFAULT 1
  CreatedBy       UNIQUEIDENTIFIER FK -> Users.UserId
  CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()

CaseTags
  TagId   INT PK IDENTITY
  Name    NVARCHAR(100) UNIQUE NOT NULL

CaseCaseTags
  CaseId  UNIQUEIDENTIFIER FK -> Cases.CaseId
  TagId   INT FK -> CaseTags.TagId
  PK(CaseId, TagId)

CaseStatusHistory
  HistoryId   UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  CaseId      UNIQUEIDENTIFIER FK -> Cases.CaseId
  FromStatus  NVARCHAR(50) NULL
  ToStatus    NVARCHAR(50) NOT NULL
  Comment     NVARCHAR(500) NULL
  ChangedBy   UNIQUEIDENTIFIER FK -> Users.UserId
  ChangedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()

CaseNotes
  NoteId      UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  CaseId      UNIQUEIDENTIFIER FK -> Cases.CaseId
  Content     NVARCHAR(MAX) NOT NULL
  IsEditable  BIT NOT NULL DEFAULT 1   -- set to 0 after 24h
  CreatedBy   UNIQUEIDENTIFIER FK -> Users.UserId
  CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
```

#### Care Team

```sql
CareTeamMembers
  MemberId    UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  CaseId      UNIQUEIDENTIFIER FK -> Cases.CaseId
  UserId      UNIQUEIDENTIFIER FK -> Users.UserId
  TeamRole    NVARCHAR(100) NOT NULL   -- Lead, Clinician, Specialist, Support
  AddedBy     UNIQUEIDENTIFIER FK -> Users.UserId
  AddedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  RemovedAt   DATETIME2 NULL
  RemovedBy   UNIQUEIDENTIFIER FK -> Users.UserId NULL
```

#### Task Management

```sql
CaseTasks
  TaskId      UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  CaseId      UNIQUEIDENTIFIER FK -> Cases.CaseId
  Title       NVARCHAR(300) NOT NULL
  Description NVARCHAR(2000) NULL
  Status      NVARCHAR(50) NOT NULL DEFAULT 'Todo'   -- Todo, InProgress, Blocked, Done, Cancelled
  Priority    NVARCHAR(20) NOT NULL DEFAULT 'Medium'
  AssignedTo  UNIQUEIDENTIFIER FK -> Users.UserId NULL
  DueDate     DATE NULL
  CompletedAt DATETIME2 NULL
  CompletedBy UNIQUEIDENTIFIER FK -> Users.UserId NULL
  CreatedBy   UNIQUEIDENTIFIER FK -> Users.UserId
  CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  UpdatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
```

#### Document Management

```sql
Documents
  DocumentId      UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  CaseId          UNIQUEIDENTIFIER FK -> Cases.CaseId
  PatientId       UNIQUEIDENTIFIER FK -> Patients.PatientId
  FileName        NVARCHAR(500) NOT NULL
  ContentType     NVARCHAR(100) NOT NULL
  FileSizeBytes   BIGINT NOT NULL
  BlobContainerName NVARCHAR(100) NOT NULL
  BlobName        NVARCHAR(500) NOT NULL   -- path in Azure Blob Storage
  Category        NVARCHAR(100) NOT NULL   -- Referral, LabResult, ConsentForm, Insurance, Other
  Description     NVARCHAR(500) NULL
  IsDeleted       BIT NOT NULL DEFAULT 0
  DeletedAt       DATETIME2 NULL
  DeletedBy       UNIQUEIDENTIFIER FK -> Users.UserId NULL
  UploadedBy      UNIQUEIDENTIFIER FK -> Users.UserId
  UploadedAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE()
```

#### Appointments

```sql
Providers
  ProviderId  UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  UserId      UNIQUEIDENTIFIER FK -> Users.UserId NULL   -- NULL if external provider
  Name        NVARCHAR(200) NOT NULL
  Specialty   NVARCHAR(200) NULL
  IsActive    BIT NOT NULL DEFAULT 1

Appointments
  AppointmentId   UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  PatientId       UNIQUEIDENTIFIER FK -> Patients.PatientId
  CaseId          UNIQUEIDENTIFIER FK -> Cases.CaseId NULL
  ProviderId      UNIQUEIDENTIFIER FK -> Providers.ProviderId
  AppointmentType NVARCHAR(100) NOT NULL   -- InitialAssessment, FollowUp, SpecialistReferral, Telehealth
  Status          NVARCHAR(50) NOT NULL DEFAULT 'Scheduled'  -- Scheduled, Confirmed, Completed, NoShow, Cancelled
  ScheduledStart  DATETIME2 NOT NULL
  ScheduledEnd    DATETIME2 NOT NULL
  Location        NVARCHAR(300) NULL
  Notes           NVARCHAR(2000) NULL
  ReminderSentAt  DATETIME2 NULL
  CreatedBy       UNIQUEIDENTIFIER FK -> Users.UserId
  CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
```

#### Notifications

```sql
Notifications
  NotificationId  UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  UserId          UNIQUEIDENTIFIER FK -> Users.UserId
  Title           NVARCHAR(300) NOT NULL
  Body            NVARCHAR(1000) NOT NULL
  Type            NVARCHAR(100) NOT NULL   -- CaseAssigned, TaskAssigned, TaskOverdue, Escalation, AppointmentReminder
  EntityType      NVARCHAR(100) NULL       -- Case, Task, Appointment
  EntityId        UNIQUEIDENTIFIER NULL
  IsRead          BIT NOT NULL DEFAULT 0
  ReadAt          DATETIME2 NULL
  CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
```

#### Audit

```sql
AuditLogs
  AuditId     UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  EntityType  NVARCHAR(100) NOT NULL   -- Patient, Case, CaseNote, Document, User, ...
  EntityId    NVARCHAR(100) NOT NULL
  Action      NVARCHAR(50) NOT NULL    -- Create, Update, Delete, Login, Logout, Download
  OldValues   NVARCHAR(MAX) NULL       -- JSON snapshot
  NewValues   NVARCHAR(MAX) NULL       -- JSON snapshot
  UserId      UNIQUEIDENTIFIER NULL    -- NULL for system actions
  UserEmail   NVARCHAR(256) NULL       -- denormalized for log permanence
  IpAddress   NVARCHAR(50) NULL
  UserAgent   NVARCHAR(500) NULL
  OccurredAt  DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  -- No FK to Users: audit log must survive user deletion
```

#### FHIR Integration

```sql
FhirImportLogs
  ImportId          UNIQUEIDENTIFIER PK DEFAULT NEWSEQUENTIALID()
  PatientId         UNIQUEIDENTIFIER FK -> Patients.PatientId NULL  -- NULL when import fails before Patient is created
  SourceSystem      NVARCHAR(200) NULL        -- e.g. "ExternalEHR", "ManualUpload"
  FhirResourceType  NVARCHAR(100) NOT NULL DEFAULT 'Patient'
  FhirResourceId    NVARCHAR(100) NULL        -- source system's FHIR resource ID
  FhirVersion       NVARCHAR(20) NOT NULL DEFAULT 'R4'
  RawPayload        NVARCHAR(MAX) NOT NULL    -- original FHIR JSON stored for traceability
  Status            NVARCHAR(50) NOT NULL     -- Success, Failed, PartialSuccess
  ErrorMessage      NVARCHAR(2000) NULL
  ImportedBy        UNIQUEIDENTIFIER FK -> Users.UserId
  ImportedAt        DATETIME2 NOT NULL DEFAULT GETUTCDATE()
```

---

## 4. API Modules (.NET 8 Web API)

### Clean Architecture Layers

```
HCM.Domain          — Entities, value objects, domain events, enums
HCM.Application     — Use cases (CQRS with MediatR), interfaces, DTOs, validators
HCM.Infrastructure  — EF Core DbContext, repositories, Blob storage, Key Vault, email
HCM.API             — Controllers, middleware, filters, DI composition root
```

### Controllers & Key Endpoints

#### `AuthController` — `/api/auth`
| Method | Route | Description |
|---|---|---|
| POST | `/login` | Authenticate; return JWT + refresh token |
| POST | `/refresh` | Exchange refresh token for new JWT |
| POST | `/logout` | Revoke refresh token |
| POST | `/change-password` | Authenticated password change |

#### `UsersController` — `/api/users` _(Admin only)_
| Method | Route | Description |
|---|---|---|
| GET | `/` | Paginated user list with filters |
| GET | `/{id}` | User detail |
| POST | `/` | Create user and assign roles |
| PUT | `/{id}` | Update user info |
| PUT | `/{id}/roles` | Reassign roles |
| PUT | `/{id}/activate` | Activate / deactivate user |

#### `PatientsController` — `/api/patients`
| Method | Route | Description |
|---|---|---|
| GET | `/` | Paginated search (name, DOB, MRN, insurance ID) |
| GET | `/{id}` | Full patient profile |
| POST | `/` | Register new patient |
| PUT | `/{id}` | Update demographics |
| DELETE | `/{id}` | Soft-delete (deactivate) |
| GET | `/{id}/cases` | All cases for patient |
| GET | `/{id}/appointments` | Patient appointment history |
| GET | `/{id}/documents` | Patient document list |

#### `CasesController` — `/api/cases`
| Method | Route | Description |
|---|---|---|
| GET | `/` | Paginated case list with filters (status, priority, coordinator, tag) |
| GET | `/{id}` | Case detail with care team, notes summary |
| POST | `/` | Create case |
| PUT | `/{id}` | Update case metadata |
| PUT | `/{id}/status` | Transition case status |
| GET | `/{id}/history` | Status change history |
| GET | `/{id}/notes` | Paginated notes |
| POST | `/{id}/notes` | Add note |
| GET | `/{id}/tasks` | Task list for case |
| POST | `/{id}/tasks` | Create task |
| GET | `/{id}/documents` | Documents attached to case |
| GET | `/{id}/team` | Care team members |
| POST | `/{id}/team` | Add team member |
| DELETE | `/{id}/team/{memberId}` | Remove team member |

#### `TasksController` — `/api/tasks`
| Method | Route | Description |
|---|---|---|
| GET | `/my` | Tasks assigned to current user |
| GET | `/{id}` | Task detail |
| PUT | `/{id}` | Update task |
| PUT | `/{id}/status` | Transition task status |
| DELETE | `/{id}` | Cancel task (soft) |

#### `DocumentsController` — `/api/documents`
| Method | Route | Description |
|---|---|---|
| POST | `/upload` | Upload document (multipart/form-data) |
| GET | `/{id}/download-url` | Get 1-hour SAS URL |
| DELETE | `/{id}` | Soft-delete document |

#### `AppointmentsController` — `/api/appointments`
| Method | Route | Description |
|---|---|---|
| GET | `/` | Paginated list with filters |
| GET | `/{id}` | Appointment detail |
| POST | `/` | Schedule appointment (validates conflicts) |
| PUT | `/{id}` | Update appointment |
| PUT | `/{id}/status` | Update status (Confirm, Complete, Cancel, NoShow) |
| GET | `/providers/{providerId}/availability` | Available slots for a provider |

#### `DashboardController` — `/api/dashboard`
| Method | Route | Description |
|---|---|---|
| GET | `/coordinator` | My cases summary, overdue tasks, upcoming appointments |
| GET | `/supervisor` | Team caseloads, escalated cases, inactive cases |
| GET | `/admin` | User counts, login activity, system metrics |

#### `ReportsController` — `/api/reports`
| Method | Route | Description |
|---|---|---|
| GET | `/cases-by-status` | Count/list grouped by status (CSV or JSON) |
| GET | `/case-duration` | Average open→close duration by case type |
| GET | `/tasks-by-assignee` | Task completion rates per user |

#### `AuditController` — `/api/audit` _(Admin only)_
| Method | Route | Description |
|---|---|---|
| GET | `/` | Paginated audit log with filters (entity, user, date range, action) |
| GET | `/{entityType}/{entityId}` | All audit entries for a specific entity |

#### `NotificationsController` — `/api/notifications`
| Method | Route | Description |
|---|---|---|
| GET | `/` | Current user's notifications (unread first) |
| GET | `/unread-count` | Badge count |
| PUT | `/{id}/read` | Mark notification read |
| PUT | `/mark-all-read` | Mark all read |

#### `FhirController` — `/fhir/R4`

Uses the **Firely .NET SDK** (`Hl7.Fhir.R4` NuGet package) for resource serialization. The controller maps internal entities to FHIR R4 resources on the fly — no separate FHIR datastore needed.

| Method | Route | FHIR Operation | Internal Mapping |
|---|---|---|---|
| GET | `/metadata` | CapabilityStatement | Hardcoded conformance resource declaring supported resource types and operations |
| GET | `/Patient` | search-type | `Patients` table; search params: `_id`, `name`, `birthdate`, `identifier` (MRN) |
| GET | `/Patient/{id}` | read | Single `Patient` entity → FHIR Patient resource |
| POST | `/Patient` | create | Import FHIR Patient JSON → validate with Firely SDK → map fields → create `Patients` row → write `FhirImportLogs` entry + `AuditLogs` entry → return `201 Created` with new MRN |
| GET | `/Patient/{id}/$everything` | operation | FHIR Bundle (type: collection): Patient + Encounters + Appointments + Tasks + DocumentReferences |
| GET | `/Encounter` | search-type | `Cases` table; params: `patient`, `status` |
| GET | `/Encounter/{id}` | read | Single `Case` mapped to FHIR Encounter |
| GET | `/Appointment/{id}` | read | Single `Appointment` mapped to FHIR Appointment |
| GET | `/Task/{id}` | read | Single `CaseTask` mapped to FHIR Task |
| GET | `/DocumentReference/{id}` | read | Single `Document` mapped to FHIR DocumentReference |

**FHIR Resource Mappings:**

| Internal Entity | FHIR R4 Resource | Key Field Mappings |
|---|---|---|
| `Patient` | `Patient` | `PatientId → id`, `MRN → identifier[system=MRN]`, `FirstName+LastName → name`, `DateOfBirth → birthDate`, `Gender → gender`, `Phone/Email → telecom` |
| `Case` | `Encounter` | `CaseId → id`, `CaseNumber → identifier`, `Status: Draft/Open → planned, InProgress → in-progress, Closed → finished, Escalated → entered-in-error`, `Priority → priority` |
| `Appointment` | `Appointment` | `AppointmentId → id`, `Status → status`, `ScheduledStart → start`, `ScheduledEnd → end`, `AppointmentType → serviceType` |
| `CaseTask` | `Task` | `TaskId → id`, `Status → status`, `AssignedTo → owner`, `DueDate → executionPeriod.end`, `Priority → priority` |
| `Document` | `DocumentReference` | `DocumentId → id`, `Category → category`, `FileName → content.attachment.title`, `ContentType → content.attachment.contentType` |

**FHIR Patient Import — Detailed Flow (`POST /fhir/R4/Patient`)**

This endpoint is the backend behind the FHIR Explorer "Import Patient" screen. It handles inbound patient records from any external system that speaks FHIR R4 (Epic, Cerner, referring clinics, etc.).

```
Request:  POST /fhir/R4/Patient
          Content-Type: application/fhir+json
          Authorization: Bearer {jwt}   (CareCoordinator or Clinician role required)
          Body: { FHIR R4 Patient resource JSON }

Processing steps (Application layer — IFhirImportService):
  1. Deserialise with Firely SDK (Hl7.Fhir.R4) — returns OperationOutcome on parse failure
  2. Validate resourceType === "Patient" and name exists — return 422 OperationOutcome if invalid
  3. Map FHIR fields → PatientEntity (see FR-49b mapping table)
  4. Auto-generate internal MRN (format: MRN-YYYY-NNNNN, sequential)
  5. Save PatientEntity via EF Core (INSERT into Patients)
  6. Write FhirImportLogs row (Status=Success, RawPayload=original JSON, SourceSystem, ExternalId)
  7. Write AuditLogs row (Action=Create, EntityType=Patient, detail includes source system + external ID)
  8. Return 201 Created with Location header: /fhir/R4/Patient/{newPatientId}
     Body: the created FHIR Patient resource (with internal MRN as identifier)

On failure:
  - Invalid JSON:           400 Bad Request  + OperationOutcome
  - Wrong resourceType:     422 Unprocessable + OperationOutcome
  - Missing required name:  422 Unprocessable + OperationOutcome
  - Duplicate external ID:  409 Conflict      + OperationOutcome (if identifier already in FhirImportLogs)
  - All failures are logged in FhirImportLogs with Status=Failed and ErrorMessage
```

**UI Flow (Angular `FhirImportComponent`):**

The user-facing import is a two-step modal on the FHIR Explorer screen:

1. **Step 1 — Paste & Parse:** User pastes FHIR Patient JSON (or clicks "Use Sample"). Clicking "Parse & Preview" calls a client-side mapping preview (no server round-trip yet) that validates the JSON shape and renders a field-mapping table showing exactly what will be imported.
2. **Step 2 — Confirm Import:** Clicking "Confirm Import" posts the raw JSON to `POST /fhir/R4/Patient`. On `201 Created`, the Angular `PatientsService` cache is invalidated and the app navigates to the new patient's detail screen. On error, the `OperationOutcome` message is displayed inline in the modal.

#### `Hl7Controller` — `/api/hl7` *(stretch)*
| Method | Route | Description |
|---|---|---|
| POST | `/adt` | Receive HL7 v2 ADT^A01 (admit/register patient); parse with NHapi; upsert Patient + log to FhirImportLogs |
| POST | `/oru` | Receive HL7 v2 ORU^R01 (lab/observation result); attach as read-only CaseNote on patient's active case |

### Cross-Cutting Concerns (Middleware & Infrastructure)
- **Global exception handler** — maps domain exceptions to RFC 7807 ProblemDetails
- **JWT authentication** — `Microsoft.AspNetCore.Authentication.JwtBearer`
- **Audit middleware** — captures user identity, IP, and user agent per request
- **Rate limiting** — `Microsoft.AspNetCore.RateLimiting` on auth endpoints
- **FluentValidation** — request DTO validation via MediatR pipeline behavior
- **Serilog → Application Insights** — structured log sink
- **Health checks** — `/health` endpoint covering DB, Blob, Key Vault

---

## 5. Angular Modules

### Module Structure

```
src/
  app/
    core/               # Singleton services, interceptors, guards
      services/         # AuthService, UserService, NotificationService
      interceptors/     # JwtInterceptor, ErrorInterceptor, LoadingInterceptor
      guards/           # AuthGuard, RoleGuard
      models/           # Core shared interfaces
    shared/             # Reusable dumb components and pipes
      components/       # PageHeader, DataTable, ConfirmDialog, StatusBadge, FileUpload
      pipes/            # DateFormatPipe, TruncatePipe, EnumLabelPipe
      directives/       # HasRoleDirective
    features/
      auth/             # Login, ChangePassword
      dashboard/        # CoordinatorDashboard, SupervisorDashboard, AdminDashboard
      patients/         # PatientList, PatientDetail, PatientForm
      cases/            # CaseList, CaseDetail, CaseForm, CaseStatusHistory
      case-notes/       # NoteList, NoteForm (embedded in CaseDetail)
      tasks/            # TaskList, TaskDetail, TaskForm, MyTasks
      documents/        # DocumentList, DocumentUpload (embedded in CaseDetail/PatientDetail)
      appointments/     # AppointmentList, AppointmentForm, ProviderCalendar
      reports/          # ReportsDashboard, CasesByStatus, CaseDuration, TasksByAssignee
      notifications/    # NotificationBell, NotificationList
      admin/            # UserList, UserForm, RoleAssignment, AuditLog
      fhir/             # FhirExplorer, FhirImportModalComponent (two-step: parse/preview → confirm), FhirExportButton (embedded in PatientDetail)
```

### Key Angular Patterns
| Pattern | Implementation |
|---|---|
| State management | NgRx Store for auth state and notifications; component-level RxJS for local UI state |
| HTTP | `HttpClient` with typed services; `catchError` to global error handler |
| Forms | Reactive Forms throughout; custom validators for date ranges, phone, MRN |
| Routing | Lazy-loaded feature modules; role-based route guards |
| UI Library | Angular Material (MDC) — consistent enterprise look, accessibility built-in |
| Tables | Angular CDK virtual scroll for large lists; Angular Material paginator |
| Charts | Chart.js via `ng2-charts` for dashboard widgets |
| File upload | Custom `FileUploadComponent` using `FormData` and progress events |
| Auth tokens | `localStorage` for JWT; `sessionStorage` option for sensitive roles |
| FHIR display | `ngx-json-viewer` for rendering FHIR resource JSON; `FhirService` targets `/fhir/R4/` endpoints |

---

## 6. Azure Deployment Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        GitHub Actions CI/CD                          │
│  Build → Test → Publish → Deploy (dev → staging → prod)             │
└────────────────────────────┬────────────────────────────────────────┘
                             │
            ┌────────────────┼────────────────┐
            ▼                ▼                 ▼
  ┌──────────────────┐  ┌─────────────┐  ┌──────────────────┐
  │ Azure Static     │  │ Azure App   │  │  Azure SQL        │
  │ Web Apps         │  │ Service     │  │  Database         │
  │ (Angular SPA)    │  │ (.NET 8 API)│  │  (General Purpose │
  │  Free/Standard   │  │  P1v3 tier) │  │   S2 tier)        │
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
     │  (documents,     │  │  (traces,    │  │  (alerts,      │
     │   private tier)  │  │   metrics,   │  │   dashboards)  │
     └──────────────────┘  │   logs)      │  └───────────────┘
                           └──────────────┘
```

### Resource Naming Convention
`hcm-{env}-{resource}` e.g. `hcm-prod-api`, `hcm-prod-sql`, `hcm-prod-kv`

### Environments
| Environment | Purpose | Tier |
|---|---|---|
| `dev` | Local development | SQL Server LocalDB / Docker |
| `staging` | Integration testing + demos | Azure Free/Basic tiers |
| `prod` | Production portfolio demo | Azure Standard tiers |

### Key Azure Services Configuration

**Azure App Service (API)**
- Runtime: .NET 8
- Always On: enabled
- HTTPS only: enforced
- Managed Identity: enabled (used to access Key Vault and Blob Storage without stored credentials)
- VNET Integration: connects to Azure SQL on private endpoint (prod)

**Azure Static Web Apps (Angular)**
- Custom domain support
- Global CDN distribution
- Free SSL certificate
- Staging environments per pull request (preview URLs)

**Azure SQL Database**
- General Purpose tier (dev/staging: Basic)
- Azure Defender for SQL: enabled
- Transparent Data Encryption: enabled by default
- Geo-redundant backup: enabled (prod)
- Connection: EF Core connection string from Key Vault

**Azure Blob Storage**
- Account kind: StorageV2
- Access tier: Hot
- Container: `case-documents` (private — no public access)
- CORS configured for API origin only
- Soft delete: 30 days (prod)
- Managed Identity access from App Service (no stored keys)

**Azure Key Vault**
- Access: Managed Identity RBAC (Key Vault Secrets User)
- Secrets: `ConnectionStrings--DefaultConnection`, `Jwt--Secret`, `BlobStorage--ConnectionString`
- Diagnostic logs forwarded to Application Insights

**Application Insights**
- SDK: `Microsoft.ApplicationInsights.AspNetCore`
- Custom events: case status transitions, document uploads, login failures
- Availability test: ping `/health` every 5 minutes
- Alerts: error rate > 5%, p95 latency > 2s, availability < 99%

### CI/CD Pipeline (GitHub Actions)

```
Trigger: push to main / PR to main

Jobs:
  build-api:
    - dotnet restore && dotnet build --no-restore
    - dotnet test (unit + integration)
    - dotnet publish -o ./publish
    - Upload artifact

  build-angular:
    - npm ci
    - ng lint
    - ng test --watch=false
    - ng build --configuration production
    - Upload artifact

  deploy-staging:
    needs: [build-api, build-angular]
    - Deploy API to hcm-staging-api (App Service)
    - Deploy Angular to Static Web Apps staging slot
    - Run smoke tests

  deploy-prod:
    needs: deploy-staging
    environment: production  (manual approval gate)
    - Deploy API to hcm-prod-api
    - Deploy Angular to Static Web Apps production
```

---

## 7. Azure Resource Diagram (Logical)

```
Resource Group: rg-hcm-prod
│
├── App Service Plan: asp-hcm-prod (P1v3 Linux)
│   └── App Service: hcm-prod-api (.NET 8)
│
├── Static Web App: hcm-prod-web (Angular)
│
├── Azure SQL Server: hcm-prod-sqlsrv
│   └── Database: hcm-prod-db (S2)
│
├── Storage Account: hcmprodst
│   └── Container: case-documents (private)
│
├── Key Vault: hcm-prod-kv
│
├── Application Insights: hcm-prod-ai
│
└── Log Analytics Workspace: hcm-prod-law
```

---

## 8. Risks and Assumptions

### Risks

| # | Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| R1 | Azure costs exceed free-tier for portfolio demo | Medium | Medium | Use Basic/Free tiers; set budget alerts; shut down non-prod after business hours |
| R2 | HIPAA compliance gaps expose PHI in logs | Medium | High | Strip PHI from all log messages; use entity IDs only; enable App Insights data masking |
| R3 | JWT secret leaked to source control | Low | Critical | Use Key Vault; scan repo with GitHub secret detection; pre-commit hooks |
| R4 | SQL injection via dynamic queries | Low | Critical | Use EF Core parameterized queries only; ban raw SQL in PRs |
| R5 | Blob SAS URL leaked (document access control failure) | Low | High | Short-lived SAS (1 hour); log every download in audit; no public container access |
| R6 | Scope creep extends Phase 1 indefinitely | High | Medium | Strict phase definitions; defer nice-to-haves to backlog; time-box phases |
| R7 | EF Core migration conflicts in team development | Medium | Low | Single migration owner per branch; squash migrations before merge |
| R8 | Angular bundle size too large for demo | Low | Low | Lazy loading per module; check bundle with `ng build --stats-json` + webpack-bundle-analyzer |
| R9 | No real SMTP for email notifications | Medium | Low | Phase 4 feature; use SendGrid free tier or stub in earlier phases |
| R10 | Azure AD not integrated (SAML/OAuth) | Low | Low | Out of scope for portfolio; JWT + RBAC is sufficient to demonstrate skill |
| R11 | FHIR resource mapping has edge cases (e.g. status enum mismatches, missing required FHIR fields) | Medium | Medium | Scope Phase 5 to read + export first; use Firely SDK for serialization validation; add unit tests per mapping; defer write operations if time-boxed |
| R12 | Firely .NET SDK (`Hl7.Fhir.R4`) adds significant package weight and serialization overhead | Low | Low | Isolate FHIR logic behind `IFhirMappingService` in Infrastructure; SDK is only loaded in FHIR-specific request paths |

### Assumptions

| # | Assumption |
|---|---|
| A1 | This is a portfolio project; no real patient data will ever be stored |
| A2 | A single Azure subscription is used for all environments |
| A3 | GitHub is the source control and CI/CD platform |
| A4 | The developer has an active Azure subscription (pay-as-you-go or Visual Studio credits) |
| A5 | Angular 17+ (standalone components acceptable but NgModules used for demonstrating enterprise patterns) |
| A6 | .NET 8 LTS is the target runtime |
| A7 | SQL Server 2022 / Azure SQL is the only supported database |
| A8 | Multi-tenancy (multiple hospital organizations) is out of scope for this phase |
| A9 | FHIR R4 API facade, Bundle export, and Patient import are in scope (Phase 5); SMART on FHIR auth and full bi-directional EHR sync are out of scope |
| A10 | Mobile app is out of scope; responsive web is the target |
| A11 | Email notifications use a third-party service (SendGrid) when implemented in Phase 4 |
| A12 | The Angular app is a SPA; no SSR is required |
