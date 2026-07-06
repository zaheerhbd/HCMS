# Backend — Unit Tests

**Pattern:** Unit tests verify DataHandler logic in isolation — Moq the `IApplicationDbContext` interface (or individual `DbSet`s). No DB, no HTTP, fast feedback. Think of it like testing a recipe without a real kitchen — you swap real ingredients with predictable fakes.

## Project Structure to Create

```
src/
  HCM.Application.Tests/         ← new xUnit project
    Auth/
      AuthDataHandlerTests.cs
    Patients/
      PatientDataHandlerTests.cs
    Cases/
      CaseDataHandlerTests.cs
      CaseStatusMachineTests.cs
    Documents/
      DocumentDataHandlerTests.cs
    Notifications/
      NotificationDataHandlerTests.cs
    Helpers/
      MockDbContext.cs           ← shared Moq setup
```

```bash
# Create project
dotnet new xunit -n HCM.Application.Tests -o src/HCM.Application.Tests
dotnet add src/HCM.Application.Tests package Moq
dotnet add src/HCM.Application.Tests package FluentAssertions
dotnet sln add src/HCM.Application.Tests
```

## DataHandlers to Cover

| DataHandler | Coverage | Effort |
|---|---|---|
| `AuthDataHandler` — LoginAsync, RefreshAsync, LogoutAsync | Mock: IApplicationDbContext, IPasswordHasher, IJwtService · Verify: account lockout increments, JWT issued on valid creds, locked account returns error | High |
| `PatientDataHandler` — CreateAsync, UpdateAsync, SearchAsync, SoftDeleteAsync | Mock: DbContext · Verify: MRN format `MRN-{YYYY}-{NNNNN}`, duplicate name search works, soft delete sets `IsActive=false` | Medium |
| `CaseDataHandler` — status state machine transitions (valid + invalid) | Table-driven tests: Draft→Open (valid), Open→Closed (invalid, throws), Draft→Escalated (invalid). Case number format `CASE-{YYYY}-{NNNNN}` | High |
| `DocumentDataHandler` — metadata write, SAS URL 1-hour expiry | Mock IBlobStorageService · Verify SAS expiry ≤ 1h, blob URLs not logged, soft delete sets `IsDeleted=true` | Medium |
| `NotificationDataHandler` — create, markRead, markAllRead, unreadCount | Verify count decrements, duplicate-day dedup logic in overdue service | Low |
| `FhirDataHandler` — ParseAndPreview, ConfirmImport, ImportTransactionBundle | Mock Firely Serializer, verify OperationOutcome returned for invalid JSON, `urn:uuid:` resolution, FhirImportLog written | High |

## Example: CaseDataHandler state machine test

```csharp
public class CaseStatusMachineTests
{
    private readonly Mock<IApplicationDbContext> _ctx;
    private readonly CaseDataHandler _sut;

    public CaseStatusMachineTests()
    {
        _ctx = new Mock<IApplicationDbContext>();
        _sut = new CaseDataHandler(_ctx.Object);
    }

    [Theory]
    [InlineData("Draft",       "Open",          true)]
    [InlineData("Open",        "InProgress",    true)]
    [InlineData("InProgress",  "PendingReview", true)]
    [InlineData("Draft",       "Closed",        false)]  // invalid
    [InlineData("Closed",      "Open",          false)]  // invalid
    public async Task ValidateTransition(
        string from, string to, bool isValid)
    {
        var @case = CaseFixture.Build(status: from);
        _ctx.SetupDbSet(@case);

        if (isValid)
        {
            var result = await _sut.TransitionStatusAsync(@case.CaseNumber, to);
            result.Status.Should().Be(to);
        }
        else
        {
            var act = () => _sut.TransitionStatusAsync(@case.CaseNumber, to);
            await act.Should().ThrowAsync<ValidationException>();
        }
    }
}
```
