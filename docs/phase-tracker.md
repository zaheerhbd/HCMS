# Healthcare Case Management System — Phase Tracker

**Project:** Enterprise Healthcare Case Management System (Portfolio)  
**Last Updated:** 2026-06-29  
**Status Key:** `[ ]` Not Started · `[~]` In Progress · `[x]` Complete · `[-]` Deferred

---

## Phase Overview

| Phase | Name | Duration | Status | Deliverable | Details |
|---|---|---|---|---|---|
| 1 | Foundation & Infrastructure | 2 weeks | `[x]` | Deployable skeleton with auth | [View Phase 1](phases/phase-1.md) |
| 2 | Patient & Case Core | 3 weeks | `[~]` | Full case lifecycle working | [View Phase 2](phases/phase-2.md) |
| 3 | Documents, Tasks & Notes | 2 weeks | `[ ]` | Blob upload + task management | [View Phase 3](phases/phase-3.md) |
| 4 | Appointments & Scheduling | 2 weeks | `[ ]` | Calendar and booking | [View Phase 4](phases/phase-4.md) |
| 5 | FHIR/HL7 Integration | 2 weeks | `[ ]` | FHIR R4 API, resource mapping, Bundle export/import | [View Phase 5](phases/phase-5.md) |
| 6 | Dashboard & Reports | 2 weeks | `[ ]` | All dashboards + CSV export | [View Phase 6](phases/phase-6.md) |
| 7 | Notifications & Audit UI | 1 week | `[ ]` | Notification bell + audit log viewer | [View Phase 7](phases/phase-7.md) |
| 8 | Testing, Security & Polish | 2 weeks | `[ ]` | Production-ready quality bar | [View Phase 8](phases/phase-8.md) |

**Total estimated duration:** ~16 weeks (part-time) / ~8 weeks (full-time)

---

## Progress Summary

| Phase | Tasks Total | Complete | Remaining | % Complete |
|---|---|---|---|---|
| Phase 1 — Foundation | ~29 | 29 | 0 | 100% |
| Phase 2 — Patient & Case Core | ~38 | 34 | 4 | 89% |
| Phase 3 — Documents & Tasks | ~22 | 0 | 22 | 0% |
| Phase 4 — Appointments | ~16 | 0 | 16 | 0% |
| Phase 5 — FHIR/HL7 Integration | ~24 | 0 | 24 | 0% |
| Phase 6 — Dashboards & Reports | ~20 | 0 | 20 | 0% |
| Phase 7 — Notifications & Audit UI | ~14 | 0 | 14 | 0% |
| Phase 8 — Testing & Polish | ~22 | 0 | 22 | 0% |
| **Total** | **~185** | **63** | **122** | **34%** |

_Update task counts as tasks are added or split during development._

---

## Quick Links

- **Phase Details:** Click "View Phase X" in the table above to see full task lists and definitions of done
- **Overall Status:** See [docs/project-status.md](project-status.md) for current blockers and decisions
- **Architecture:** See [docs/architecture.md](architecture.md) for technical design
- **Feature Reference:** See [docs/feature-reference.md](feature-reference.md) for module-by-module feature index

---

## Backlog (Post-Phase 7)

These items are intentionally deferred to keep the project shippable within the 16-week plan.

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
