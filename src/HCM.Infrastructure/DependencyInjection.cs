using HCM.Application.Interfaces;
using HCM.Infrastructure.BackgroundJobs;
using HCM.Infrastructure.BackgroundServices;
using HCM.Infrastructure.Identity;
using HCM.Infrastructure.Persistence;
using HCM.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace HCM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
            ));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IFhirMappingService, FhirMappingService>();

        services.AddHostedService<CaseNoteEditLockService>();
        services.AddHostedService<NotificationOverdueService>();

        return services;
    }
}
