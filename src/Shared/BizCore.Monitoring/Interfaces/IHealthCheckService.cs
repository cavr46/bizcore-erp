using BizCore.Monitoring.Models;

namespace BizCore.Monitoring.Interfaces;

/// <summary>
/// Health check and system monitoring service interface
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Register health check
    /// </summary>
    Task<MonitoringResult> RegisterHealthCheckAsync(string name, string description, HealthCheckConfiguration configuration);

    /// <summary>
    /// Unregister health check
    /// </summary>
    Task<bool> UnregisterHealthCheckAsync(string name);

    /// <summary>
    /// Execute health check
    /// </summary>
    Task<HealthCheck> ExecuteHealthCheckAsync(string name);

    /// <summary>
    /// Execute all health checks
    /// </summary>
    Task<IEnumerable<HealthCheck>> ExecuteAllHealthChecksAsync(string? tenantId = null);

    /// <summary>
    /// Get health check by name
    /// </summary>
    Task<HealthCheck?> GetHealthCheckAsync(string name);

    /// <summary>
    /// Get health checks for service
    /// </summary>
    Task<IEnumerable<HealthCheck>> GetHealthChecksAsync(string serviceName, string? tenantId = null);

    /// <summary>
    /// Get overall system health
    /// </summary>
    Task<Dictionary<string, object>> GetSystemHealthAsync(string? tenantId = null);

    /// <summary>
    /// Get health check history
    /// </summary>
    Task<IEnumerable<HealthCheck>> GetHealthCheckHistoryAsync(string name, DateTime? startTime = null, DateTime? endTime = null, int take = 100);

    /// <summary>
    /// Get health statistics
    /// </summary>
    Task<Dictionary<string, object>> GetHealthStatisticsAsync(string? serviceName = null, string? tenantId = null, DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// Set health check status manually
    /// </summary>
    Task<bool> SetHealthCheckStatusAsync(string name, HealthStatus status, string? message = null);
}