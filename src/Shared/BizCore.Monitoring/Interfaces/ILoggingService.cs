using BizCore.Monitoring.Models;

namespace BizCore.Monitoring.Interfaces;

/// <summary>
/// Centralized logging service interface
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Write log entry
    /// </summary>
    Task WriteLogAsync(LogEntry logEntry);

    /// <summary>
    /// Write multiple log entries in batch
    /// </summary>
    Task WriteLogsBatchAsync(List<LogEntry> logEntries);

    /// <summary>
    /// Query logs
    /// </summary>
    Task<QueryResult> QueryLogsAsync(MonitoringQuery query);

    /// <summary>
    /// Get log entry by ID
    /// </summary>
    Task<LogEntry?> GetLogEntryAsync(string logId);

    /// <summary>
    /// Get logs by filters
    /// </summary>
    Task<IEnumerable<LogEntry>> GetLogsAsync(Dictionary<string, string> filters, int skip = 0, int take = 100);

    /// <summary>
    /// Get log levels for tenant
    /// </summary>
    Task<IEnumerable<LogLevel>> GetLogLevelsAsync(string tenantId);

    /// <summary>
    /// Get loggers for tenant
    /// </summary>
    Task<IEnumerable<string>> GetLoggersAsync(string tenantId);

    /// <summary>
    /// Search logs with full-text search
    /// </summary>
    Task<IEnumerable<LogEntry>> SearchLogsAsync(string searchTerm, string tenantId, DateTime? startTime = null, DateTime? endTime = null, LogLevel? minLevel = null);

    /// <summary>
    /// Get log statistics
    /// </summary>
    Task<Dictionary<string, object>> GetLogStatisticsAsync(string tenantId, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Archive old logs
    /// </summary>
    Task<bool> ArchiveLogsAsync(string tenantId, DateTime beforeDate);
}