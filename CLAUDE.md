# HCMS — Project Memory for Claude

> Update this file whenever a rule changes, a phase completes, or a key decision is made.
> Claude reads this at the start of every session.

---

## What This Project Is

**Healthcare Case Management System (HCMS)** — a cloud-native, HIPAA-aligned portfolio project that lets care coordinators, clinicians, and administrators manage patient cases from intake through discharge.

This is a portfolio project. No real patient data is ever stored.

---

## Stack & Repository Layout

See [docs/architecture.md](docs/architecture.md) — tech stack table, repo folder map, layer diagram, Azure services, CI/CD pipeline.

---

## Key Commands

### .NET API
```bash
# Restore and build
dotnet restore && dotnet build

# Run locally (reads appsettings.Development.json + .env via DotNetEnv)
dotnet run --project src/HCM.API

# Apply EF Core migrations
dotnet ef database update --project src/HCM.Infrastructure --startup-project src/HCM.API

# Add a new migration
dotnet ef migrations add <MigrationName> --project src/HCM.Infrastructure --startup-project src/HCM.API

# Run tests
dotnet test

# Publish
dotnet publish src/HCM.API -c Release -o ./publish
```

### Angular
```bash
# Install dependencies
npm ci

# Dev server (proxies /api → http://localhost:5001)
ng serve

# Production build
ng build --configuration production

# Run unit tests
ng test --watch=false

# Lint
ng lint

# Check bundle size
ng build --configuration production --stats-json
npx webpack-bundle-analyzer dist/hcm-web/stats.json
```

### Azure / Azurite (local dev)
```bash
# Start Azurite blob emulator (for Document Management)
azurite --silent --location .azurite --debug .azurite/debug.log

# Azure CLI login (for Key Vault access locally via DefaultAzureCredential)
az login

# List Key Vault secrets
az keyvault secret list --vault-name hcm-dev-kv
```

### dotnet user-secrets (recommended for local JWT secret)
```bash
cd src/HCM.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "your-dev-secret-here"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=..."
```
## Code Style Guidelines

### General C#
- Write code that prioritizes readability over cleverness
- Use clear, descriptive variable and function names (no single-letter vars except loop counters)
- Add a comment above every function explaining what it does, its inputs, and what it returns
- Add inline comments for any logic that isn't immediately obvious
- Break complex operations into small, named helper functions
- Prefer explicit over implicit — avoid magic numbers, use named constants
- Group related code together and separate groups with a blank line and a section comment

### DataHandlers (Business Logic & Validation)
- **DataHandlers combine validation + business logic** in a single class — no separate Command/Handler pattern
  - Create an interface (`IAuthDataHandler`) and implementation (`AuthDataHandler`) for each feature's handler
  - DataHandler lives in `Application/[Feature]/DataHandlers/` folder with public async methods for each operation
  - Example interface: `public interface IAuthDataHandler { Task<AuthResponseDto> LoginAsync(string username, string password); }`
  - Example implementation: `public class AuthDataHandler : IAuthDataHandler { public async Task<AuthResponseDto> LoginAsync(...) {...} }`
  - Validate within the handler method; throw `ValidationException` for validation errors
  - Use private helper methods to keep logic readable (e.g., `private async Task RecordFailedAttempt(...)`)
- **Controllers stay thin:** Inject the handler interface; only deserialize request → call handler → return response
  - No business logic in controllers, only orchestration
  - Example: `public AuthController(IAuthDataHandler authHandler) => _authHandler = authHandler;`
- **Register interfaces in DependencyInjection.cs:** `services.AddScoped<IAuthDataHandler, AuthDataHandler>();`
  - Enables easy mocking for unit tests without requiring a test-specific implementation
  - One registration per interface; reuse same handler for multiple related operations (login, refresh, logout)
---

## Hard Rules (Always Follow)

### Security
- **No secrets in source code, ever.** All secrets go in Azure Key Vault (prod) or `dotnet user-secrets` / `appsettings.Development.json` (local dev). `.env` is gitignored.
- **No raw SQL.** Use EF Core parameterized queries only. Raw SQL is blocked in PRs.
- **JWT secret ≥ 32 characters.** 
- **SAS URLs expire in 1 hour max** (FR-25). Never log SAS URLs to Application Insights.
- **No PHI in logs.** Log entity IDs only — never names, DOB, SSN or insurance numbers.
- **Blob container is always private.** No public access on `case-documents`.

### Data Integrity
- **Soft deletes only** on Patients and Documents. No hard deletes. `IsActive`/`IsDeleted` flags.
- **Audit log is immutable.** No DELETE or UPDATE endpoint on `AuditLogs`. No FK to `Users` (survives user deletion).
- **Case notes are append-only after 24 hours.** `IsEditable` set to `0` by background job.
- **Case status transitions are validated server-side** even if the UI enforces them too.

### Architecture
- **Clean Architecture layer rules:** Domain has no dependencies. Application depends only on Domain. Infrastructure implements Application interfaces. API depends on Application (not Infrastructure directly, except DI root).
- **DataHandlers for all use cases.** Controllers call DataHandlers; DataHandlers contain business logic + validation.
- **No MediatR.** DataHandlers are injected directly into controllers and called as scoped dependencies.
- **One migration owner per branch.** Squash migrations before merging to avoid conflicts.

### Angular
- **Lazy-loaded feature modules** for every feature area.
- **`HasRoleDirective`** for hiding UI elements by role — never rely solely on route guards.
- **Reactive Forms** throughout — no template-driven forms.
- **NgRx** for auth state and notifications. Component-level RxJS for local UI state.
- **`localStorage`** for JWT; `sessionStorage` option available for sensitive roles.

### Format Conventions
- **MRN format:** `MRN-{YYYY}-{NNNNN}` (e.g. `MRN-2026-00001`)
- **Case number format:** `CASE-{YYYY}-{NNNNN}` (e.g. `CASE-2026-00001`)
- **Azure resource naming:** `hcm-{env}-{resource}` (e.g. `hcm-prod-api`, `hcm-prod-kv`)
- **Environments:** `dev` (local) · `staging` (Azure Free/Basic) · `prod` (Azure Standard)
- **FHIR base path:** `/fhir/R4/` (distinct from `/api/`)
- **API error format:** RFC 7807 ProblemDetails for REST errors; FHIR OperationOutcome for `/fhir/R4/` errors.

---

## RBAC Roles (Least Privilege)

| Role | Key Permissions |
|---|---|
| `Admin` | Full access including user management, audit log, system config |
| `Supervisor` | All coordinator permissions + reassign cases, approve escalations, supervisor dashboard |
| `CareCoordinator` | Create/manage cases, add notes, assign tasks, upload documents, schedule appointments |
| `Clinician` | View assigned cases, add notes, view documents, update task status |
| `ReadOnly` | View-only on patients, cases, and documents |

---

## Phase HTML Reports (Required at Phase Completion)

At the end of every development phase, generate a self-contained HTML report saved as `docs/phase-<N>-<short-title>.html`.

**Report requirements:**
- **Self-contained** — inline CSS and JS, no external dependencies (CDN links are forbidden)
- **Navigable** — sticky sidebar TOC with jump links, active-section highlighting via IntersectionObserver
- **Collapsible sections** — use `<details>`/`<summary>` for implementation detail blocks
- **Diagrams** — inline SVG for architecture, sequence, state machine, and component tree diagrams. Only add diagrams where they genuinely aid understanding; skip trivial points
- **Real-life analogies** — for every non-obvious concept or design decision, include a short real-world analogy that makes it intuitive for a non-expert reader
- **Content** — what was built, why, key decisions and trade-offs, files/modules added or changed, how the pieces fit together, what's next

**Auto-tracking hook (already wired):**  
`.claude/settings.json` has a `PostToolUse(Write)` hook that fires `.claude/scripts/update-phase-index.ps1` whenever a `phase-N-*.html` file is written. This appends a row to `.phase-reports-index.md` (gitignored, local only) with: timestamp, phase number, filename, one-line summary, and a relative link.

**Do not** add entries to `.phase-reports-index.md` manually — the hook handles it.

---

## Current Phase

**Phase 2 — Patient & Case Core** (In Progress — backend + frontend complete, E2E testing pending)

See [docs/project-status.md](docs/project-status.md) for the current detailed status.

---

## Linked Documents

| Document | Purpose | Update Frequency |
|---|---|---|
| [docs/product-requirements.md](docs/product-requirements.md) | Full FR/NFR list, DB schema, API contracts, Azure architecture | When requirements change |
| [docs/phase-tracker.md](docs/phase-tracker.md) | Phase-by-phase task checklist | As tasks complete |
| [docs/architecture.md](docs/architecture.md) | System design, layers, conventions, diagrams | When architecture changes |
| [docs/changelog.md](docs/changelog.md) | What changed and when | Every merge to `main` |
| [docs/project-status.md](docs/project-status.md) | Current phase, done/blocked/next | Weekly or after each session |
| [docs/feature-reference.md](docs/feature-reference.md) | Module-by-module feature index with FR numbers | When features are added/changed |
| [prototype/index.html](prototype/index.html) | Static UI prototype (reference only) | Do not modify |
