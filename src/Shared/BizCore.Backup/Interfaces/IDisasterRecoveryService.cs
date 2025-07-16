using BizCore.Backup.Models;

namespace BizCore.Backup.Interfaces;

/// <summary>
/// Disaster recovery service interface
/// </summary>
public interface IDisasterRecoveryService
{
    /// <summary>
    /// Create disaster recovery plan
    /// </summary>
    Task<DisasterRecoveryPlan> CreatePlanAsync(DisasterRecoveryPlan plan);

    /// <summary>
    /// Update disaster recovery plan
    /// </summary>
    Task<DisasterRecoveryPlan> UpdatePlanAsync(string planId, DisasterRecoveryPlan plan);

    /// <summary>
    /// Get disaster recovery plan
    /// </summary>
    Task<DisasterRecoveryPlan?> GetPlanAsync(string planId);

    /// <summary>
    /// Query disaster recovery plans
    /// </summary>
    Task<IEnumerable<DisasterRecoveryPlan>> QueryPlansAsync(string tenantId);

    /// <summary>
    /// Activate disaster recovery plan
    /// </summary>
    Task<DRActivation> ActivatePlanAsync(string planId, DRActivationRequest request);

    /// <summary>
    /// Test disaster recovery plan
    /// </summary>
    Task<DRTestResult> TestPlanAsync(string planId, DRTestOptions options);

    /// <summary>
    /// Get recovery metrics
    /// </summary>
    Task<DRMetrics> GetMetricsAsync(string tenantId);

    /// <summary>
    /// Failover to secondary site
    /// </summary>
    Task<DRFailoverResult> FailoverAsync(string planId, DRFailoverOptions options);

    /// <summary>
    /// Failback to primary site
    /// </summary>
    Task<DRFailbackResult> FailbackAsync(string planId, DRFailbackOptions options);

    /// <summary>
    /// Get disaster recovery status
    /// </summary>
    Task<DRStatus> GetStatusAsync(string tenantId);

    /// <summary>
    /// Delete disaster recovery plan
    /// </summary>
    Task<bool> DeletePlanAsync(string planId);
}

/// <summary>
/// DR activation request
/// </summary>
public class DRActivationRequest
{
    public string Reason { get; set; } = string.Empty;
    public DRTriggerType TriggerType { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public bool SkipVerification { get; set; } = false;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// DR activation result
/// </summary>
public class DRActivation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PlanId { get; set; } = string.Empty;
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DRActivationStatus Status { get; set; } = DRActivationStatus.InProgress;
    public string InitiatedBy { get; set; } = string.Empty;
    public DRStepExecution[] StepExecutions { get; set; } = Array.Empty<DRStepExecution>();
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// DR activation status
/// </summary>
public enum DRActivationStatus
{
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted,
    Cancelled
}

/// <summary>
/// DR step execution
/// </summary>
public class DRStepExecution
{
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DRStepStatus Status { get; set; } = DRStepStatus.Pending;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Output { get; set; } = new();
}

/// <summary>
/// DR step status
/// </summary>
public enum DRStepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// DR test options
/// </summary>
public class DRTestOptions
{
    public bool TestFailover { get; set; } = true;
    public bool TestNotifications { get; set; } = true;
    public bool TestDataIntegrity { get; set; } = true;
    public bool UseTestEnvironment { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(2);
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// DR test result
/// </summary>
public class DRTestResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PlanId { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public bool IsSuccessful { get; set; }
    public DRTestMetrics Metrics { get; set; } = new();
    public DRTestIssue[] Issues { get; set; } = Array.Empty<DRTestIssue>();
    public string Summary { get; set; } = string.Empty;
    public Dictionary<string, object> Results { get; set; } = new();
}

/// <summary>
/// DR test metrics
/// </summary>
public class DRTestMetrics
{
    public TimeSpan RecoveryTime { get; set; }
    public TimeSpan DataLoss { get; set; }
    public double SuccessRate { get; set; }
    public int StepsCompleted { get; set; }
    public int StepsFailed { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// DR test issue
/// </summary>
public class DRTestIssue
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ViolationSeverity Severity { get; set; }
    public string Component { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// DR metrics
/// </summary>
public class DRMetrics
{
    public TimeSpan AverageRecoveryTime { get; set; }
    public TimeSpan LastRecoveryTime { get; set; }
    public double RecoverySuccessRate { get; set; }
    public int TotalActivations { get; set; }
    public int SuccessfulActivations { get; set; }
    public int FailedActivations { get; set; }
    public DateTime? LastActivation { get; set; }
    public DateTime? LastTest { get; set; }
    public double TestSuccessRate { get; set; }
    public TimeSpan EstimatedRTO { get; set; }
    public TimeSpan EstimatedRPO { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// DR failover options
/// </summary>
public class DRFailoverOptions
{
    public bool PreserveData { get; set; } = true;
    public bool MaintainConnections { get; set} = true;
    public bool NotifyUsers { get; set; } = true;
    public int GracePeriodMinutes { get; set; } = 5;
    public string[] TargetRegions { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// DR failover result
/// </summary>
public class DRFailoverResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string PrimaryRegion { get; set; } = string.Empty;
    public string SecondaryRegion { get; set; } = string.Empty;
    public TimeSpan Downtime { get; set; }
    public TimeSpan DataLoss { get; set; }
    public int AffectedUsers { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// DR failback options
/// </summary>
public class DRFailbackOptions
{
    public bool VerifyPrimarySite { get; set; } = true;
    public bool SyncData { get; set; } = true;
    public bool TestBeforeFailback { get; set; } = true;
    public int SyncWindowMinutes { get; set; } = 30;
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// DR failback result
/// </summary>
public class DRFailbackResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public TimeSpan Duration { get; set; }
    public long DataSynced { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// DR status
/// </summary>
public class DRStatus
{
    public string TenantId { get; set; } = string.Empty;
    public DRSystemStatus SystemStatus { get; set; } = DRSystemStatus.Normal;
    public string PrimarySite { get; set; } = string.Empty;
    public string[] SecondarySites { get; set; } = Array.Empty<string>();
    public DateTime LastBackup { get; set; }
    public DateTime LastTest { get; set; }
    public bool IsFailoverActive { get; set; }
    public string? ActiveFailoverId { get; set; }
    public DRReadiness Readiness { get; set; } = new();
    public Dictionary<string, object> Health { get; set; } = new();
}

/// <summary>
/// DR system status
/// </summary>
public enum DRSystemStatus
{
    Normal,
    Warning,
    Critical,
    Failover,
    Failback,
    Maintenance
}

/// <summary>
/// DR readiness
/// </summary>
public class DRReadiness
{
    public bool IsReady { get; set; }
    public double ReadinessScore { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public DateTime LastChecked { get; set; }
    public Dictionary<string, bool> ComponentReadiness { get; set; } = new();
}