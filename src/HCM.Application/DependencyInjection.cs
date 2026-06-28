using HCM.Application.Auth.DataHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace HCM.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all DataHandlers (one per feature/aggregate)
        services.AddScoped<IAuthDataHandler, AuthDataHandler>();
        // TODO: Add other feature DataHandlers (IPatientDataHandler, ICaseDataHandler, etc.)

        return services;
    }
}
