using BizCore.Monitoring.Interfaces;
using BizCore.Monitoring.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BizCore.Monitoring.Services;

/// <summary>
/// Distributed tracing service implementation with Jaeger/OpenTelemetry support
/// </summary>
public class TracingService : ITracingService
{
    private readonly ILogger<TracingService> _logger;
    private readonly TracingConfiguration _configuration;
    private readonly ConcurrentDictionary<string, TraceSpan> _activeSpans = new();

    public TracingService(
        ILogger<TracingService> logger,
        IOptions<MonitoringConfiguration> configuration)
    {
        _logger = logger;
        _configuration = configuration.Value.Tracing;
    }

    public async Task<TraceSpan> StartSpanAsync(string operationName, string? parentSpanId = null, Dictionary<string, string>? tags = null)
    {
        try
        {
            _logger.LogTrace("Starting span: {OperationName}, parent: {ParentSpanId}", operationName, parentSpanId);

            var span = new TraceSpan
            {
                TraceId = parentSpanId != null ? GetTraceIdFromSpan(parentSpanId) : GenerateTraceId(),
                SpanId = GenerateSpanId(),
                ParentSpanId = parentSpanId,
                OperationName = operationName,
                ServiceName = GetServiceName(),
                StartTime = DateTime.UtcNow,
                Status = SpanStatus.Ok,
                Tags = tags ?? new Dictionary<string, string>()
            };

            // Add default tags
            AddDefaultTags(span);

            // Apply sampling decision
            if (ShouldSampleTrace(span.TraceId))
            {
                // Store active span
                _activeSpans.TryAdd(span.SpanId, span);

                // In a real implementation, this would send to tracing backend (Jaeger, Zipkin, etc.)
                await SendSpanToBackendAsync(span, "start");

                _logger.LogTrace("Started span: {SpanId} for operation: {OperationName}", span.SpanId, operationName);
            }
            else
            {
                _logger.LogTrace("Span not sampled: {SpanId}", span.SpanId);
            }

            return span;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start span: {OperationName}", operationName);
            
            // Return a minimal span to avoid breaking the application
            return new TraceSpan
            {
                TraceId = GenerateTraceId(),
                SpanId = GenerateSpanId(),
                OperationName = operationName,
                ServiceName = GetServiceName(),
                StartTime = DateTime.UtcNow,
                Status = SpanStatus.Internal
            };
        }
    }

    public async Task FinishSpanAsync(string spanId, SpanStatus status = SpanStatus.Ok, string? errorMessage = null)
    {
        try
        {
            if (!_activeSpans.TryRemove(spanId, out var span))
            {
                _logger.LogWarning("Attempted to finish non-existent span: {SpanId}", spanId);
                return;
            }

            span.EndTime = DateTime.UtcNow;
            span.Duration = span.EndTime.Value - span.StartTime;
            span.Status = status;
            span.ErrorMessage = errorMessage;

            if (status != SpanStatus.Ok && !string.IsNullOrEmpty(errorMessage))
            {
                span.Tags["error"] = "true";
                span.Tags["error.message"] = errorMessage;
            }

            // Send final span data to backend
            await SendSpanToBackendAsync(span, "finish");

            _logger.LogTrace("Finished span: {SpanId}, duration: {Duration}ms, status: {Status}", 
                spanId, span.Duration.TotalMilliseconds, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to finish span: {SpanId}", spanId);
        }
    }

    public async Task AddSpanLogAsync(string spanId, string level, string message, Dictionary<string, object>? fields = null)
    {
        try
        {
            if (!_activeSpans.TryGetValue(spanId, out var span))
            {
                _logger.LogWarning("Attempted to add log to non-existent span: {SpanId}", spanId);
                return;
            }

            var log = new SpanLog
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                Fields = fields ?? new Dictionary<string, object>()
            };

            span.Logs.Add(log);

            _logger.LogTrace("Added log to span: {SpanId}, level: {Level}, message: {Message}", spanId, level, message);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add log to span: {SpanId}", spanId);
        }
    }

    public async Task AddSpanTagAsync(string spanId, string key, string value)
    {
        try
        {
            if (!_activeSpans.TryGetValue(spanId, out var span))
            {
                _logger.LogWarning("Attempted to add tag to non-existent span: {SpanId}", spanId);
                return;
            }

            span.Tags[key] = value;

            _logger.LogTrace("Added tag to span: {SpanId}, key: {Key}, value: {Value}", spanId, key, value);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tag to span: {SpanId}", spanId);
        }
    }

    public async Task<IEnumerable<TraceSpan>> GetTraceAsync(string traceId)
    {
        try
        {
            _logger.LogDebug("Getting trace: {TraceId}", traceId);

            // In a real implementation, this would query the tracing backend
            await Task.CompletedTask;
            
            // Return spans from active spans that match the trace ID
            return _activeSpans.Values.Where(s => s.TraceId == traceId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trace: {TraceId}", traceId);
            return Array.Empty<TraceSpan>();
        }
    }

    public async Task<TraceSpan?> GetSpanAsync(string spanId)
    {
        try
        {
            // Check active spans first
            if (_activeSpans.TryGetValue(spanId, out var activeSpan))
            {
                return activeSpan;
            }

            // In a real implementation, this would query the tracing backend for completed spans
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get span: {SpanId}", spanId);
            return null;
        }
    }

    public async Task<IEnumerable<TraceSpan>> QueryTracesAsync(MonitoringQuery query)
    {
        try
        {
            _logger.LogDebug("Querying traces with query: {Query}", query.Query);

            // In a real implementation, this would query the tracing backend
            await Task.CompletedTask;

            // For now, return filtered active spans
            var spans = _activeSpans.Values.AsEnumerable();

            // Apply time range filter
            spans = spans.Where(s => s.StartTime >= query.StartTime && s.StartTime <= query.EndTime);

            // Apply service name filter if specified
            if (query.Filters.TryGetValue("service", out var serviceName))
            {
                spans = spans.Where(s => s.ServiceName == serviceName);
            }

            // Apply operation name filter if specified
            if (query.Filters.TryGetValue("operation", out var operationName))
            {
                spans = spans.Where(s => s.OperationName.Contains(operationName));
            }

            // Apply limit
            spans = spans.Take(query.Limit);

            return spans.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query traces");
            return Array.Empty<TraceSpan>();
        }
    }

    public async Task<Dictionary<string, object>> GetTraceStatisticsAsync(string serviceName, DateTime startTime, DateTime endTime, string tenantId)
    {
        try
        {
            _logger.LogDebug("Getting trace statistics for service: {ServiceName}, tenant: {TenantId}", serviceName, tenantId);

            // In a real implementation, this would query the tracing backend for statistics
            await Task.CompletedTask;

            var spans = _activeSpans.Values
                .Where(s => s.ServiceName == serviceName && s.TenantId == tenantId)
                .Where(s => s.StartTime >= startTime && s.StartTime <= endTime)
                .ToList();

            return new Dictionary<string, object>
            {
                ["service_name"] = serviceName,
                ["tenant_id"] = tenantId,
                ["period_start"] = startTime,
                ["period_end"] = endTime,
                ["total_spans"] = spans.Count,
                ["total_traces"] = spans.Select(s => s.TraceId).Distinct().Count(),
                ["average_duration_ms"] = spans.Any() ? spans.Average(s => s.Duration.TotalMilliseconds) : 0,
                ["p95_duration_ms"] = CalculatePercentile(spans.Select(s => s.Duration.TotalMilliseconds), 0.95),
                ["p99_duration_ms"] = CalculatePercentile(spans.Select(s => s.Duration.TotalMilliseconds), 0.99),
                ["error_rate"] = spans.Any() ? spans.Count(s => s.Status != SpanStatus.Ok) / (double)spans.Count : 0,
                ["operations"] = spans.GroupBy(s => s.OperationName).ToDictionary(g => g.Key, g => g.Count())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trace statistics for service: {ServiceName}", serviceName);
            return new Dictionary<string, object>();
        }
    }

    public async Task<IEnumerable<string>> GetOperationNamesAsync(string serviceName, string tenantId)
    {
        try
        {
            _logger.LogDebug("Getting operation names for service: {ServiceName}, tenant: {TenantId}", serviceName, tenantId);

            // In a real implementation, this would query the tracing backend
            await Task.CompletedTask;

            return _activeSpans.Values
                .Where(s => s.ServiceName == serviceName && s.TenantId == tenantId)
                .Select(s => s.OperationName)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation names for service: {ServiceName}", serviceName);
            return Array.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetServiceNamesAsync(string tenantId)
    {
        try
        {
            _logger.LogDebug("Getting service names for tenant: {TenantId}", tenantId);

            // In a real implementation, this would query the tracing backend
            await Task.CompletedTask;

            return _activeSpans.Values
                .Where(s => s.TenantId == tenantId)
                .Select(s => s.ServiceName)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service names for tenant: {TenantId}", tenantId);
            return Array.Empty<string>();
        }
    }

    private string GenerateTraceId()
    {
        return Guid.NewGuid().ToString("N")[..16]; // 16-character trace ID
    }

    private string GenerateSpanId()
    {
        return Guid.NewGuid().ToString("N")[..16]; // 16-character span ID
    }

    private string GetTraceIdFromSpan(string parentSpanId)
    {
        if (_activeSpans.TryGetValue(parentSpanId, out var parentSpan))
        {
            return parentSpan.TraceId;
        }
        return GenerateTraceId(); // Fallback if parent span not found
    }

    private string GetServiceName()
    {
        return Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "bizcore-erp";
    }

    private void AddDefaultTags(TraceSpan span)
    {
        span.Tags["service.name"] = span.ServiceName;
        span.Tags["service.version"] = Environment.GetEnvironmentVariable("SERVICE_VERSION") ?? "1.0.0";
        span.Tags["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production";
        span.Tags["host.name"] = Environment.MachineName;
        
        if (!string.IsNullOrEmpty(span.TenantId))
        {
            span.Tags["tenant.id"] = span.TenantId;
        }

        if (!string.IsNullOrEmpty(span.UserId))
        {
            span.Tags["user.id"] = span.UserId;
        }
    }

    private bool ShouldSampleTrace(string traceId)
    {
        if (!_configuration.IsEnabled)
        {
            return false;
        }

        // Simple hash-based sampling
        var hash = traceId.GetHashCode();
        var sample = Math.Abs(hash % 10000) < (_configuration.SamplingRate * 10000);
        
        return sample;
    }

    private async Task SendSpanToBackendAsync(TraceSpan span, string action)
    {
        try
        {
            // In a real implementation, this would send to Jaeger, Zipkin, or OpenTelemetry Collector
            await Task.Delay(1); // Simulate network call

            _logger.LogTrace("Sent span to backend: {SpanId}, action: {Action}", span.SpanId, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send span to backend: {SpanId}", span.SpanId);
        }
    }

    private double CalculatePercentile(IEnumerable<double> values, double percentile)
    {
        var sortedValues = values.OrderBy(x => x).ToArray();
        if (sortedValues.Length == 0) return 0;

        var index = (int)Math.Ceiling(percentile * sortedValues.Length) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Length - 1));
        
        return sortedValues[index];
    }
}