# Phase 1 — Foundation & Infrastructure

**Goal:** A deployable, authenticated skeleton. Login works end-to-end in Azure. CI/CD pipeline is green. Nothing else.

**Duration:** 2 weeks  
**Status:** `[x]` Complete  
**Last Updated:** 2026-06-27

---

## Backend Tasks

- [x] Create solution with Clean Architecture projects: `HCM.Domain`, `HCM.Application`, `HCM.Infrastructure`, `HCM.API`
- [x] Configure `appsettings.json` with environment-specific overrides (`appsettings.Development.json`, `appsettings.Production.json`)
- [-] Wire Azure Key Vault via `DefaultAzureCredential` (deferred — tenant mismatch; using App Service Application Settings instead)
- [x] Add EF Core with SQL Server provider; create initial `ApplicationDbContext`
- [x] Create `Users`, `Roles`, `UserRoles`, `RefreshTokens` entities and first migration
- [x] Implement JWT authentication: `AuthController` with `/login`, `/refresh`, `/logout`
- [x] Implement refresh token rotation with sliding expiration
- [x] Add account lockout after 5 failed login attempts
- [x] Implement global exception handler middleware returning RFC 7807 ProblemDetails
- [x] Add request logging middleware (Serilog → Application Insights)
- [x] Add `/health` endpoint checking DB connectivity
- [x] Add rate limiting on `/api/auth/login` (10 req/min per IP)
- [x] Seed database: Admin user, all roles

## Frontend Tasks

- [x] Scaffold Angular project with `ng new hcm-web --routing --style=scss`
- [x] Install and configure Angular Material
- [x] Set up core services (standalone pattern — Angular 19 uses functional guards/interceptors)
- [x] Implement `LoginComponent` (standalone): Login page with Reactive Form
- [x] Implement `AuthService`: login, logout, token storage, refresh on 401
- [x] Implement `jwtInterceptor`: attach Bearer token + auto-refresh on 401
- [x] Implement `errorInterceptor`: map API errors to Material snack bar toasts
- [x] Implement `authGuard` and `roleGuard`
- [x] Create `AppShellComponent`: sidebar nav, top bar, notification placeholder
- [x] Create placeholder route-guarded stubs for all feature modules (empty components)
- [x] Configure environment files: `environment.ts`, `environment.prod.ts`

## Infrastructure Tasks

- [ ] Configure GitHub branch protection on `main` (require passing CI before merge)
- [x] Provision Azure prod resources: Resource Group, App Service Plan, App Service (.NET 8 Linux), Azure SQL Server + DB, Application Insights, Static Web Apps
- [-] Configure Managed Identity + Key Vault Secrets User role (deferred — Key Vault skipped)
- [-] Store secrets in Key Vault (deferred — secrets in App Service Application Settings instead)
- [x] Create GitHub Actions workflow: `build-api.yml` — restore, build, test, publish, deploy to staging App Service
- [x] Create GitHub Actions workflow: `build-angular.yml` — npm ci, lint, test, build prod, deploy to Static Web Apps
- [x] Configure CORS on API to accept Angular SWA origin
- [x] Add `.github/dependabot.yml` for dependency update PRs
- [x] Add `.gitignore` entries: `appsettings.*.json` (local overrides), `*.user`, `bin/`, `obj/`, `node_modules/`

## Definition of Done

- [ ] User can log in on the deployed staging URL
- [ ] JWT refresh works transparently (no logout on token expiry)
- [ ] GitHub Actions pipeline is green on push to `main`
- [ ] `/health` returns 200 from staging App Service
- [ ] No secrets in source code (verified by GitHub secret scanning)
