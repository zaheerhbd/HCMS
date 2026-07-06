using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HCM.API.Tests.Fixtures;
using HCM.Application.Cases.DTOs;
using HCM.Application.Patients.DTOs;
using HCM.Domain.Entities;
using Xunit;

namespace HCM.API.Tests.Cases;

public class CaseStateMachineTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CaseStateMachineTests(ApiFactory factory) => _factory = factory;

    private static async Task<string> CreatePatientAsync(HttpClient client, string first, string last)
    {
        var dto = new CreatePatientDto { FirstName = first, LastName = last, DateOfBirth = new DateTime(1975, 6, 1) };
        var created = await (await client.PostAsJsonAsync("/api/patients", dto)).Content.ReadFromJsonAsync<PatientDto>();
        return created!.MRN;
    }

    [Fact]
    public async Task CreateCase_ThenValidTransitions_Succeed()
    {
        var client = await TestAuth.LoginAsClientAsync(_factory, "sarah.jones", "Demo@123!");
        var mrn = await CreatePatientAsync(client, "Elena", "Rossi");

        var created = await (await client.PostAsJsonAsync(
            $"/api/cases?patientMrn={mrn}", new CreateCaseDto { CaseTypeId = 1, Notes = "Initial intake" }))
            .Content.ReadFromJsonAsync<CaseDto>();

        created!.CaseNumber.Should().MatchRegex(@"^CASE-\d{4}-\d{5}$");
        created.CurrentStatus.Should().Be(CaseStatus.Open);

        var toInProgress = await client.PostAsJsonAsync(
            $"/api/cases/{created.CaseNumber}/status", new CaseStatusChangeDto { NewStatus = CaseStatus.InProgress });
        toInProgress.StatusCode.Should().Be(HttpStatusCode.OK);

        var toClosed = await client.PostAsJsonAsync(
            $"/api/cases/{created.CaseNumber}/status", new CaseStatusChangeDto { NewStatus = CaseStatus.Closed });
        toClosed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidTransition_Returns400()
    {
        var client = await TestAuth.LoginAsClientAsync(_factory, "sarah.jones", "Demo@123!");
        var mrn = await CreatePatientAsync(client, "Marco", "Bianchi");
        var created = await (await client.PostAsJsonAsync(
            $"/api/cases?patientMrn={mrn}", new CreateCaseDto { CaseTypeId = 1 }))
            .Content.ReadFromJsonAsync<CaseDto>();

        // Open -> Reopened is not a valid transition (Reopened only follows Closed)
        var response = await client.PostAsJsonAsync(
            $"/api/cases/{created!.CaseNumber}/status", new CaseStatusChangeDto { NewStatus = CaseStatus.Reopened });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCase_AsReadOnlyRole_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestAuth.MintToken(_factory, "ReadOnly"));

        var response = await client.PostAsJsonAsync(
            "/api/cases?patientMrn=MRN-2026-00001", new CreateCaseDto { CaseTypeId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeStatus_AsClinicianRole_Returns403()
    {
        // Clinician is not in the "Admin,CareCoordinator,Supervisor" allow-list for status changes
        var setupClient = await TestAuth.LoginAsClientAsync(_factory, "sarah.jones", "Demo@123!");
        var mrn = await CreatePatientAsync(setupClient, "Luca", "Conti");
        var created = await (await setupClient.PostAsJsonAsync(
            $"/api/cases?patientMrn={mrn}", new CreateCaseDto { CaseTypeId = 1 }))
            .Content.ReadFromJsonAsync<CaseDto>();

        var clinicianClient = await TestAuth.LoginAsClientAsync(_factory, "dr.patel", "Demo@123!");
        var response = await clinicianClient.PostAsJsonAsync(
            $"/api/cases/{created!.CaseNumber}/status", new CaseStatusChangeDto { NewStatus = CaseStatus.InProgress });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
