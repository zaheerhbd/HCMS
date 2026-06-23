Add a new EF Core migration to the HCMS solution.

Migration name: **$ARGUMENTS**

## Steps

1. **Verify the model change is in place** — read the relevant entity in `src/HCM.Domain/Entities/` and the `ApplicationDbContext` in `src/HCM.Infrastructure/Persistence/` to confirm the change I want to migrate actually exists in code.

2. **Show me the exact command to run** (do not run it — I will run it myself):
   ```
   dotnet ef migrations add $ARGUMENTS \
     --project src/HCM.Infrastructure \
     --startup-project src/HCM.API
   ```

3. **After I confirm the migration was created**, remind me to:
   - Review the generated `Up()` and `Down()` methods in `src/HCM.Infrastructure/Migrations/` before applying.
   - Apply with: `dotnet ef database update --project src/HCM.Infrastructure --startup-project src/HCM.API`
   - Check that `Down()` correctly reverses the change (critical for rollback safety).

## Hard rules

- Soft deletes only — never add a `DELETE` cascade that would hard-delete Patients, Documents, or AuditLogs.
- Audit log table (`AuditLogs`) must have no FK to `Users` — it must survive user deletion.
- One migration owner per branch — warn me if there are already uncommitted migrations from another branch.
- No raw SQL in migration `Up()`/`Down()` unless absolutely necessary (e.g., seeding data); if raw SQL is used, use parameterized form.
