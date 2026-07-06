using HCM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Tests.Helpers;

// Reuses the real ApplicationDbContext (entity configurations, keys, relationships)
// but swaps SQL Server for EF Core's InMemory provider — real mapping, no real DB.
public static class TestDbContextFactory
{
    // Each call gets an isolated, uniquely named InMemory database so tests never leak state into each other.
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
