using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HCM.API.Controllers;
using HCM.API.Tests.Fixtures;
using HCM.Application.Auth.DTOs;
using Xunit;

namespace HCM.API.Tests.Auth;

// Each test class gets its own ApiFactory (own InMemory DB instance via a fresh Guid dbName),
// so the failed-login-lockout test below can't bleed into other classes' login tests.
public class AuthFlowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthFlowTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_WithSeededAdminCredentials_ReturnsTokensAndRoles()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "Admin@123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task Login_WithBadPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ThenAccessProtectedRoute_Succeeds()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "Admin@123!"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var protectedResponse = await client.GetAsync("/api/cases");

        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithoutToken_OnProtectedRoute_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cases");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksAccountAndSixthAttemptStaysUnauthorized()
    {
        // Uses a demo user (not admin) so this test's lockout doesn't affect other admin-login tests
        // sharing the same class fixture / InMemory database instance.
        var client = _factory.CreateClient();

        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("sarah.jones", "WrongPassword!"));

        var sixthAttempt = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("sarah.jones", "Demo@123!"));

        // Even the correct password is rejected once locked out.
        sixthAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_IsNotYetImplemented_ReturnsUnauthorized()
    {
        // Documents a known gap: AuthController.Refresh is a TODO stub that always returns 401.
        // See docs/testing-and-audit/integrationtesting.md — full auth flow assumes this works.
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest("some-refresh-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
