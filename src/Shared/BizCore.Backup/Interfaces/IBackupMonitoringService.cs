using BizCore.Backup.Models;

namespace BizCore.Backup.Interfaces;

/// <summary>
/// Backup monitoring service interface
/// </summary>
public interface IBackupMonitoringService
{
    /// <summary>
    /// Track backup execution
    /// </summary>
    Task TrackExecutionAsync(BackupExecution execution);

    /// <summary>
    /// Update execution progress
    /// </summary>
    Task UpdateProgressAsync(string executionId, BackupProgress progress);

    /// <summary>
    /// Record backup metrics
    /// </summary>
    Task RecordMetricsAsync(string executionId, BackupPerformanceMetrics metrics);

    /// <summary>
    /// Send backup alert
    /// </summary>
    Task SendAlertAsync(BackupAlert alert);

    /// <summary>
    /// Get real-time metrics
    /// </summary>
    Task<BackupRealtimeMetrics> GetRealtimeMetricsAsync(string tenantId);

    /// <summary>
    /// Get health status
    /// </summary>
    Task<BackupHealthStatus> GetHealthStatusAsync(string tenantId);

    /// <summary>
    /// Check SLA compliance
    /// </summary>
    Task<SLAComplianceReport> CheckSLAComplianceAsync(string tenantId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get backup trends
    /// </summary>
    Task<BackupTrends> GetTrendsAsync(string tenantId, int days = 30);

    /// <summary>
    /// Monitor storage usage
    /// </summary>
    Task<StorageUsageReport> GetStorageUsageAsync(string tenantId);

    /// <summary>
    /// Get failure analysis
    /// </summary>
    Task<FailureAnalysisReport> GetFailureAnalysisAsync(string tenantId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Set up alert rule
    /// </summary>
    Task<AlertRule> CreateAlertRuleAsync(AlertRule rule);

    /// <summary>
    /// Get alert rules
    /// </summary>
    Task<IEnumerable<AlertRule>> GetAlertRulesAsync(string tenantId);

    /// <summary>
    /// Delete alert rule
    /// </summary>
    Task<bool> DeleteAlertRuleAsync(string ruleId);
}

/// <summary>
/// Backup progress
/// </summary>
public class BackupProgress
{
    public double PercentageComplete { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public long BytesProcessed { get; set; }
    public long TotalBytes { get; set; }
    public int FilesProcessed { get; set; }
    public int TotalFiles { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public double TransferRateMBps { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Backup alert
/// </summary>
public class BackupAlert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public AlertType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? JobId { get; set; }
    public string? ExecutionId { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public AlertAction[] SuggestedActions { get; set; } = Array.Empty<AlertAction>();
}

/// <summary>
/// Alert severity
/// </summary>
public enum AlertSeverity
{
    Information,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Alert type
/// </summary>
public enum AlertType
{
    BackupFailed,
    BackupDelayed,
    StorageFull,
    EncryptionError,
    NetworkError,
    VerificationFailed,
    SLAViolation,
    SecurityIssue,
    ConfigurationError,
    PerformanceIssue
}

/// <summary>
/// Alert action
/// </summary>
public class AlertAction
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Backup realtime metrics
/// </summary>
public class BackupRealtimeMetrics
{
    public int ActiveBackups { get; set; }
    public int QueuedBackups { get; set; }
    public double CurrentThroughputMBps { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double NetworkUsagePercent { get; set; }
    public long StorageUsedBytes { get; set; }
    public long StorageAvailableBytes { get; set; }
    public Dictionary<string, RunningBackupMetrics> RunningJobs { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Running backup metrics
/// </summary>
public class RunningBackupMetrics
{
    public string JobId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public TimeSpan RunningTime { get; set; }
    public double ThroughputMBps { get; set; }
    public long BytesProcessed { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
}

/// <summary>
/// Backup health status
/// </summary>
public class BackupHealthStatus
{
    public HealthLevel Overall { get; set; }
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    public HealthIssue[] Issues { get; set; } = Array.Empty<HealthIssue>();
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public double HealthScore { get; set; }
}

/// <summary>
/// Health level
/// </summary>
public enum HealthLevel
{
    Healthy,
    Degraded,
    Unhealthy,
    Critical
}

/// <summary>
/// Component health
/// </summary>
public class ComponentHealth
{
    public string Name { get; set; } = string.Empty;
    public HealthLevel Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Health issue
/// </summary>
public class HealthIssue
{
    public string Component { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public HealthLevel Severity { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// SLA compliance report
/// </summary>
public class SLAComplianceReport
{
    public string TenantId { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public double CompliancePercentage { get; set; }
    public SLAMetric[] Metrics { get; set; } = Array.Empty<SLAMetric>();
    public SLAViolation[] Violations { get; set; } = Array.Empty<SLAViolation>();
    public Dictionary<string, object> Summary { get; set; } = new();
}

/// <summary>
/// SLA metric
/// </summary>
public class SLAMetric
{
    public string Name { get; set; } = string.Empty;
    public double Target { get; set; }
    public double Actual { get; set; }
    public bool IsMet { get; set; }
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// SLA violation
/// </summary>
public class SLAViolation
{
    public string Metric { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public double Expected { get; set; }
    public double Actual { get; set; }
    public string Reason { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Backup trends
/// </summary>
public class BackupTrends
{
    public TrendData BackupCount { get; set; } = new();
    public TrendData DataSize { get; set; } = new();
    public TrendData Duration { get; set; } = new();
    public TrendData SuccessRate { get; set; } = new();
    public TrendData CompressionRatio { get; set; } = new();
    public Dictionary<string, TrendData> CustomTrends { get; set; } = new();
}

/// <summary>
/// Trend data
/// </summary>
public class TrendData
{
    public string Name { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double PreviousValue { get; set; }
    public double ChangePercentage { get; set; }
    public TrendDirection Direction { get; set; }
    public DataPoint[] DataPoints { get; set; } = Array.Empty<DataPoint>();
}

/// <summary>
/// Trend direction
/// </summary>
public enum TrendDirection
{
    Up,
    Down,
    Stable
}

/// <summary>
/// Data point
/// </summary>
public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Storage usage report
/// </summary>
public class StorageUsageReport
{
    public string TenantId { get; set; } = string.Empty;
    public long TotalStorageBytes { get; set; }
    public long UsedStorageBytes { get; set; }
    public long AvailableStorageBytes { get; set; }
    public double UsagePercentage { get; set; }
    public Dictionary<string, StorageBreakdown> BreakdownByType { get; set; } = new();
    public Dictionary<string, StorageBreakdown> BreakdownByDestination { get; set; } = new();
    public StorageTrend[] Trends { get; set; } = Array.Empty<StorageTrend>();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Storage breakdown
/// </summary>
public class StorageBreakdown
{
    public string Category { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public double Percentage { get; set; }
    public int FileCount { get; set; }
    public DateTime OldestFile { get; set; }
    public DateTime NewestFile { get; set; }
}

/// <summary>
/// Storage trend
/// </summary>
public class StorageTrend
{
    public DateTime Date { get; set; }
    public long UsedBytes { get; set; }
    public double GrowthRate { get; set; }
    public int BackupCount { get; set; }
}

/// <summary>
/// Failure analysis report
/// </summary>
public class FailureAnalysisReport
{
    public string TenantId { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalFailures { get; set; }
    public double FailureRate { get; set; }
    public Dictionary<string, FailureCategory> FailuresByCategory { get; set; } = new();
    public FailurePattern[] Patterns { get; set; } = Array.Empty<FailurePattern>();
    public string[] CommonCauses { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Failure category
/// </summary>
public class FailureCategory
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string MostCommonError { get; set; } = string.Empty;
    public TimeSpan AverageRecoveryTime { get; set; }
}

/// <summary>
/// Failure pattern
/// </summary>
public class FailurePattern
{
    public string Description { get; set; } = string.Empty;
    public int Occurrences { get; set; }
    public string TimePattern { get; set; } = string.Empty;
    public string[] AffectedJobs { get; set; } = Array.Empty<string>();
    public double CorrelationStrength { get; set; }
}

/// <summary>
/// Alert rule
/// </summary>
public class AlertRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public AlertRuleCondition Condition { get; set; } = new();
    public AlertRuleAction[] Actions { get; set; } = Array.Empty<AlertRuleAction>();
    public bool IsEnabled { get; set; } = true;
    public AlertSeverity Severity { get; set; }
    public TimeSpan? CooldownPeriod { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggered { get; set; }
    public int TriggerCount { get; set; }
}

/// <summary>
/// Alert rule condition
/// </summary>
public class AlertRuleCondition
{
    public string MetricName { get; set; } = string.Empty;
    public ComparisonOperator Operator { get; set; }
    public double Threshold { get; set; }
    public TimeSpan? Duration { get; set; }
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Comparison operator
/// </summary>
public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    NotContains
}

/// <summary>
/// Alert rule action
/// </summary>
public class AlertRuleAction
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}