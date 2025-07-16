using BizCore.Monitoring.Models;

namespace BizCore.Monitoring.Interfaces;

/// <summary>
/// Performance monitoring and analysis service interface
/// </summary>
public interface IPerformanceService
{
    /// <summary>
    /// Record performance counter
    /// </summary>
    Task<MonitoringResult> RecordPerformanceCounterAsync(PerformanceCounter counter);

    /// <summary>
    /// Record multiple performance counters in batch
    /// </summary>
    Task<MonitoringResult> RecordPerformanceCountersBatchAsync(List<PerformanceCounter> counters);

    /// <summary>
    /// Get performance counters
    /// </summary>
    Task<IEnumerable<PerformanceCounter>> GetPerformanceCountersAsync(string category, string? instance = null, DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// Get system performance metrics
    /// </summary>
    Task<Dictionary<string, object>> GetSystemPerformanceAsync(string serviceName, string? instance = null);

    /// <summary>
    /// Get application performance insights
    /// </summary>
    Task<Dictionary<string, object>> GetApplicationPerformanceAsync(string serviceName, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Analyze performance trends
    /// </summary>
    Task<Dictionary<string, object>> AnalyzePerformanceTrendsAsync(string serviceName, TimeSpan period, string metricName);

    /// <summary>
    /// Get performance anomalies
    /// </summary>
    Task<IEnumerable<Dictionary<string, object>>> GetPerformanceAnomaliesAsync(string serviceName, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Generate performance report
    /// </summary>
    Task<Dictionary<string, object>> GeneratePerformanceReportAsync(string serviceName, DateTime startTime, DateTime endTime, string reportType = "summary");

    /// <summary>
    /// Get performance baselines
    /// </summary>
    Task<Dictionary<string, double>> GetPerformanceBaselinesAsync(string serviceName, List<string> metricNames);

    /// <summary>
    /// Set performance baseline
    /// </summary>
    Task<bool> SetPerformanceBaselineAsync(string serviceName, string metricName, double baselineValue);

    /// <summary>
    /// Compare performance periods
    /// </summary>
    Task<Dictionary<string, object>> ComparePerformancePeriodsAsync(string serviceName, DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End);

    /// <summary>
    /// Get resource utilization
    /// </summary>
    Task<Dictionary<string, object>> GetResourceUtilizationAsync(string serviceName, string? instance = null, DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// Predict performance issues
    /// </summary>
    Task<IEnumerable<Dictionary<string, object>>> PredictPerformanceIssuesAsync(string serviceName, TimeSpan predictionWindow);
}