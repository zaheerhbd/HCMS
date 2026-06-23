Scaffold a new MediatR **Query** (read operation) in the HCMS Clean Architecture solution.

Query name: **$ARGUMENTS**

Follow these rules exactly:

## What to create

1. **`src/HCM.Application/Features/<Feature>/Queries/<QueryName>/`**
   - `<QueryName>Query.cs` — record implementing `IRequest<Result<T>>`
   - `<QueryName>QueryHandler.cs` — class implementing `IRequestHandler<>`
   - `<QueryName>Dto.cs` — response DTO (flat, no EF entities exposed outside Application layer)

## Conventions

- Namespace: `HCM.Application.Features.<Feature>.Queries.<QueryName>`
- Queries are read-only — no writes, no side effects.
- Return `Result<T>` where `T` is the DTO or a list DTO. Use `Result.Failure("Not found")` for missing records — do not throw `NotFoundException` unless it's already the project pattern.
- Use `IApplicationDbContext` with `.AsNoTracking()` on all reads.
- No validators needed for simple queries; add one only if the query has filter parameters that need validation.
- No PHI in log messages — log entity IDs only.
- Map entities to DTOs inside the handler (AutoMapper or manual mapping — match existing project pattern).

## Hard rules (from CLAUDE.md)

- Application depends only on Domain.
- No EF entity types in DTOs returned to the API layer.
- No raw SQL.

Infer the `<Feature>` folder from the query name (e.g., `GetPatientByIdQuery` → `Features/Patients/Queries/GetPatientById/`).

After creating the files, show the full file tree and remind me to add the controller GET endpoint if this is a new route.
