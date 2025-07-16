using BizCore.Monitoring.Models;

namespace BizCore.Monitoring.Interfaces;

/// <summary>
/// Distributed tracing service interface
/// </summary>
public interface ITracingService
{
    /// <summary>
    /// Start new trace span
    /// </summary>
    Task<TraceSpan> StartSpanAsync(string operationName, string? parentSpanId = null, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Finish trace span
    /// </summary>
    Task FinishSpanAsync(string spanId, SpanStatus status = SpanStatus.Ok, string? errorMessage = null);

    /// <summary>
    /// Add log to span
    /// </summary>
    Task AddSpanLogAsync(string spanId, string level, string message, Dictionary<string, object>? fields = null);

    /// <summary>
    /// Add tag to span
    /// </summary>
    Task AddSpanTagAsync(string spanId, string key, string value);

    /// <summary>
    /// Get trace by ID
    /// </summary>
    Task<IEnumerable<TraceSpan>> GetTraceAsync(string traceId);

    /// <summary>
    /// Get span by ID
    /// </summary>
    Task<TraceSpan?> GetSpanAsync(string spanId);

    /// <summary>
    /// Query traces
    /// </summary>
    Task<IEnumerable<TraceSpan>> QueryTracesAsync(MonitoringQuery query);

    /// <summary>
    /// Get trace statistics
    /// </summary>
    Task<Dictionary<string, object>> GetTraceStatisticsAsync(string serviceName, DateTime startTime, DateTime endTime, string tenantId);

    /// <summary>
    /// Get operation names for service
    /// </summary>
    Task<IEnumerable<string>> GetOperationNamesAsync(string serviceName, string tenantId);

    /// <summary>
    /// Get service names for tenant
    /// </summary>
    Task<IEnumerable<string>> GetServiceNamesAsync(string tenantId);
}