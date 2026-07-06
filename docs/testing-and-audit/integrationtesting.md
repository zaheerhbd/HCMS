# Backend — Integration Tests

**Analogy:** Unit tests are like testing each LEGO brick in isolation. Integration tests assemble the full set and verify the structure holds — real DB, real HTTP stack, real middleware. Testcontainers spins up a throwaway SQL Server in Docker for each test run.

Flow: `xUnit Test → WebApplicationFactory → Testcontainers SQL Server (Docker) → Mock Blob`

- Each test class gets a fresh DB via EF migrations applied at startup.
- Blob Storage replaced by `IBlob` mock — avoids Azurite dependency.

## Project Structure to Create

```
src/
  HCM.API.Tests/                 ← new xUnit project
    Fixtures/
      ApiFactory.cs              ← WebApplicationFactory with Testcontainers
      DbSeedHelper.cs
    Auth/
      AuthFlowTests.cs           ← login → access → refresh → logout
    Patients/
      PatientCrudTests.cs
    Cases/
      CaseStateMachineTests.cs
    Documents/
      DocumentUploadTests.cs     ← mock IBlobStorageService
```

```bash
# Create project
dotnet new xunit -n HCM.API.Tests -o src/HCM.API.Tests
dotnet add src/HCM.API.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add src/HCM.API.Tests package Testcontainers.MsSql
dotnet add src/HCM.API.Tests package FluentAssertions
dotnet sln add src/HCM.API.Tests
```

## Integration Test Scenarios

| Scenario | Detail | Effort |
|---|---|---|
| Auth flow: login → access protected route → refresh → logout | `POST /api/auth/login` → 200 + tokens; `GET /api/patients` (with token) → 200; `POST /api/auth/refresh` → new token; `POST /api/auth/logout` → 204 | High |
| Auth security: 5 failed logins → account locked | Loop 5 bad-password calls → 6th returns 423 Locked. Verify AuditLog entry written | Medium |
| Patient CRUD via HTTP: create, read, search, soft-delete | `POST` → 201 with MRN-YYYY-NNNNN; `GET /{mrn}` → 200; `GET ?name=…` → paginated; `DELETE` → 200 sets `IsActive=false` (GET still returns record) | Medium |
| Case state machine: valid transitions succeed, invalid return 422 | Create case (Draft) → transition to Open → InProgress → PendingReview → Closed. Then attempt Draft→Closed → 422 with ProblemDetails | High |
| Document upload and SAS URL generation (mock blob) | `POST multipart/form-data` → 201; GET download URL → SAS URL with expiry ≤ 1h. `IBlobStorageService` mocked via DI override in ApiFactory | Medium |
| RBAC: ReadOnly JWT → POST endpoints return 403 | Mint a ReadOnly-role token in test; verify `POST /api/patients`, `POST /api/cases` all return 403 Forbidden | Low |

## Example: ApiFactory with Testcontainers

```csharp
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real DB with Testcontainer connection string
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseSqlServer(_db.GetConnectionString()));

            // Replace blob with mock
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton(Mock.Of<IBlobStorageService>(b =>
                b.GetSasUrl(It.IsAny<string>()) ==
                "https://blob.test/file?sv=2024&se=+1h"));
        });
    }

    public new async Task DisposeAsync() => await _db.DisposeAsync();
}
```
