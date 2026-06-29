using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HCM.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await db.Database.MigrateAsync();
            await SeedRolesAsync(db);
            await SeedAdminUserAsync(db);
            await SeedCaseTypesAsync(db);
            await SeedCaseTagsAsync(db);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(ApplicationDbContext db)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName))
                db.Roles.Add(new Role { Name = roleName });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Username == "admin"))
            return;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@hcms.local",
            FirstName = "System",
            LastName = "Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123!"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var adminRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Admin);
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();
    }

    private static async Task SeedCaseTypesAsync(ApplicationDbContext db)
    {
        var types = new[]
        {
            "Chronic Disease",
            "Post-Surgery",
            "Mental Health",
            "Preventive",
            "Behavioral"
        };

        foreach (var name in types)
        {
            if (!await db.CaseTypes.AnyAsync(ct => ct.Name == name))
                db.CaseTypes.Add(new CaseType { Name = name, IsActive = true, CreatedAt = DateTime.UtcNow });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedCaseTagsAsync(ApplicationDbContext db)
    {
        var tags = new[]
        {
            "Urgent",
            "High Priority",
            "Pediatric",
            "Geriatric",
            "Chronic Condition",
            "Post-Surgery",
            "Mental Health",
            "Preventive Care",
            "Follow-Up Required",
            "Complex Case"
        };

        foreach (var name in tags)
        {
            if (!await db.CaseTags.AnyAsync(t => t.Name == name))
                db.CaseTags.Add(new CaseTag { Name = name, IsActive = true, CreatedAt = DateTime.UtcNow });
        }

        await db.SaveChangesAsync();
    }
}
