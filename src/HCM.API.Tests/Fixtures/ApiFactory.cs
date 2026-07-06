using HCM.Application.Interfaces;
using HCM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HCM.API.Tests.Fixtures;

// Boots the real HCM.API pipeline (auth, middleware, controllers) against an isolated
// InMemory database instead of SQL Server, and a fake blob service instead of Azure.
// Docker/Testcontainers aren't available in this environment, so InMemory stands in for
// "real DB" here — DataSeeder branches on provider (see DataSeeder.IsInMemory check).
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    // Jwt:Secret is read eagerly by Program.cs (a plain top-level statement) before
    // ConfigureWebHost's ConfigureAppConfiguration hook is applied at builder.Build() time.
    // Setting env vars in the constructor guarantees they're already present when
    // WebApplication.CreateBuilder(args) picks up its configuration sources — otherwise
    // the token gets signed with one secret (this override) and validated against another
    // (whatever real dev-machine user-secret happens to be configured), and every request
    // fails auth with a bare 401.
    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-signing-secret-that-is-32-chars-min");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "HCMS");
        Environment.SetEnvironmentVariable("Jwt__Audience", "HCMS-Users");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:4200");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();
        });
    }
}
