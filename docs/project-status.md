# HCMS — Project Status

**Last Updated:** 2026-06-24
**Current Phase:** Phase 1 — Foundation & Infrastructure (code complete — Azure provisioning pending)
**Overall Progress:** 29 / 185 tasks complete

> **Local dev confirmed working:** API starts, DB created, migrations applied, seed runs. Run with `dotnet run` from `src/HCM.API`. Login: `admin` / `Admin@123!`.

> Update this file at the start of each working session. It should answer: where are we, what's blocking us, what's next.

---

## Phase Overview

| # | Phase | Status | Tasks | Done | Remaining |
|---|---|---|---|---|---|
| 1 | Foundation & Infrastructure | `Complete` | ~29 | 29 | 0 |
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

### Status: Complete (code done — Azure provisioning pending)

### Backend Checklist (Phase 1)

- [x] Solution: `HCM.Domain`, `HCM.Application`, `HCM.Infrastructure`, `HCM.API`
- [x] `appsettings.json` + environment overrides
- [ ] Azure Key Vault via `DefaultAzureCredential` ← needs Azure resources provisioned
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
- [ ] Azure prod resources provisioned ← run az CLI commands in Blockers section
- [ ] Managed Identity on App Service + Key Vault Secrets User role assigned
- [ ] Secrets stored in Key Vault (DB connection string, JWT secret)
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE` secret added to GitHub repo
- [ ] `AZURE_STATIC_WEB_APPS_API_TOKEN` secret added to GitHub repo
- [x] GitHub Actions: `build-api.yml`
- [x] GitHub Actions: `build-angular.yml`
- [x] CORS configured (in `Program.cs` via `appsettings.json`)
- [x] `.github/dependabot.yml`

### Definition of Done — Phase 1

- [ ] User can log in on the deployed prod URL (`https://hcm-prod-api.azurewebsites.net`) ← blocked on Azure provisioning
- [ ] JWT refresh works transparently (no logout on token expiry)
- [ ] GitHub Actions pipeline is green on push to `main`
- [ ] `/health` returns 200 from prod App Service
- [ ] Angular SPA loads from Static Web App URL
- [ ] No secrets in source code (verified by GitHub secret scanning)

---

## Blockers

- **Azure provisioning not done** — Key Vault, App Service, SQL Server, and SWA need to be provisioned. Run the az CLI commands below in order, then add the two GitHub secrets, then push to `main` to trigger CI/CD.

```bash
# 1. Resource group (single prod environment)
az group create --name hcm-prod-rg --location eastus

# 2. App Service Plan + Web App (.NET 8 on Linux)
az appservice plan create --name hcm-prod-plan --resource-group hcm-prod-rg --sku B1 --is-linux
az webapp create --name hcm-prod-api --resource-group hcm-prod-rg --plan hcm-prod-plan --runtime "DOTNETCORE:8.0"

# 3. Azure SQL Server + Database
az sql server create --name hcm-prod-sql --resource-group hcm-prod-rg --location eastus --admin-user hcmadmin --admin-password <STRONG_PASSWORD>
az sql db create --name HcmsDb --server hcm-prod-sql --resource-group hcm-prod-rg --edition Basic

# Allow Azure services to reach SQL Server
az sql server firewall-rule create --name AllowAzureServices --server hcm-prod-sql --resource-group hcm-prod-rg --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# 4. Key Vault
az keyvault create --name hcm-prod-kv --resource-group hcm-prod-rg --location eastus

# 5. Application Insights
az monitor app-insights component create --app hcm-prod-ai --location eastus --resource-group hcm-prod-rg --kind web

# 6. Enable Managed Identity on App Service
az webapp identity assign --name hcm-prod-api --resource-group hcm-prod-rg

# 7. Grant App Service identity access to Key Vault
PRINCIPAL_ID=$(az webapp identity show --name hcm-prod-api --resource-group hcm-prod-rg --query principalId -o tsv)
az keyvault set-policy --name hcm-prod-kv --object-id $PRINCIPAL_ID --secret-permissions get list

# 8. Store secrets in Key Vault (replace <...> placeholders)
az keyvault secret set --vault-name hcm-prod-kv --name "ConnectionStrings--DefaultConnection" \
  --value "Server=hcm-prod-sql.database.windows.net;Database=HcmsDb;User Id=hcmadmin;Password=<STRONG_PASSWORD>;Encrypt=True;"
az keyvault secret set --vault-name hcm-prod-kv --name "Jwt--Secret" \
  --value "<RANDOM_64_CHAR_SECRET>"

# 9. Wire Key Vault URI into App Service config
KV_URI=$(az keyvault show --name hcm-prod-kv --resource-group hcm-prod-rg --query properties.vaultUri -o tsv)
az webapp config appsettings set --name hcm-prod-api --resource-group hcm-prod-rg \
  --settings KeyVaultUri=$KV_URI ASPNETCORE_ENVIRONMENT=Production

# 10. Static Web App (Angular)
az staticwebapp create --name hcm-prod-swa --resource-group hcm-prod-rg --location eastus
```

**After provisioning — add GitHub repo secrets:**

| Secret name | Where to get the value |
|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Azure Portal → `hcm-prod-api` → Overview → **Get publish profile** → copy XML |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Azure Portal → `hcm-prod-swa` → Overview → **Manage deployment token** |

Add both at: **GitHub repo → Settings → Secrets and variables → Actions → New repository secret**

Then push to `main` — both CI/CD workflows will trigger automatically.

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
| 2026-06-24 | Single prod environment only (no staging) | Portfolio project — staging adds cost and complexity with no benefit; deploy directly from `main` to prod (`hcm-prod-rg`). Azure resources use `hcm-prod-*` naming. |

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
