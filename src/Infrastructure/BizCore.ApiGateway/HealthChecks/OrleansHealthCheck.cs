using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orleans;

namespace BizCore.ApiGateway.Services;

public class OrleansHealthCheck : IHealthCheck
{
    private readonly IClusterClient _clusterClient;

    public OrleansHealthCheck(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_clusterClient.IsInitialized)
            {
                return HealthCheckResult.Unhealthy("Orleans cluster client is not initialized");
            }

            // Try to get a simple grain to test connectivity
            var testGrain = _clusterClient.GetGrain<ITestGrain>(Guid.NewGuid());
            await testGrain.PingAsync();

            return HealthCheckResult.Healthy("Orleans cluster is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Orleans cluster is unhealthy: {ex.Message}");
        }
    }
}

public interface ITestGrain : IGrainWithGuidKey
{
    Task<string> PingAsync();
}

public class TestGrain : Grain, ITestGrain
{
    public Task<string> PingAsync()
    {
        return Task.FromResult("Pong");
    }
}