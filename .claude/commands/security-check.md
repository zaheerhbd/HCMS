Run a HCMS security audit against the current working changes.

## What to check

Use `git diff HEAD` and read any recently modified files, then verify each item below. Report ✅ pass or ❌ FAIL with file:line for each.

### Secrets & Credentials
- [ ] No secrets, API keys, or connection strings hardcoded in any `.cs`, `.ts`, `.json`, or `.yml` file (excluding `.env` which is gitignored)
- [ ] No JWT secrets in `appsettings.json` or `appsettings.Development.json` (must use `dotnet user-secrets` or env vars)
- [ ] `.env` is listed in `.gitignore`

### SQL & Data Access
- [ ] No raw SQL strings (`ExecuteSqlRaw`, `FromSqlRaw` without parameters, string interpolation in SQL)
- [ ] All queries go through EF Core parameterized methods

### Authentication & Authorization
- [ ] JWT secret is ≥ 32 characters (check any config values visible in code)
- [ ] Every new controller has `[Authorize]` or explicit `[AllowAnonymous]` — no unguarded endpoints
- [ ] Role checks use `[Authorize(Roles = "...")]` or `HasRoleDirective` — no string comparisons in business logic

### PHI / Logging
- [ ] No patient names, DOB, SSN, MRN, or insurance numbers in any `_logger.Log*()` calls
- [ ] Only entity IDs logged (PatientId, CaseId, DocumentId, etc.)
- [ ] No SAS URLs logged to Application Insights

### Blob Storage
- [ ] SAS token expiry is ≤ 1 hour (`SasTokenExpiryHours ≤ 1`)
- [ ] Blob container access is `Private` — no `PublicAccessType.Blob` or `PublicAccessType.Container`

### Soft Deletes
- [ ] No `context.Patients.Remove()` or `context.Documents.Remove()` — soft delete via `IsActive`/`IsDeleted` only
- [ ] No hard-delete endpoint on `AuditLogs`

### Angular
- [ ] No secrets or bearer tokens hardcoded in `.ts` or `environment.ts` files
- [ ] `environment.prod.ts` has `enableDebugTools: false`

## Output format

List every check with ✅ or ❌. For failures, show the file path and line number. At the end, give a one-line summary: "X issues found" or "All checks passed."
