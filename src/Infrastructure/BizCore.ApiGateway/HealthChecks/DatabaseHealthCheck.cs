using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BizCore.ApiGateway.Services;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public DatabaseHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, this would check actual database connections
            // For now, we'll simulate a database check
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Degraded("Database connection string is not configured");
            }

            // Simulate database connectivity check
            await Task.Delay(10, cancellationToken);

            return HealthCheckResult.Healthy("Database is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Database is unhealthy: {ex.Message}");
        }
    }
}