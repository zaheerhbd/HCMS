# HCMS — Project Status

**Last Updated:** 2026-06-23
**Current Phase:** Pre-Phase (Setup & Planning)
**Overall Progress:** 0 / 185 tasks complete

> Update this file at the start of each working session. It should answer: where are we, what's blocking us, what's next.

---

## Phase Overview

| # | Phase | Status | Tasks | Done | Remaining |
|---|---|---|---|---|---|
| 1 | Foundation & Infrastructure | `Not Started` | ~29 | 0 | 29 |
| 2 | Patient & Case Core | `Not Started` | ~38 | 0 | 38 |
| 3 | Documents, Tasks & Notes | `Not Started` | ~22 | 0 | 22 |
| 4 | Appointments & Scheduling | `Not Started` | ~16 | 0 | 16 |
| 5 | FHIR/HL7 Integration | `Not Started` | ~24 | 0 | 24 |
| 6 | Dashboards & Reports | `Not Started` | ~20 | 0 | 20 |
| 7 | Notifications & Audit UI | `Not Started` | ~14 | 0 | 14 |
| 8 | Testing, Security & Polish | `Not Started` | ~22 | 0 | 22 |

---

## Current Phase Detail — Phase 1: Foundation & Infrastructure

**Goal:** A deployable, authenticated skeleton. Login works end-to-end in Azure. CI/CD pipeline is green.

### Status: Not Started

### What's Needed to Begin

- [ ] Scaffold .NET solution with Clean Architecture projects
- [ ] Scaffold Angular app (`ng new hcm-web`)
- [ ] Provision Azure staging resources (Resource Group, App Service, SQL, Key Vault, etc.)
- [ ] Create GitHub Actions workflows

### Backend Checklist (Phase 1)

- [ ] Solution: `HCM.Domain`, `HCM.Application`, `HCM.Infrastructure`, `HCM.API`
- [ ] `appsettings.json` + environment overrides
- [ ] Azure Key Vault via `DefaultAzureCredential`
- [ ] EF Core + SQL Server provider + `ApplicationDbContext`
- [ ] `Users`, `Roles`, `UserRoles`, `RefreshTokens` entities + first migration
- [ ] JWT auth: `AuthController` (`/login`, `/refresh`, `/logout`)
- [ ] Refresh token rotation with sliding expiration
- [ ] Account lockout after 5 failed attempts
- [ ] Global exception handler → RFC 7807 ProblemDetails
- [ ] Serilog → Application Insights
- [ ] `/health` endpoint
- [ ] Rate limiting on `/api/auth/login` (10 req/min per IP)
- [ ] Database seed: Admin user + all roles

### Frontend Checklist (Phase 1)

- [ ] Scaffold Angular project (`ng new hcm-web --routing --style=scss`)
- [ ] Install Angular Material
- [ ] `CoreModule` + `SharedModule` setup
- [ ] `AuthModule` — Login page with Reactive Form
- [ ] `AuthService` — login, logout, token storage, refresh-on-401
- [ ] `JwtInterceptor` — attach Bearer token
- [ ] `ErrorInterceptor` — API errors → toasts
- [ ] `AuthGuard` + `RoleGuard`
- [ ] `AppShellComponent` — sidebar nav, top bar, notification placeholder
- [ ] Placeholder stubs for all feature modules
- [ ] Environment files: `environment.ts`, `environment.prod.ts`

### Infrastructure Checklist (Phase 1)

- [ ] GitHub branch protection on `main` (require PR + passing CI)
- [ ] Azure staging resources provisioned (Resource Group, App Service Plan, App Service, SQL, Key Vault, App Insights, Storage, SWA)
- [ ] Managed Identity on App Service + Key Vault Secrets User role granted
- [ ] Secrets stored in Key Vault (DB connection string, JWT secret)
- [ ] GitHub Actions: `build-api.yml`
- [ ] GitHub Actions: `build-angular.yml`
- [ ] CORS configured
- [ ] `.github/dependabot.yml`

### Definition of Done — Phase 1

- [ ] User can log in on the deployed staging URL
- [ ] JWT refresh works transparently (no logout on token expiry)
- [ ] GitHub Actions pipeline is green on push to `main`
- [ ] `/health` returns 200 from staging App Service
- [ ] No secrets in source code (verified by GitHub secret scanning)

---

## Blockers

_(none — project hasn't started yet)_

---

## Decisions Made

| Date | Decision | Rationale |
|---|---|---|
| 2026-06-22 | JWT + refresh token (not Azure AD) | Sufficient for portfolio; avoids Azure AD cost and complexity |
| 2026-06-22 | Soft deletes only (no hard deletes) | HIPAA-aligned audit integrity |
| 2026-06-22 | Firely SDK for FHIR (not hand-rolled) | Official .NET FHIR SDK; handles serialization + validation |
| 2026-06-22 | EF Core only (no raw SQL) | Prevents SQL injection; easier migrations |
| 2026-06-22 | Angular Material as UI library | Enterprise look + WCAG 2.1 AA built-in |
| 2026-06-22 | MediatR for CQRS | Clean separation of concerns; pipeline behaviors for validation and logging |
| 2026-06-23 | .env deferred (not native to .NET/Angular) | .NET uses appsettings.Development.json; Angular uses environment.ts; .env.example kept for reference and CI/CD |

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
