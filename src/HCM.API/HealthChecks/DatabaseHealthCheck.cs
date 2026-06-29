using HCM.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HCM.API.HealthChecks;

/// <summary>
/// Simple health check that verifies the database is accessible and responding.
/// Used by load balancers and monitoring systems to detect if the app is alive.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;

    // Constructor: Inject the database context
    public DatabaseHealthCheck(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Check if the database is healthy.
    /// Called periodically by the health check system.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to execute a simple query against the database
            // If this succeeds, the database is up and responding
            var canConnectToDb = await _dbContext.Database.CanConnectAsync(cancellationToken);

            // If database connection succeeds, return Healthy status
            if (canConnectToDb)
            {
                return HealthCheckResult.Healthy("Database is accessible and responding normally.");
            }

            // If connection fails, return Unhealthy status
            return HealthCheckResult.Unhealthy("Database connection failed.");
        }
        catch (Exception exception)
        {
            // If any error occurs during the health check, return Unhealthy with the error message
            return HealthCheckResult.Unhealthy(
                description: $"Database health check failed: {exception.Message}",
                exception: exception
            );
        }
    }
}
