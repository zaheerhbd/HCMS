using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HCM.API.Tests.Fixtures;
using Xunit;

namespace HCM.API.Tests.Rbac;

// Cross-checks docs/testing-and-audit/rbacverification.md against the live [Authorize] attributes.
public class RbacVerificationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RbacVerificationTests(ApiFactory factory) => _factory = factory;

    private HttpClient ClientWithRole(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestAuth.MintToken(_factory, role));
        return client;
    }

    [Theory]
    [InlineData("ReadOnly", HttpStatusCode.OK)]
    [InlineData("CareCoordinator", HttpStatusCode.OK)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task GetPatients_Search_AllRolesAllowed(string role, HttpStatusCode expected)
    {
        var client = ClientWithRole(role);

        var response = await client.GetAsync("/api/patients/search");

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData("ReadOnly", HttpStatusCode.Forbidden)]
    [InlineData("CareCoordinator", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task GetAuditLogs_OnlyAdminAllowed(string role, HttpStatusCode expected)
    {
        var client = ClientWithRole(role);

        var response = await client.GetAsync("/api/audit");

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData("ReadOnly", HttpStatusCode.Forbidden)]
    [InlineData("CareCoordinator", HttpStatusCode.Created)]
    [InlineData("Admin", HttpStatusCode.Created)]
    public async Task PostPatients_AdminAndCareCoordinatorAllowed(string role, HttpStatusCode expected)
    {
        var client = ClientWithRole(role);

        var response = await client.PostAsJsonAsync("/api/patients", new HCM.Application.Patients.DTOs.CreatePatientDto
        {
            FirstName = "Rbac",
            LastName = $"Test-{role}",
            DateOfBirth = new DateTime(1980, 1, 1)
        });

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithNoToken_Returns401NotForbidden()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/audit");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
