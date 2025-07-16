using System.ComponentModel.DataAnnotations;

namespace BizCore.Monitoring.Models;

/// <summary>
/// Metric data model
/// </summary>
public class Metric
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MetricType Type { get; set; } = MetricType.Counter;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public Dictionary<string, object> Dimensions { get; set; } = new();
    public MetricMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Metric types
/// </summary>
public enum MetricType
{
    Counter,
    Gauge,
    Histogram,
    Summary,
    Timer,
    Set,
    Custom
}

/// <summary>
/// Metric metadata
/// </summary>
public class MetricMetadata
{
    public string Category { get; set; } = string.Empty;
    public MetricSeverity Severity { get; set; } = MetricSeverity.Info;
    public bool IsAlert { get; set; } = false;
    public string AlertRule { get; set; } = string.Empty;
    public double? Threshold { get; set; }
    public string Format { get; set; } = string.Empty;
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// Metric severity levels
/// </summary>
public enum MetricSeverity
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Trace span model
/// </summary>
public class TraceSpan
{
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public string? ParentSpanId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public SpanStatus Status { get; set; } = SpanStatus.Ok;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public List<SpanLog> Logs { get; set; } = new();
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public Dictionary<string, object> Baggage { get; set; } = new();
}

/// <summary>
/// Span status
/// </summary>
public enum SpanStatus
{
    Ok,
    Cancelled,
    Unknown,
    InvalidArgument,
    DeadlineExceeded,
    NotFound,
    AlreadyExists,
    PermissionDenied,
    ResourceExhausted,
    FailedPrecondition,
    Aborted,
    OutOfRange,
    Unimplemented,
    Internal,
    Unavailable,
    DataLoss,
    Unauthenticated
}

/// <summary>
/// Span log entry
/// </summary>
public class SpanLog
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Fields { get; set; } = new();
}

/// <summary>
/// Log entry model
/// </summary>
public class LogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogLevel Level { get; set; } = LogLevel.Information;
    public string Message { get; set; } = string.Empty;
    public string Logger { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? StackTrace { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public Dictionary<string, string> Scopes { get; set; } = new();
}

/// <summary>
/// Log levels
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    None
}

/// <summary>
/// Health check model
/// </summary>
public class HealthCheck
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public HealthStatus Status { get; set; } = HealthStatus.Healthy;
    public TimeSpan Duration { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string ServiceName { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public HealthCheckConfiguration Configuration { get; set; } = new();
}

/// <summary>
/// Health status
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Health check configuration
/// </summary>
public class HealthCheckConfiguration
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int FailureThreshold { get; set; } = 3;
    public int SuccessThreshold { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Performance counter model
/// </summary>
public class PerformanceCounter
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ServiceName { get; set; } = string.Empty;
    public string MachineName { get; set; } = Environment.MachineName;
    public CounterType Type { get; set; } = CounterType.RawValue;
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Performance counter types
/// </summary>
public enum CounterType
{
    RawValue,
    Rate,
    Delta,
    Percentage,
    Average
}

/// <summary>
/// Alert model
/// </summary>
public class Alert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public AlertStatus Status { get; set; } = AlertStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string Source { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertRule Rule { get; set; } = new();
    public List<AlertAction> Actions { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public AlertMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Alert status
/// </summary>
public enum AlertStatus
{
    Active,
    Acknowledged,
    Resolved,
    Suppressed
}

/// <summary>
/// Alert rule
/// </summary>
public class AlertRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.GreaterThan;
    public TimeSpan EvaluationWindow { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan Frequency { get; set; } = TimeSpan.FromMinutes(1);
    public bool IsEnabled { get; set; } = true;
    public string TenantId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public AlertRuleConfiguration Configuration { get; set; } = new();
}

/// <summary>
/// Comparison operators
/// </summary>
public enum ComparisonOperator
{
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Equal,
    NotEqual,
    Contains,
    NotContains
}

/// <summary>
/// Alert rule configuration
/// </summary>
public class AlertRuleConfiguration
{
    public bool RequireDataPoints { get; set; } = true;
    public int MinDataPoints { get; set; } = 1;
    public bool TreatMissingDataAsZero { get; set; } = false;
    public TimeSpan? RearmDelay { get; set; }
    public string GroupBy { get; set; } = string.Empty;
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// Alert action
/// </summary>
public class AlertAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public List<AlertSeverity> TriggerOn { get; set; } = new();
    public AlertActionRetry Retry { get; set; } = new();
}

/// <summary>
/// Alert action retry configuration
/// </summary>
public class AlertActionRetry
{
    public bool IsEnabled { get; set; } = true;
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(10);
    public double BackoffMultiplier { get; set; } = 2.0;
}

/// <summary>
/// Alert metrics
/// </summary>
public class AlertMetrics
{
    public int TotalOccurrences { get; set; }
    public TimeSpan AverageResolutionTime { get; set; }
    public DateTime? LastOccurrence { get; set; }
    public DateTime? LastResolution { get; set; }
    public string MostCommonSource { get; set; } = string.Empty;
    public Dictionary<string, int> OccurrencesByHour { get; set; } = new();
}

/// <summary>
/// Dashboard model
/// </summary>
public class Dashboard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublic { get; set; } = false;
    public List<DashboardWidget> Widgets { get; set; } = new();
    public DashboardLayout Layout { get; set; } = new();
    public DashboardSettings Settings { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
/// Dashboard widget
/// </summary>
public class DashboardWidget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WidgetType Type { get; set; } = WidgetType.LineChart;
    public WidgetPosition Position { get; set; } = new();
    public WidgetSize Size { get; set; } = new();
    public WidgetConfiguration Configuration { get; set; } = new();
    public List<WidgetQuery> Queries { get; set; } = new();
    public WidgetThresholds Thresholds { get; set; } = new();
    public bool IsVisible { get; set; } = true;
    public DateTime? LastRefresh { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Widget types
/// </summary>
public enum WidgetType
{
    LineChart,
    BarChart,
    PieChart,
    GaugeChart,
    Heatmap,
    Table,
    SingleStat,
    Text,
    LogPanel,
    AlertList,
    HealthStatus,
    Custom
}

/// <summary>
/// Widget position
/// </summary>
public class WidgetPosition
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; } = 0;
}

/// <summary>
/// Widget size
/// </summary>
public class WidgetSize
{
    public int Width { get; set; } = 4;
    public int Height { get; set; } = 3;
    public int MinWidth { get; set; } = 2;
    public int MinHeight { get; set; } = 2;
    public int MaxWidth { get; set; } = 12;
    public int MaxHeight { get; set; } = 12;
}

/// <summary>
/// Widget configuration
/// </summary>
public class WidgetConfiguration
{
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(1);
    public string TimeRange { get; set; } = "last_1h";
    public Dictionary<string, object> ChartOptions { get; set; } = new();
    public Dictionary<string, string> ColorMappings { get; set; } = new();
    public string Format { get; set; } = string.Empty;
    public int? Decimals { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool ShowLegend { get; set; } = true;
    public bool ShowTooltip { get; set; } = true;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Widget query
/// </summary>
public class WidgetQuery
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public QueryType Type { get; set; } = QueryType.Metric;
    public string DataSource { get; set; } = string.Empty;
    public TimeSpan TimeRange { get; set; } = TimeSpan.FromHours(1);
    public string Aggregation { get; set; } = string.Empty;
    public Dictionary<string, string> Filters { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public string Color { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
}

/// <summary>
/// Query types
/// </summary>
public enum QueryType
{
    Metric,
    Log,
    Trace,
    Alert,
    Custom
}

/// <summary>
/// Widget thresholds
/// </summary>
public class WidgetThresholds
{
    public List<Threshold> Values { get; set; } = new();
    public string Mode { get; set; } = "absolute";
    public bool ShowThresholdLabels { get; set; } = true;
    public bool ShowThresholdMarkers { get; set; } = true;
}

/// <summary>
/// Threshold definition
/// </summary>
public class Threshold
{
    public double Value { get; set; }
    public string Color { get; set; } = string.Empty;
    public ThresholdOperator Operator { get; set; } = ThresholdOperator.GreaterThan;
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Threshold operators
/// </summary>
public enum ThresholdOperator
{
    GreaterThan,
    LessThan,
    Equal,
    NotEqual
}

/// <summary>
/// Dashboard layout
/// </summary>
public class DashboardLayout
{
    public LayoutType Type { get; set; } = LayoutType.Grid;
    public int Columns { get; set; } = 12;
    public int RowHeight { get; set; } = 30;
    public bool IsResizable { get; set; } = true;
    public bool IsDraggable { get; set; } = true;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Layout types
/// </summary>
public enum LayoutType
{
    Grid,
    Flex,
    Fixed,
    Custom
}

/// <summary>
/// Dashboard settings
/// </summary>
public class DashboardSettings
{
    public TimeSpan AutoRefresh { get; set; } = TimeSpan.FromMinutes(5);
    public bool ShowTitle { get; set; } = true;
    public bool ShowDescription { get; set; } = true;
    public bool ShowTimeRange { get; set; } = true;
    public bool ShowRefreshButton { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string Timezone { get; set; } = "UTC";
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Service level objective (SLO)
/// </summary>
public class ServiceLevelObjective
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public SLOType Type { get; set; } = SLOType.Availability;
    public double Target { get; set; } = 99.9;
    public TimeSpan Period { get; set; } = TimeSpan.FromDays(30);
    public string Query { get; set; } = string.Empty;
    public SLOConfiguration Configuration { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public SLOStatus Status { get; set; } = new();
}

/// <summary>
/// SLO types
/// </summary>
public enum SLOType
{
    Availability,
    Latency,
    ErrorRate,
    Throughput,
    Custom
}

/// <summary>
/// SLO configuration
/// </summary>
public class SLOConfiguration
{
    public string GoodQuery { get; set; } = string.Empty;
    public string TotalQuery { get; set; } = string.Empty;
    public double ErrorBudget { get; set; } = 0.1;
    public TimeSpan AlertingWindow { get; set; } = TimeSpan.FromMinutes(5);
    public List<SLOAlert> Alerts { get; set; } = new();
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// SLO alert configuration
/// </summary>
public class SLOAlert
{
    public string Name { get; set; } = string.Empty;
    public double BurnRateThreshold { get; set; } = 1.0;
    public TimeSpan LookbackWindow { get; set; } = TimeSpan.FromHours(1);
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// SLO status
/// </summary>
public class SLOStatus
{
    public double CurrentValue { get; set; }
    public double ErrorBudgetConsumed { get; set; }
    public double ErrorBudgetRemaining { get; set; }
    public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
    public SLOHealth Health { get; set; } = SLOHealth.Healthy;
    public List<SLOViolation> RecentViolations { get; set; } = new();
}

/// <summary>
/// SLO health status
/// </summary>
public enum SLOHealth
{
    Healthy,
    Warning,
    Critical,
    Breached
}

/// <summary>
/// SLO violation
/// </summary>
public class SLOViolation
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double Duration { get; set; }
    public double Impact { get; set; }
    public string Cause { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Monitoring configuration
/// </summary>
public class MonitoringConfiguration
{
    public string TenantId { get; set; } = string.Empty;
    public MetricsConfiguration Metrics { get; set; } = new();
    public TracingConfiguration Tracing { get; set; } = new();
    public LoggingConfiguration Logging { get; set; } = new();
    public AlertingConfiguration Alerting { get; set; } = new();
    public RetentionConfiguration Retention { get; set; } = new();
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Metrics configuration
/// </summary>
public class MetricsConfiguration
{
    public bool IsEnabled { get; set; } = true;
    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromSeconds(15);
    public List<string> EnabledMetrics { get; set; } = new();
    public List<string> DisabledMetrics { get; set; } = new();
    public Dictionary<string, double> Thresholds { get; set; } = new();
    public string ExportFormat { get; set; } = "prometheus";
    public Dictionary<string, object> CustomConfiguration { get; set; } = new();
}

/// <summary>
/// Tracing configuration
/// </summary>
public class TracingConfiguration
{
    public bool IsEnabled { get; set; } = true;
    public double SamplingRate { get; set; } = 0.1;
    public List<string> IgnoredOperations { get; set; } = new();
    public int MaxSpansPerTrace { get; set; } = 1000;
    public TimeSpan SpanTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public string Exporter { get; set; } = "jaeger";
    public Dictionary<string, object> ExporterConfiguration { get; set; } = new();
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfiguration
{
    public bool IsEnabled { get; set; } = true;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    public List<string> IgnoredLoggers { get; set; } = new();
    public bool IncludeScopes { get; set; } = true;
    public bool IncludeException { get; set; } = true;
    public string Format { get; set; } = "json";
    public Dictionary<string, object> Enrichers { get; set; } = new();
}

/// <summary>
/// Alerting configuration
/// </summary>
public class AlertingConfiguration
{
    public bool IsEnabled { get; set; } = true;
    public TimeSpan DefaultEvaluationInterval { get; set; } = TimeSpan.FromMinutes(1);
    public List<string> DefaultRecipients { get; set; } = new();
    public Dictionary<string, object> NotificationChannels { get; set; } = new();
    public bool EnableDigestMode { get; set; } = true;
    public TimeSpan DigestInterval { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Retention configuration
/// </summary>
public class RetentionConfiguration
{
    public TimeSpan MetricRetention { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan LogRetention { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan TraceRetention { get; set; } = TimeSpan.FromDays(3);
    public TimeSpan AlertRetention { get; set; } = TimeSpan.FromDays(90);
    public bool EnableArchiving { get; set; } = true;
    public string ArchiveLocation { get; set; } = string.Empty;
    public bool EnableCompression { get; set; } = true;
}

/// <summary>
/// Monitoring query
/// </summary>
public class MonitoringQuery
{
    public string Query { get; set; } = string.Empty;
    public QueryType Type { get; set; } = QueryType.Metric;
    public DateTime StartTime { get; set; } = DateTime.UtcNow.AddHours(-1);
    public DateTime EndTime { get; set; } = DateTime.UtcNow;
    public TimeSpan? Step { get; set; }
    public string Aggregation { get; set; } = string.Empty;
    public Dictionary<string, string> Filters { get; set; } = new();
    public int Limit { get; set; } = 1000;
    public string OrderBy { get; set; } = string.Empty;
    public bool Ascending { get; set; } = false;
}

/// <summary>
/// Query result
/// </summary>
public class QueryResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<QuerySeries> Series { get; set; } = new();
    public QueryMetadata Metadata { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Query series
/// </summary>
public class QuerySeries
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public List<DataPoint> Points { get; set; } = new();
    public string Unit { get; set; } = string.Empty;
    public QuerySeriesMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Data point
/// </summary>
public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public Dictionary<string, object>? Annotations { get; set; }
}

/// <summary>
/// Query series metadata
/// </summary>
public class QuerySeriesMetadata
{
    public int PointCount { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public double? AverageValue { get; set; }
    public DateTime? FirstTimestamp { get; set; }
    public DateTime? LastTimestamp { get; set; }
    public Dictionary<string, object> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Query metadata
/// </summary>
public class QueryMetadata
{
    public int TotalSeries { get; set; }
    public int TotalPoints { get; set; }
    public bool IsPartial { get; set; }
    public string? WarningMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
}

/// <summary>
/// Request/response models
/// </summary>
public class CreateMetricRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public MetricType Type { get; set; }
    
    [Required]
    public double Value { get; set; }
    
    public string Unit { get; set; } = string.Empty;
    
    [Required]
    public string Source { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public string ServiceName { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public Dictionary<string, object> Dimensions { get; set; } = new();
}

/// <summary>
/// Create dashboard request
/// </summary>
public class CreateDashboardRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public bool IsPublic { get; set; } = false;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
/// Create alert rule request
/// </summary>
public class CreateAlertRuleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Query { get; set; } = string.Empty;
    
    [Required]
    public string Condition { get; set; } = string.Empty;
    
    [Required]
    public double Threshold { get; set; }
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.GreaterThan;
    public TimeSpan EvaluationWindow { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan Frequency { get; set; } = TimeSpan.FromMinutes(1);
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Monitoring result wrapper
/// </summary>
public class MonitoringResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public List<ValidationError> ValidationErrors { get; set; } = new();

    public static MonitoringResult Success(object? data = null) =>
        new() { IsSuccess = true, Data = data != null ? new() { ["result"] = data } : new() };

    public static MonitoringResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static MonitoringResult ValidationFailure(List<ValidationError> errors) =>
        new() { IsSuccess = false, ValidationErrors = errors };
}

/// <summary>
/// Validation error
/// </summary>
public class ValidationError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Property { get; set; }
    public object? AttemptedValue { get; set; }
}

/// <summary>
/// Validation warning
/// </summary>
public class ValidationWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Property { get; set; }
}

/// <summary>
/// Condition operators for complex queries
/// </summary>
public enum ConditionOperator
{
    And,
    Or,
    Not
}