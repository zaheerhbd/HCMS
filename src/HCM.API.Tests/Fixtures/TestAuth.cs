using System.Net.Http.Headers;
using System.Net.Http.Json;
using HCM.API.Controllers;
using HCM.Application.Auth.DTOs;
using HCM.Application.Interfaces;
using HCM.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HCM.API.Tests.Fixtures;

// Mints JWTs directly via the real ITokenService for roles DataSeeder doesn't provision
// (e.g. ReadOnly). JWT bearer auth only validates the token itself, not a DB round-trip,
// so a signed token for a fabricated user is sufficient to exercise [Authorize(Roles=...)].
public static class TestAuth
{
    public static string MintToken(ApiFactory factory, string role, string username = "test.user")
    {
        var tokenService = factory.Services.CreateScope().ServiceProvider.GetRequiredService<ITokenService>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = $"{username}@hcms.local",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "unused",
            CreatedAt = DateTime.UtcNow,
        };
        return tokenService.GenerateAccessToken(user, [role]);
    }

    // Logs in through the real /api/auth/login endpoint using a DataSeeder-provisioned account
    // and returns an HttpClient with the Bearer token already attached.
    public static async Task<HttpClient> LoginAsClientAsync(ApiFactory factory, string username, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }
}
