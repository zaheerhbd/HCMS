using FluentAssertions;
using FluentValidation;
using HCM.Application.Patients.DataHandlers;
using HCM.Application.Patients.DTOs;
using HCM.Application.Tests.Helpers;
using Xunit;

namespace HCM.Application.Tests.Patients;

public class PatientDataHandlerTests
{
    private static CreatePatientDto ValidPatient(string first = "John", string last = "Smith") => new()
    {
        FirstName = first,
        LastName = last,
        DateOfBirth = new DateTime(1990, 1, 1),
        Gender = "Male",
        Email = "john.smith@example.com"
    };

    [Fact]
    public async Task CreatePatientAsync_GeneratesMrnInExpectedFormat()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new PatientDataHandler(db);

        var result = await sut.CreatePatientAsync(ValidPatient(), Guid.NewGuid());

        result.MRN.Should().MatchRegex(@"^MRN-\d{4}-\d{5}$");
        result.MRN.Should().StartWith($"MRN-{DateTime.UtcNow.Year}-00001");
    }

    [Fact]
    public async Task CreatePatientAsync_SecondPatientInSameYear_IncrementsSequence()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new PatientDataHandler(db);

        var first = await sut.CreatePatientAsync(ValidPatient("Alice", "Anders"), Guid.NewGuid());
        var second = await sut.CreatePatientAsync(ValidPatient("Bob", "Baker"), Guid.NewGuid());

        first.MRN.Should().EndWith("00001");
        second.MRN.Should().EndWith("00002");
    }

    [Fact]
    public async Task CreatePatientAsync_WithFutureDateOfBirth_ThrowsValidationException()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new PatientDataHandler(db);
        var dto = ValidPatient();
        dto.DateOfBirth = DateTime.UtcNow.AddDays(1);

        var act = async () => await sut.CreatePatientAsync(dto, Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreatePatientAsync_WithInvalidEmail_ThrowsValidationException()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new PatientDataHandler(db);
        var dto = ValidPatient();
        dto.Email = "not-an-email";

        var act = async () => await sut.CreatePatientAsync(dto, Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreatePatientAsync_WithMissingLastName_ThrowsValidationException()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new PatientDataHandler(db);
        var dto = ValidPatient(last: "  ");

        var act = async () => await sut.CreatePatientAsync(dto, Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DeletePatientAsync_SetsIsActiveFalse_SoftDelete()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new PatientDataHandler(db);
        var created = await sut.CreatePatientAsync(ValidPatient(), Guid.NewGuid());

        await sut.DeletePatientAsync(created.MRN);

        var stored = await db.Patients.FindAsync(created.Id);
        stored!.IsActive.Should().BeFalse();

        // Soft-deleted patients are excluded from lookups (HIPAA: no hard deletes, but not "active" either)
        var act = async () => await sut.GetPatientByMrnAsync(created.MRN);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SearchPatientsAsync_FiltersByNameCaseInsensitively()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new PatientDataHandler(db);
        await sut.CreatePatientAsync(ValidPatient("Maria", "Garcia"), Guid.NewGuid());
        await sut.CreatePatientAsync(ValidPatient("Tom", "Nguyen"), Guid.NewGuid());

        var result = await sut.SearchPatientsAsync("garcia");

        result.TotalCount.Should().Be(1);
        result.Results.Single().FullName.Should().Be("Maria Garcia");
    }

    [Fact]
    public async Task GetPatientByMrnAsync_WithUnknownMrn_ThrowsKeyNotFoundException()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new PatientDataHandler(db);

        var act = async () => await sut.GetPatientByMrnAsync("MRN-2026-99999");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
