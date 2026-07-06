# README & Demo Video

## README.md Sections Required

```markdown
# Healthcare Case Management System (HCMS)

## Architecture Diagram
[inline diagram or link to docs/architecture.md]

## Tech Stack
Angular 19 · .NET 8 · EF Core · SQL Server · Azure Blob · FHIR R4

## Local Setup
1. Prerequisites: .NET 8 SDK, Node 20, SQL Server / LocalDB, Docker (for tests)
2. Clone + restore: `dotnet restore && npm ci`
3. Apply migrations: `dotnet ef database update ...`
4. Run API: `dotnet run --project src/HCM.API`
5. Run Angular: `ng serve` (proxies /api → localhost:5001)

## Environment Variables
| Variable | Where | Description |
| Jwt__Secret | dotnet user-secrets | ≥ 32 chars |
| ConnectionStrings__DefaultConnection | appsettings.Development.json | SQL Server |

## Test Credentials (local dev)
admin / Admin@123! (full access)

## Screenshots
[login, dashboard, patient list, case detail, FHIR explorer]

## Live Demo
[link to Azure SWA] · [5-min demo video link]
```

## Tasks

| Task | Detail | Effort |
|---|---|---|
| Update README.md — architecture diagram, setup steps, env var guide, screenshots | Add 5 screenshots: Login, Dashboard (Coordinator), Patient List, Case Detail, FHIR Explorer. Inline or hosted in `docs/screenshots/` | Medium |
| Record 5-minute demo walkthrough video | Script: Login → Dashboard → Create Patient → Open Case → Upload Document → FHIR Export → Admin Dashboard. Upload to YouTube (unlisted) and link from README | Medium |
