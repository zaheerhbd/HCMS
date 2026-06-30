# HCMS — Feature Reference

**Last Updated:** 2026-06-29 (session 3)

> One section per module. Update when a feature ships, changes, or is deferred.
> Each entry links to the FR number in [product-requirements.md](product-requirements.md).

---

## Module Index

| # | Module | Phase | Status | FR References |
|---|---|---|---|---|
| 1 | Identity & Access (Auth) | 1 | Complete | FR-01 – FR-04 |
| 2 | Patient Management | 2 | In Progress | FR-05 – FR-08 |
| 3 | Case Management | 2 | In Progress | FR-09 – FR-14 |
| 4 | Case Notes | 2 | In Progress | FR-15 – FR-18 |
| 5 | Task Management | 3 | Not Started | FR-19 – FR-22 |
| 6 | Document Management | 3 | Not Started | FR-23 – FR-27 |
| 7 | Appointments & Scheduling | 4 | Not Started | FR-28 – FR-32 |
| 8 | Care Team | 2 | In Progress | FR-33 – FR-35 |
| 9 | Dashboard & Reports | 6 | Not Started | FR-36 – FR-39 |
| 10 | Audit & Compliance | 2 | In Progress | FR-40 – FR-42 |
| 11 | Notifications | 7 | Not Started | FR-43 – FR-45 |
| 12 | Administration (User Mgmt) | 2 | In Progress | (Admin endpoints) |
| 13 | FHIR / HL7 Integration | 5 | Not Started | FR-46 – FR-54 |

---

## 1. Identity & Access

**Phase:** 1 | **Status:** Complete

### Features
- Username + password login with JWT (HS256) access tokens (60 min) and refresh tokens (30 days).
- Refresh token rotation — old token revoked on each refresh, preventing replay.
- Account lockout after 5 consecutive failed attempts; `LockedUntil` timestamp set.
- RBAC: `Admin`, `Supervisor`, `CareCoordinator`, `Clinician`, `ReadOnly`.
- Password change endpoint for authenticated users.

### API Endpoints
| Method | Route | Role Required | Description |
|---|---|---|---|
| POST | `/api/auth/login` | Public | Authenticate; return JWT + refresh token |
| POST | `/api/auth/refresh` | Public | Exchange refresh token for new pair |
| POST | `/api/auth/logout` | Authenticated | Revoke refresh token |
| POST | `/api/auth/change-password` | Authenticated | Password change |

### Angular Components
- `LoginComponent` — Reactive Form; shows error on lockout vs. wrong credentials.
- `AuthService` — token storage, refresh-on-401, `isAuthenticated()`, `hasRole()`.
- `JwtInterceptor` — attaches `Authorization: Bearer` to every request.
- `ErrorInterceptor` — intercepts 401 and triggers silent refresh.
- `AuthGuard` — redirects unauthenticated users to `/login`.
- `RoleGuard` — reads `data.roles` from route config; redirects if role not permitted.
- `HasRoleDirective` — `*hasRole="['Admin']"` structural directive.

### DB Tables
`Users` · `Roles` · `UserRoles` · `RefreshTokens`

### Notes
- No Azure AD / SAML. JWT self-signed with a Key Vault secret.
- Rate limiting: 10 login attempts per minute per IP.

---

## 2. Patient Management

**Phase:** 2 | **Status:** In Progress (Backend + Frontend complete; routes use MRN: `/api/patients/{mrn}`)

### Features
- Register patients with demographics, contact info, and insurance.
- Auto-generated MRN: `MRN-{YYYY}-{NNNNN}`.
- Search by name, DOB, MRN, or insurance ID — server-side paginated.
- View full patient profile: demographics, risk level, active cases, documents, appointment history.
- Soft-delete (deactivate) — `IsActive = 0`; record and audit log preserved.

### API Endpoints
| Method | Route | Role Required | Description |
|---|---|---|---|
| GET | `/api/patients` | All | Paginated search (name, DOB, MRN, insuranceId) |
| GET | `/api/patients/{mrn}` | All | Full patient profile |
| POST | `/api/patients` | CareCoordinator+ | Register new patient |
| PUT | `/api/patients/{mrn}` | CareCoordinator+ | Update demographics |
| DELETE | `/api/patients/{mrn}` | Supervisor+ | Soft-delete (deactivate) |
| GET | `/api/patients/{mrn}/cases` | All | All cases for patient |
| GET | `/api/patients/{mrn}/appointments` | All | Appointment history |
| GET | `/api/patients/{mrn}/documents` | All | Document list |

### Angular Components
- `PatientListComponent` — server-side paginated table, search bar, risk level filter.
- `PatientDetailComponent` — hero card + tabs (Cases, Documents, Appointments, FHIR Export).
- `PatientFormComponent` — create/edit Reactive Form with insurance section.
- `PatientService` — typed HTTP calls.

### DB Tables
`Patients` · `PatientInsurance`

---

## 3. Case Management

**Phase:** 2 | **Status:** In Progress (Backend + Frontend complete; routes use CaseNumber: `/api/cases/{caseNumber}`)

### Features
- Create cases linked to a patient with case type, priority, tags, and lead coordinator.
- Auto-generated case number: `CASE-{YYYY}-{NNNNN}`.
- Status lifecycle: `Draft → Open → InProgress → PendingReview → Closed / Escalated`.
- Server-side state machine validates every transition (invalid transitions return 422).
- Full status change history with timestamp and acting user.
- Multiple concurrent open cases per patient.
- Case tags (e.g., Chronic Disease, Post-Surgery, Mental Health).

### Status Transitions (Allowed)
```
Draft        → Open, Closed
Open         → InProgress, Closed, Escalated
InProgress   → PendingReview, Closed, Escalated
PendingReview→ InProgress, Closed, Escalated
Escalated    → InProgress, Closed
Closed       → (terminal — no transitions)
```

### API Endpoints
| Method | Route | Role Required | Description |
|---|---|---|---|
| GET | `/api/cases` | All | Paginated list (status, priority, coordinator, tag filters) |
| GET | `/api/cases/{caseNumber}` | All | Case detail with team and notes summary |
| POST | `/api/cases` | CareCoordinator+ | Create case |
| PUT | `/api/cases/{caseNumber}` | CareCoordinator+ | Update metadata |
| PUT | `/api/cases/{caseNumber}/status` | CareCoordinator+ | Transition status (validated) |
| GET | `/api/cases/{caseNumber}/history` | All | Status change history |
| GET | `/api/cases/{caseNumber}/notes` | All | Paginated notes |
| POST | `/api/cases/{caseNumber}/notes` | CareCoordinator, Clinician | Add note |
| GET | `/api/cases/{caseNumber}/tasks` | All | Task list |
| POST | `/api/cases/{caseNumber}/tasks` | CareCoordinator+ | Create task |
| GET | `/api/cases/{caseNumber}/documents` | All | Document list |
| GET | `/api/cases/{caseNumber}/team` | All | Care team members |
| POST | `/api/cases/{caseNumber}/team` | Supervisor+ | Add team member |
| DELETE | `/api/cases/{caseNumber}/team/{memberId}` | Supervisor+ | Remove team member |

### Angular Components
- `CaseListComponent` — filterable, paginated table.
- `CaseDetailComponent` — header + 5 tabs: Overview, Notes, Tasks, Documents, Team & History.
- `CaseFormComponent` — create/edit with patient autocomplete, case type, priority, tags.
- `CaseStatusHistoryComponent` — timeline of status changes.
- `CareTeamComponent` — member list with add/remove.
- `CaseService` — typed HTTP calls.

### DB Tables
`Cases` · `CaseTypes` · `CaseTags` · `CaseCaseTags` · `CaseStatusHistory` · `CaseNotes` · `CareTeamMembers`

---

## 4. Case Notes

**Phase:** 2 | **Status:** In Progress (Backend + Frontend complete)

### Features
- Timestamped, author-attributed notes on any case.
- Plain text (rich text is a stretch goal).
- **Append-only after 24 hours** — `IsEditable` flag set to `0` by background `IHostedService`.
- Visible to all care team members assigned to the case.

### Notes
- Notes are stored in `CaseNotes` (not a separate table from Cases).
- Editing a note older than 24h is blocked at the API level (not just UI).
- "Corrections" require adding a new note; old note is preserved.

---

## 5. Task Management

**Phase:** 3 | **Status:** Not Started

### Features
- Tasks created within a case, assigned to any care team member.
- Status lifecycle: `Todo → InProgress → Blocked → Done → Cancelled`.
- Due dates with overdue detection; overdue tasks surface on assignee's dashboard.
- Task completion triggers a notification to the case coordinator.

### Status Transitions
```
Todo      → InProgress, Cancelled
InProgress→ Blocked, Done, Cancelled
Blocked   → InProgress, Cancelled
Done      → (terminal)
Cancelled → (terminal)
```

### API Endpoints
| Method | Route | Role Required | Description |
|---|---|---|---|
| GET | `/api/tasks/my` | Authenticated | Tasks assigned to current user |
| GET | `/api/tasks/{id}` | Authenticated | Task detail |
| PUT | `/api/tasks/{id}` | Authenticated | Update task |
| PUT | `/api/tasks/{id}/status` | Authenticated | Transition status (validated) |
| DELETE | `/api/tasks/{id}` | CareCoordinator+ | Soft-cancel task |

### Angular Components
- `MyTasksComponent` — current user's tasks, overdue highlighted in red.
- `TaskListComponent` — embedded in Case Detail tasks tab.
- `TaskFormComponent` — create/edit with assignee picker and due date picker.
- `TaskStatusChipComponent` — clickable chip triggering transition confirmation.

### DB Tables
`CaseTasks`

---

## 6. Document Management

**Phase:** 3 | **Status:** Not Started

### Features
- Upload PDF, DOCX, PNG, JPG — max 20 MB per file.
- Files stored in Azure Blob Storage (private container `case-documents`).
- Only metadata (name, size, type, uploader) stored in SQL — no blob URL.
- Time-limited SAS download URLs (1 hour max) generated on demand.
- Document categories: `Referral`, `LabResult`, `ConsentForm`, `Insurance`, `Other`.
- Soft-delete — metadata marked `IsDeleted = 1`; blob deletion is a deferred background job.
- Every download is logged to `AuditLogs`.

### API Endpoints
| Method | Route | Role Required | Description |
|---|---|---|---|
| POST | `/api/documents/upload` | CareCoordinator+ | Upload (multipart/form-data) |
| GET | `/api/documents/{id}/download-url` | All | Generate 1-hour SAS URL |
| DELETE | `/api/documents/{id}` | Supervisor+ | Soft-delete |

### Angular Components
- `DocumentListComponent` — table with name, category, uploader, size, date; download button.
- `DocumentUploadComponent` — drag-drop + file picker, category selector, upload progress bar.
- `DocumentService` — `HttpClient` with `reportProgress: true`.

### DB Tables
`Documents`

### Security Notes
- SAS URL is **never** logged to Application Insights.
- Blob container has no public access.
- File type validated by MIME type (not just extension) at API layer.

---

## 7. Appointments & Scheduling

**Phase:** 4 | **Status:** Not Started

### Features
- Schedule appointments linked to a patient (optionally to a case).
- Types: `InitialAssessment`, `FollowUp`, `SpecialistReferral`, `Telehealth`.
- Statuses: `Scheduled → Confirmed → Completed / NoShow / Cancelled`.
- **Conflict detection** — prevents double-booking a provider for the same time slot.
- Provider availability query (returns busy slots for a date range).
- Appointment reminders queued 24h before via background `IHostedService` → `Notifications` rows.

### API Endpoints
| Method | Route | Role Required | Description |
|---|---|---|---|
| GET | `/api/appointments` | All | Paginated list (patient, provider, status, date range) |
| GET | `/api/appointments/{id}` | All | Appointment detail |
| POST | `/api/appointments` | CareCoordinator+ | Schedule (validates conflicts) |
| PUT | `/api/appointments/{id}` | CareCoordinator+ | Reschedule (re-validates conflicts) |
| PUT | `/api/appointments/{id}/status` | CareCoordinator+ | Confirm / Complete / Cancel / NoShow |
| GET | `/api/appointments/providers/{id}/availability` | All | Busy slots for date range |

### Angular Components
- `AppointmentListComponent` — filterable table by patient/provider/status/date.
- `AppointmentFormComponent` — patient picker, provider picker, type, date/time range, conflict warning.
- `AppointmentDetailComponent` — status action buttons.
- `ProviderAvailabilityComponent` — slot grid for provider + date.

### DB Tables
`Providers` · `Appointments`

---

## 8. Care Team

**Phase:** 2 | **Status:** In Progress (Backend + Frontend complete)

### Features
- Each case has one lead coordinator plus optional clinicians and specialists.
- Members can be added or removed; removals are logged in audit.
- A user can be on multiple care teams simultaneously.
- Team roles: `Lead`, `Clinician`, `Specialist`, `Support`.

### DB Tables
`CareTeamMembers`

---

## 9. Dashboard & Reports

**Phase:** 6 | **Status:** Not Started

### Dashboards

| Dashboard | Role | Key Widgets |
|---|---|---|
| Coordinator | CareCoordinator | My open cases by status, overdue tasks, today's appointments, inactive cases (7d) |
| Supervisor | Supervisor | Team caseloads per user, escalated cases, inactive cases |
| Admin | Admin | Active users, logins last 7 days, error rate, system metrics |

### Reports

| Report | Endpoint | Export |
|---|---|---|
| Cases by Status | `GET /api/reports/cases-by-status` | JSON + CSV (`?format=csv`) |
| Case Duration | `GET /api/reports/case-duration` | JSON + CSV |
| Tasks by Assignee | `GET /api/reports/tasks-by-assignee` | JSON + CSV |

### Angular Components
- `CoordinatorDashboardComponent` — stat cards, cases-by-status donut, overdue task list.
- `SupervisorDashboardComponent` — team caseload bar chart, escalated cases table.
- `AdminDashboardComponent` — user counts, login trend chart.
- `CasesByStatusReportComponent` — bar chart + table + CSV download.
- `CaseDurationReportComponent` — bar chart per case type.
- `TasksByAssigneeReportComponent` — stacked bar chart.

### Libraries
- `ng2-charts` + `Chart.js` for all charts.
- `CsvHelper` (.NET) for CSV generation.

---

## 10. Audit & Compliance

**Phase:** 2 (interceptor) + 7 (UI) | **Status:** Not Started

### Features
- Every create/update/delete on Patient, Case, CaseNote, Document, User produces an audit entry.
- Audit log is **immutable** — no DELETE or UPDATE endpoint.
- Captures: entity type, entity ID, action, old value (JSON), new value (JSON), user ID, user email, IP address, timestamp.
- No FK to `Users` — log survives user deletion.
- Admin-only UI: paginated, filterable by entity type, user, date range, action.
- Row expand shows OldValues / NewValues JSON diff.

### API Endpoints
| Method | Route | Role Required | Description |
|---|---|---|---|
| GET | `/api/audit` | Admin | Paginated audit log with filters |
| GET | `/api/audit/{entityType}/{entityId}` | Admin | All entries for one entity |

### DB Tables
`AuditLogs` ← no FK to Users, no DELETE endpoint

---

## 11. Notifications

**Phase:** 7 | **Status:** Not Started

### Features
- In-app notification bell with unread count badge.
- Trigger events: case assigned, task assigned, task overdue, case escalated, appointment reminder.
- Poll `GET /api/notifications/unread-count` every 60 seconds.
- Mark individual or all notifications read.

### API Endpoints
| Method | Route | Description |
|---|---|---|
| GET | `/api/notifications` | Current user's notifications (unread first) |
| GET | `/api/notifications/unread-count` | Badge count only |
| PUT | `/api/notifications/{id}/read` | Mark one read |
| PUT | `/api/notifications/mark-all-read` | Mark all read |

### Angular Components
- `NotificationBellComponent` — icon in top bar, polling, unread badge.
- `NotificationListComponent` — dropdown panel, latest 20, mark-read action.
- `NotificationService` — HTTP calls + 60s polling.

### DB Tables
`Notifications`

---

## 12. Administration — User Management

**Phase:** 2 | **Status:** In Progress (Backend complete — `UsersController` + `UserDataHandler`; Admin UI not started)

### Features
- Admin-only: view all users, create users, assign roles, activate/deactivate.
- Deactivated users cannot log in (`IsActive = false`).
- Role changes are immediate (access tokens expire naturally within 60 min; no token invalidation on role change in Phase 1).

### API Endpoints
| Method | Route | Role Required | Description |
|---|---|---|---|
| GET | `/api/users` | Admin | Paginated user list with filters |
| GET | `/api/users/{id}` | Admin | User detail |
| POST | `/api/users` | Admin | Create user + assign roles |
| PUT | `/api/users/{id}` | Admin | Update user info |
| PUT | `/api/users/{id}/roles` | Admin | Reassign roles |
| PUT | `/api/users/{id}/activate` | Admin | Activate or deactivate |

### Angular Components
- `UserListComponent` — table with role badges, status, last login.
- `UserFormComponent` — create/edit with role multi-select.
- `RoleAssignmentComponent` — drag-drop role assignment.

---

## 13. FHIR / HL7 Integration

**Phase:** 5 | **Status:** Not Started

### Features

#### FHIR R4 REST Facade (`/fhir/R4/`)
- `GET /metadata` — FHIR CapabilityStatement.
- Patient search by `_id`, `name`, `birthdate`, `identifier` (MRN) — returns searchset Bundle.
- Patient read — returns FHIR Patient resource.
- `POST /Patient` — import a FHIR R4 Patient JSON; validate with Firely SDK; map fields; auto-generate MRN; write `FhirImportLogs` + `AuditLogs`; return `201 Created`.
- `GET /Patient/{id}/$everything` — return FHIR Bundle (Patient + Encounters + Appointments + Tasks + DocumentReferences).
- Encounter (Case) read + search.
- Appointment, Task, DocumentReference read.
- All FHIR errors return `OperationOutcome` (not ProblemDetails).

#### Patient Import (Two-Step UI Flow)
1. **Parse & Preview** — client-side validation; field-mapping table shown; no server round-trip.
2. **Confirm Import** — `POST /fhir/R4/Patient`; on `201` navigate to new patient detail.

#### FHIR Resource Mapping
| Internal | FHIR R4 | Key Fields |
|---|---|---|
| `Patient` | `Patient` | MRN → `identifier`, name, birthDate, gender, telecom, address |
| `Case` | `Encounter` | CaseNumber → `identifier`; status: Draft/Open→planned, InProgress→in-progress, Closed→finished |
| `Appointment` | `Appointment` | start, end, serviceType |
| `CaseTask` | `Task` | status, priority, owner, executionPeriod.end (dueDate) |
| `Document` | `DocumentReference` | category, content.attachment (title, contentType) |

#### HL7 v2 (Stretch)
- `POST /api/hl7/adt` — HL7 v2 ADT^A01; parsed by NHapi; upsert Patient.
- `POST /api/hl7/oru` — HL7 v2 ORU^R01; attach as read-only CaseNote.

### API Endpoints
| Method | Route | Description |
|---|---|---|
| GET | `/fhir/R4/metadata` | CapabilityStatement |
| GET | `/fhir/R4/Patient` | Search patients |
| GET | `/fhir/R4/Patient/{id}` | Read patient |
| POST | `/fhir/R4/Patient` | Import patient from FHIR JSON |
| GET | `/fhir/R4/Patient/{id}/$everything` | Full Bundle for patient |
| GET | `/fhir/R4/Encounter` | Search cases |
| GET | `/fhir/R4/Encounter/{id}` | Read case as Encounter |
| GET | `/fhir/R4/Appointment/{id}` | Read appointment |
| GET | `/fhir/R4/Task/{id}` | Read task |
| GET | `/fhir/R4/DocumentReference/{id}` | Read document |

### Angular Components
- `FhirExplorerComponent` — raw FHIR JSON viewer (`ngx-json-viewer`) for any resource.
- `FhirImportComponent` — two-step import modal (paste + preview → confirm).
- `FhirService` — typed HTTP calls to `/fhir/R4/` endpoints.
- **"Export as FHIR Bundle"** button on `PatientDetailComponent` — downloads `patient-{mrn}-fhir-bundle.json`.

### DB Tables
`FhirImportLogs`

### Libraries
- `Hl7.Fhir.R4` NuGet (Firely .NET SDK) — FHIR serialization + validation.
- `ngx-json-viewer` npm package — collapsible JSON tree in Angular.
- `NHapi` NuGet (stretch) — HL7 v2 parsing.

---

## Backlog (Post-Phase 8)

| Feature | Notes |
|---|---|
| SendGrid email notifications | Phase 4 lays groundwork; wire SMTP in Phase 7+ |
| Patient self-service portal | Separate Angular app; requires patient user role |
| SMART on FHIR auth | Azure AD OIDC + SMART app launch; out of scope for portfolio |
| Full bi-directional EHR sync | Phase 5 covers read/export/import; real-time sync out of scope |
| Multi-tenancy | Schema-per-tenant or row-level security |
| Azure AD SSO | Replace JWT with Azure AD OIDC |
| Real-time notifications via SignalR | Replace 60s polling |
| Mobile PWA | Progressive Web App manifest + offline |
| Physical blob deletion job | Azure Function nightly cleanup of soft-deleted blobs |
| Two-factor authentication | TOTP or SMS |
| Document versioning | Track document versions with history |
