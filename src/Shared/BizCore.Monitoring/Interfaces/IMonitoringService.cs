using BizCore.Monitoring.Models;

namespace BizCore.Monitoring.Interfaces;

/// <summary>
/// Core monitoring service interface
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Record metric data
    /// </summary>
    Task<MonitoringResult> RecordMetricAsync(CreateMetricRequest request);

    /// <summary>
    /// Record multiple metrics in batch
    /// </summary>
    Task<MonitoringResult> RecordMetricsBatchAsync(List<CreateMetricRequest> requests);

    /// <summary>
    /// Query metrics
    /// </summary>
    Task<QueryResult> QueryMetricsAsync(MonitoringQuery query);

    /// <summary>
    /// Get metric by ID
    /// </summary>
    Task<Metric?> GetMetricAsync(string metricId);

    /// <summary>
    /// Get metrics by filters
    /// </summary>
    Task<IEnumerable<Metric>> GetMetricsAsync(Dictionary<string, string> filters, int skip = 0, int take = 100);

    /// <summary>
    /// Delete metric
    /// </summary>
    Task<bool> DeleteMetricAsync(string metricId);

    /// <summary>
    /// Get metric names for tenant
    /// </summary>
    Task<IEnumerable<string>> GetMetricNamesAsync(string tenantId);

    /// <summary>
    /// Get metric statistics
    /// </summary>
    Task<Dictionary<string, object>> GetMetricStatisticsAsync(string metricName, DateTime startTime, DateTime endTime, string tenantId);
}