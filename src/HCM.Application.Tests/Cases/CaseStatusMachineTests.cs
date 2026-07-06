using FluentAssertions;
using FluentValidation;
using HCM.Application.Cases.DataHandlers;
using HCM.Application.Cases.DTOs;
using HCM.Application.Notifications.DataHandlers;
using HCM.Application.Tests.Helpers;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HCM.Application.Tests.Cases;

public class CaseStatusMachineTests
{
    private static async Task<(HCM.Infrastructure.Persistence.ApplicationDbContext Db, CaseDataHandler Sut, string CaseNumber)> SeedOpenCase()
    {
        var db = TestDbContextFactory.Create();
        var notifications = new NotificationDataHandler(db);
        var sut = new CaseDataHandler(db, notifications);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            MRN = "MRN-2026-00001",
            FirstName = "Test",
            LastName = "Patient",
            DateOfBirth = new DateTime(1985, 5, 5),
            CreatedBy = Guid.NewGuid()
        };
        var caseType = new CaseType { Id = 1, Name = "Chronic Disease", IsActive = true };
        db.Patients.Add(patient);
        db.CaseTypes.Add(caseType);
        await db.SaveChangesAsync();

        var created = await sut.CreateCaseAsync(patient.MRN, new CreateCaseDto { CaseTypeId = caseType.Id }, Guid.NewGuid(), Guid.Empty);
        return (db, sut, created.CaseNumber);
    }

    // Mirrors CaseDataHandler.IsValidStatusTransition exactly, so a change to the
    // production state machine must be a deliberate edit here too.
    [Theory]
    [InlineData(CaseStatus.Open, CaseStatus.InProgress, true)]
    [InlineData(CaseStatus.Open, CaseStatus.OnHold, true)]
    [InlineData(CaseStatus.Open, CaseStatus.Closed, true)]
    [InlineData(CaseStatus.InProgress, CaseStatus.OnHold, true)]
    [InlineData(CaseStatus.InProgress, CaseStatus.Closed, true)]
    [InlineData(CaseStatus.OnHold, CaseStatus.InProgress, true)]
    [InlineData(CaseStatus.OnHold, CaseStatus.Closed, true)]
    [InlineData(CaseStatus.Closed, CaseStatus.Reopened, true)]
    [InlineData(CaseStatus.Reopened, CaseStatus.InProgress, true)]
    [InlineData(CaseStatus.Open, CaseStatus.Reopened, false)]
    [InlineData(CaseStatus.Closed, CaseStatus.InProgress, false)]
    [InlineData(CaseStatus.Closed, CaseStatus.Open, false)]
    public async Task ChangeCaseStatusAsync_ValidatesTransition(string from, string to, bool isValid)
    {
        var (db, sut, caseNumber) = await SeedOpenCase();
        await using var _ = db;

        // Drive the case to the "from" state first (Open is the natural starting state).
        if (from != CaseStatus.Open)
        {
            var caseEntity = await db.Cases.FirstAsync(c => c.CaseNumber == caseNumber);
            caseEntity.CurrentStatus = from;
            await db.SaveChangesAsync();
        }

        if (isValid)
        {
            var result = await sut.ChangeCaseStatusAsync(caseNumber, new CaseStatusChangeDto { NewStatus = to }, Guid.NewGuid());
            result.CurrentStatus.Should().Be(to);
        }
        else
        {
            var act = async () => await sut.ChangeCaseStatusAsync(caseNumber, new CaseStatusChangeDto { NewStatus = to }, Guid.NewGuid());
            await act.Should().ThrowAsync<ValidationException>();
        }
    }

    [Fact]
    public async Task CreateCaseAsync_GeneratesCaseNumberInExpectedFormat()
    {
        var (db, _, caseNumber) = await SeedOpenCase();
        await using var _ = db;

        caseNumber.Should().MatchRegex(@"^CASE-\d{4}-\d{5}$");
    }

    [Fact]
    public async Task ChangeCaseStatusAsync_ToClosed_SetsClosedAtTimestamp()
    {
        var (db, sut, caseNumber) = await SeedOpenCase();
        await using var _ = db;

        var result = await sut.ChangeCaseStatusAsync(caseNumber, new CaseStatusChangeDto { NewStatus = CaseStatus.Closed }, Guid.NewGuid());

        result.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeCaseStatusAsync_RecordsStatusHistoryEntry()
    {
        var (db, sut, caseNumber) = await SeedOpenCase();
        await using var _ = db;

        await sut.ChangeCaseStatusAsync(caseNumber, new CaseStatusChangeDto { NewStatus = CaseStatus.InProgress, Comment = "Starting work" }, Guid.NewGuid());

        var history = await db.CaseStatusHistories.Where(h => h.ToStatus == CaseStatus.InProgress).ToListAsync();
        history.Should().ContainSingle(h => h.Comment == "Starting work" && h.FromStatus == CaseStatus.Open);
    }

    [Fact]
    public async Task CloseCaseAsync_WhenAlreadyClosed_ThrowsValidationException()
    {
        var (db, sut, caseNumber) = await SeedOpenCase();
        await using var _ = db;
        await sut.CloseCaseAsync(caseNumber, Guid.NewGuid());

        var act = async () => await sut.CloseCaseAsync(caseNumber, Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }
}
