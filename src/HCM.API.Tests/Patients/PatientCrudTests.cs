using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HCM.API.Tests.Fixtures;
using HCM.Application.Patients.DTOs;
using Xunit;

namespace HCM.API.Tests.Patients;

public class PatientCrudTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PatientCrudTests(ApiFactory factory) => _factory = factory;

    private static CreatePatientDto NewPatientDto(string first, string last) => new()
    {
        FirstName = first,
        LastName = last,
        DateOfBirth = new DateTime(1988, 3, 14),
        Gender = "Female",
        Email = $"{first.ToLower()}.{last.ToLower()}@example.com"
    };

    [Fact]
    public async Task CreatePatient_AsCareCoordinator_Returns201WithGeneratedMrn()
    {
        var client = await TestAuth.LoginAsClientAsync(_factory, "sarah.jones", "Demo@123!");

        var response = await client.PostAsJsonAsync("/api/patients", NewPatientDto("Grace", "Hopper"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PatientDto>();
        created!.MRN.Should().MatchRegex(@"^MRN-\d{4}-\d{5}$");
    }

    [Fact]
    public async Task CreatePatient_AsReadOnlyRole_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestAuth.MintToken(_factory, "ReadOnly"));

        var response = await client.PostAsJsonAsync("/api/patients", NewPatientDto("Ada", "Lovelace"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPatientByMrn_AfterCreate_ReturnsSamePatient()
    {
        var client = await TestAuth.LoginAsClientAsync(_factory, "sarah.jones", "Demo@123!");
        var created = await (await client.PostAsJsonAsync("/api/patients", NewPatientDto("Katherine", "Johnson")))
            .Content.ReadFromJsonAsync<PatientDto>();

        var response = await client.GetAsync($"/api/patients/{created!.MRN}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await response.Content.ReadFromJsonAsync<PatientDto>();
        fetched!.LastName.Should().Be("Johnson");
    }

    [Fact]
    public async Task SearchPatients_ByName_ReturnsPaginatedMatch()
    {
        var client = await TestAuth.LoginAsClientAsync(_factory, "sarah.jones", "Demo@123!");
        await client.PostAsJsonAsync("/api/patients", NewPatientDto("Rosalind", "Franklin"));

        var response = await client.GetAsync("/api/patients/search?search=Franklin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PatientSearchDto>();
        result!.Results.Should().Contain(p => p.FullName == "Rosalind Franklin");
    }

    [Fact]
    public async Task DeletePatient_AsAdmin_SoftDeletesAndSubsequentGetReturns404()
    {
        var client = await TestAuth.LoginAsClientAsync(_factory, "admin", "Admin@123!");
        var created = await (await client.PostAsJsonAsync("/api/patients", NewPatientDto("Marie", "Curie")))
            .Content.ReadFromJsonAsync<PatientDto>();

        var deleteResponse = await client.DeleteAsync($"/api/patients/{created!.MRN}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDelete = await client.GetAsync($"/api/patients/{created.MRN}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePatient_AsCareCoordinator_Returns403()
    {
        var client = await TestAuth.LoginAsClientAsync(_factory, "sarah.jones", "Demo@123!");
        var created = await (await client.PostAsJsonAsync("/api/patients", NewPatientDto("Chien-Shiung", "Wu")))
            .Content.ReadFromJsonAsync<PatientDto>();

        var response = await client.DeleteAsync($"/api/patients/{created!.MRN}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
