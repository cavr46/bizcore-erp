using System.ComponentModel.DataAnnotations;

namespace BizCore.Backup.Models;

/// <summary>
/// Backup job configuration and execution details
/// </summary>
public class BackupJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public BackupType Type { get; set; } = BackupType.Incremental;
    public BackupScope Scope { get; set; } = BackupScope.TenantData;
    public BackupStatus Status { get; set; } = BackupStatus.Pending;
    public BackupSchedule Schedule { get; set; } = new();
    public BackupConfiguration Configuration { get; set; } = new();
    public BackupEncryption Encryption { get; set; } = new();
    public BackupRetention Retention { get; set; } = new();
    public BackupDestination[] Destinations { get; set; } = Array.Empty<BackupDestination>();
    public BackupExecution? LastExecution { get; set; }
    public BackupExecution? NextExecution { get; set; }
    public BackupStatistics Statistics { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsSystemJob { get; set; } = false;
    public int Priority { get; set; } = 50;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup execution instance
/// </summary>
public class BackupExecution
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string JobId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public BackupType Type { get; set; }
    public BackupStatus Status { get; set; } = BackupStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public long TotalSize { get; set; }
    public long CompressedSize { get; set; }
    public int FilesProcessed { get; set; }
    public int FilesSkipped { get; set; }
    public int FilesErrored { get; set; }
    public double CompressionRatio { get; set; }
    public double ProgressPercentage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WarningMessage { get; set; }
    public BackupChecksum Checksum { get; set; } = new();
    public BackupDestinationResult[] DestinationResults { get; set; } = Array.Empty<BackupDestinationResult>();
    public string[] ProcessedFiles { get; set; } = Array.Empty<string>();
    public string[] SkippedFiles { get; set; } = Array.Empty<string>();
    public string[] ErrorFiles { get; set; } = Array.Empty<string>();
    public BackupPerformanceMetrics Performance { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup types
/// </summary>
public enum BackupType
{
    Full,
    Incremental,
    Differential,
    Snapshot,
    Archive,
    Mirror,
    Synthetic
}

/// <summary>
/// Backup scope definition
/// </summary>
public enum BackupScope
{
    TenantData,
    SystemData,
    UserData,
    ApplicationData,
    DatabaseOnly,
    FilesOnly,
    ConfigurationOnly,
    Complete
}

/// <summary>
/// Backup status
/// </summary>
public enum BackupStatus
{
    Pending,
    Queued,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled,
    Expired,
    Verifying,
    Compressing,
    Uploading,
    Encrypted
}

/// <summary>
/// Backup schedule configuration
/// </summary>
public class BackupSchedule
{
    public bool IsEnabled { get; set; } = true;
    public BackupFrequency Frequency { get; set; } = BackupFrequency.Daily;
    public TimeSpan Time { get; set; } = TimeSpan.FromHours(2); // 2 AM
    public DayOfWeek[]? DaysOfWeek { get; set; }
    public int[]? DaysOfMonth { get; set; }
    public string? CronExpression { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public DateTime? NextRun { get; set; }
    public DateTime? LastRun { get; set; }
    public int MaxConcurrentJobs { get; set; } = 1;
    public int RetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public BackupWindow? MaintenanceWindow { get; set; }
}

/// <summary>
/// Backup frequency options
/// </summary>
public enum BackupFrequency
{
    Continuous,
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Yearly,
    Custom
}

/// <summary>
/// Backup maintenance window
/// </summary>
public class BackupWindow
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DayOfWeek[] DaysOfWeek { get; set; } = Array.Empty<DayOfWeek>();
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Backup configuration settings
/// </summary>
public class BackupConfiguration
{
    public bool EnableCompression { get; set; } = true;
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;
    public bool EnableDeduplication { get; set; } = true;
    public bool EnableVerification { get; set; } = true;
    public bool EnableIntegrityCheck { get; set; } = true;
    public bool EnableTransactionLogBackup { get; set; } = true;
    public int MaxParallelStreams { get; set; } = 4;
    public int BufferSize { get; set; } = 64 * 1024 * 1024; // 64MB
    public int TransferTimeoutMinutes { get; set; } = 60;
    public string[] IncludePatterns { get; set; } = Array.Empty<string>();
    public string[] ExcludePatterns { get; set; } = Array.Empty<string>();
    public string[] IncludeTables { get; set; } = Array.Empty<string>();
    public string[] ExcludeTables { get; set; } = Array.Empty<string>();
    public bool BackupIndexes { get; set; } = true;
    public bool BackupTriggers { get; set; } = true;
    public bool BackupViews { get; set; } = true;
    public bool BackupProcedures { get; set; } = true;
    public bool BackupPermissions { get; set; } = true;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Compression levels
/// </summary>
public enum CompressionLevel
{
    None,
    Fast,
    Optimal,
    Maximum
}

/// <summary>
/// Backup encryption settings
/// </summary>
public class BackupEncryption
{
    public bool IsEnabled { get; set; } = true;
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256;
    public string KeyId { get; set; } = string.Empty;
    public string KeyVaultUrl { get; set; } = string.Empty;
    public bool UseCustomerManagedKey { get; set; } = false;
    public string? Password { get; set; }
    public string? Certificate { get; set; }
    public bool EncryptInTransit { get; set; } = true;
    public bool EncryptAtRest { get; set; } = true;
    public Dictionary<string, string> KeyRotationSchedule { get; set; } = new();
}

/// <summary>
/// Encryption algorithms
/// </summary>
public enum EncryptionAlgorithm
{
    AES128,
    AES256,
    ChaCha20,
    RSA2048,
    RSA4096
}

/// <summary>
/// Backup retention policy
/// </summary>
public class BackupRetention
{
    public int DailyRetentionDays { get; set; } = 7;
    public int WeeklyRetentionWeeks { get; set; } = 4;
    public int MonthlyRetentionMonths { get; set; } = 12;
    public int YearlyRetentionYears { get; set; } = 5;
    public long MaxStorageGB { get; set; } = 1000;
    public int MaxBackupCount { get; set; } = 100;
    public bool EnableAutoCleanup { get; set; } = true;
    public bool EnableArchiving { get; set; } = true;
    public int ArchiveAfterDays { get; set; } = 90;
    public string ArchiveStorageClass { get; set; } = "GLACIER";
    public bool EnableLegalHold { get; set; } = false;
    public DateTime? LegalHoldUntil { get; set; }
    public string[] ComplianceRequirements { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Backup destination configuration
/// </summary>
public class BackupDestination
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public BackupDestinationType Type { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsPrimary { get; set; } = false;
    public int Priority { get; set; } = 50;
    public Dictionary<string, string> Configuration { get; set; } = new();
    public BackupDestinationCredentials Credentials { get; set; } = new();
    public BackupDestinationLimits Limits { get; set; } = new();
}

/// <summary>
/// Backup destination types
/// </summary>
public enum BackupDestinationType
{
    Local,
    Network,
    AWS_S3,
    Azure_Blob,
    Google_Cloud,
    FTP,
    SFTP,
    WebDAV,
    Dropbox,
    OneDrive,
    Custom
}

/// <summary>
/// Backup destination credentials
/// </summary>
public class BackupDestinationCredentials
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Certificate { get; set; } = string.Empty;
    public string KeyFile { get; set; } = string.Empty;
    public Dictionary<string, string> CustomCredentials { get; set; } = new();
}

/// <summary>
/// Backup destination limits
/// </summary>
public class BackupDestinationLimits
{
    public long MaxStorageGB { get; set; } = 1000;
    public long MaxFileSize { get; set; } = 5 * 1024 * 1024 * 1024L; // 5GB
    public int MaxConcurrentConnections { get; set; } = 10;
    public int MaxTransferRateMbps { get; set; } = 100;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan TransferTimeout { get; set; } = TimeSpan.FromMinutes(60);
}

/// <summary>
/// Backup destination result
/// </summary>
public class BackupDestinationResult
{
    public string DestinationId { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public long BytesTransferred { get; set; }
    public double TransferRate { get; set; }
    public string RemotePath { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup checksum verification
/// </summary>
public class BackupChecksum
{
    public string Algorithm { get; set; } = "SHA256";
    public string Value { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    public DateTime? VerifiedAt { get; set; }
    public Dictionary<string, string> FileChecksums { get; set; } = new();
}

/// <summary>
/// Backup performance metrics
/// </summary>
public class BackupPerformanceMetrics
{
    public double ThroughputMBps { get; set; }
    public double CompressionRatio { get; set; }
    public double DeduplicationRatio { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double NetworkUtilizationPercent { get; set; }
    public double DiskIOUtilizationPercent { get; set; }
    public int ConcurrentStreams { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Backup statistics
/// </summary>
public class BackupStatistics
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public DateTime? LastSuccessfulBackup { get; set; }
    public DateTime? LastFailedBackup { get; set; }
    public long TotalBackupSize { get; set; }
    public long AverageBackupSize { get; set; }
    public TimeSpan AverageBackupDuration { get; set; }
    public TimeSpan FastestBackupDuration { get; set; }
    public TimeSpan SlowestBackupDuration { get; set; }
    public double SuccessRate { get; set; }
    public double AverageCompressionRatio { get; set; }
    public double AverageDeduplicationRatio { get; set; }
    public Dictionary<string, object> CustomStatistics { get; set; } = new();
}

/// <summary>
/// Restore job configuration
/// </summary>
public class RestoreJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string BackupId { get; set; } = string.Empty;
    public RestoreType Type { get; set; } = RestoreType.Full;
    public RestoreTarget Target { get; set; } = new();
    public RestoreOptions Options { get; set; } = new();
    public RestoreStatus Status { get; set; } = RestoreStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public double ProgressPercentage { get; set; }
    public string? ErrorMessage { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public RestoreVerification Verification { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Restore types
/// </summary>
public enum RestoreType
{
    Full,
    Partial,
    PointInTime,
    Incremental,
    Differential,
    FileLevel,
    DatabaseLevel,
    TableLevel
}

/// <summary>
/// Restore target configuration
/// </summary>
public class RestoreTarget
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; } = false;
    public bool CreateNewDatabase { get; set; } = false;
    public string? NewDatabaseName { get; set; }
    public DateTime? PointInTimeTarget { get; set; }
    public string[] IncludeObjects { get; set; } = Array.Empty<string>();
    public string[] ExcludeObjects { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> ObjectMapping { get; set; } = new();
}

/// <summary>
/// Restore options
/// </summary>
public class RestoreOptions
{
    public bool VerifyChecksums { get; set; } = true;
    public bool RestorePermissions { get; set; } = true;
    public bool RestoreIndexes { get; set; } = true;
    public bool RestoreTriggers { get; set; } = true;
    public bool RestoreViews { get; set; } = true;
    public bool RestoreProcedures { get; set; } = true;
    public bool PreserveReplication { get; set; } = false;
    public bool RestoreWithRecovery { get; set; } = true;
    public bool RestoreWithNoRecovery { get; set; } = false;
    public bool RestoreWithStandby { get; set; } = false;
    public int MaxParallelStreams { get; set; } = 4;
    public int BufferSize { get; set; } = 64 * 1024 * 1024; // 64MB
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// Restore status
/// </summary>
public enum RestoreStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Verifying,
    Downloading,
    Extracting,
    Restoring
}

/// <summary>
/// Restore verification
/// </summary>
public class RestoreVerification
{
    public bool IsEnabled { get; set; } = true;
    public bool IsCompleted { get; set; } = false;
    public bool IsSuccessful { get; set; } = false;
    public DateTime? VerifiedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, bool> ObjectVerification { get; set; } = new();
    public Dictionary<string, string> ChecksumVerification { get; set; } = new();
}

/// <summary>
/// Disaster recovery plan
/// </summary>
public class DisasterRecoveryPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DRPriority Priority { get; set; } = DRPriority.High;
    public TimeSpan RecoveryTimeObjective { get; set; } = TimeSpan.FromHours(4);
    public TimeSpan RecoveryPointObjective { get; set; } = TimeSpan.FromHours(1);
    public DRScope Scope { get; set; } = DRScope.Complete;
    public DRTrigger[] Triggers { get; set; } = Array.Empty<DRTrigger>();
    public DRStep[] Steps { get; set; } = Array.Empty<DRStep>();
    public DRNotification[] Notifications { get; set; } = Array.Empty<DRNotification>();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTestedAt { get; set; }
    public DateTime? LastActivatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Disaster recovery priority
/// </summary>
public enum DRPriority
{
    Critical,
    High,
    Medium,
    Low
}

/// <summary>
/// Disaster recovery scope
/// </summary>
public enum DRScope
{
    Complete,
    DatabaseOnly,
    ApplicationOnly,
    FilesOnly,
    ConfigurationOnly,
    UserDataOnly
}

/// <summary>
/// Disaster recovery trigger
/// </summary>
public class DRTrigger
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DRTriggerType Type { get; set; }
    public string Condition { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Disaster recovery trigger types
/// </summary>
public enum DRTriggerType
{
    Manual,
    Automatic,
    Scheduled,
    HealthCheck,
    Monitoring,
    External
}

/// <summary>
/// Disaster recovery step
/// </summary>
public class DRStep
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public DRStepType Type { get; set; }
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsRequired { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
    public int RetryAttempts { get; set; } = 3;
    public string[] Dependencies { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Disaster recovery step types
/// </summary>
public enum DRStepType
{
    Backup,
    Restore,
    Failover,
    Notification,
    Verification,
    Cleanup,
    Custom
}

/// <summary>
/// Disaster recovery notification
/// </summary>
public class DRNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DRNotificationType Type { get; set; }
    public string[] Recipients { get; set; } = Array.Empty<string>();
    public string Subject { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public DRNotificationTrigger Trigger { get; set; } = DRNotificationTrigger.OnStart;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Disaster recovery notification types
/// </summary>
public enum DRNotificationType
{
    Email,
    SMS,
    Slack,
    Teams,
    Webhook,
    PagerDuty
}

/// <summary>
/// Disaster recovery notification triggers
/// </summary>
public enum DRNotificationTrigger
{
    OnStart,
    OnComplete,
    OnFailure,
    OnSuccess,
    OnWarning,
    OnStepComplete
}

/// <summary>
/// Backup compliance report
/// </summary>
public class BackupComplianceReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public ComplianceStatus Status { get; set; } = ComplianceStatus.Compliant;
    public ComplianceMetrics Metrics { get; set; } = new();
    public ComplianceViolation[] Violations { get; set; } = Array.Empty<ComplianceViolation>();
    public ComplianceRecommendation[] Recommendations { get; set; } = Array.Empty<ComplianceRecommendation>();
    public Dictionary<string, object> CustomData { get; set; } = new();
}

/// <summary>
/// Compliance status
/// </summary>
public enum ComplianceStatus
{
    Compliant,
    NonCompliant,
    PartiallyCompliant,
    NotApplicable,
    UnderReview
}

/// <summary>
/// Compliance metrics
/// </summary>
public class ComplianceMetrics
{
    public double BackupComplianceRate { get; set; }
    public double RetentionComplianceRate { get; set; }
    public double EncryptionComplianceRate { get; set; }
    public double RecoveryTestComplianceRate { get; set; }
    public int TotalBackups { get; set; }
    public int CompliantBackups { get; set; }
    public int NonCompliantBackups { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Compliance violation
/// </summary>
public class ComplianceViolation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ViolationSeverity Severity { get; set; } = ViolationSeverity.Medium;
    public string Requirement { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Violation severity
/// </summary>
public enum ViolationSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Compliance recommendation
/// </summary>
public class ComplianceRecommendation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationPriority Priority { get; set; } = RecommendationPriority.Medium;
    public string Category { get; set; } = string.Empty;
    public string[] Actions { get; set; } = Array.Empty<string>();
    public string[] Benefits { get; set; } = Array.Empty<string>();
    public TimeSpan EstimatedImplementationTime { get; set; } = TimeSpan.FromHours(1);
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Recommendation priority
/// </summary>
public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Backup request DTOs
/// </summary>
public class CreateBackupJobRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public BackupType Type { get; set; } = BackupType.Incremental;
    public BackupScope Scope { get; set; } = BackupScope.TenantData;
    public BackupSchedule Schedule { get; set; } = new();
    public BackupConfiguration Configuration { get; set; } = new();
    public BackupEncryption Encryption { get; set; } = new();
    public BackupRetention Retention { get; set; } = new();
    public BackupDestination[] Destinations { get; set; } = Array.Empty<BackupDestination>();
    public int Priority { get; set; } = 50;
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Restore request DTOs
/// </summary>
public class CreateRestoreJobRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    [Required]
    public string BackupId { get; set; } = string.Empty;
    
    public RestoreType Type { get; set; } = RestoreType.Full;
    public RestoreTarget Target { get; set; } = new();
    public RestoreOptions Options { get; set; } = new();
}

/// <summary>
/// Backup query parameters
/// </summary>
public class BackupQuery
{
    public string? TenantId { get; set; }
    public BackupType? Type { get; set; }
    public BackupStatus? Status { get; set; }
    public BackupScope? Scope { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Backup result wrapper
/// </summary>
public class BackupResult
{
    public bool IsSuccess { get; set; }
    public BackupJob? Job { get; set; }
    public BackupExecution? Execution { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static BackupResult Success(BackupJob job) =>
        new() { IsSuccess = true, Job = job };

    public static BackupResult Success(BackupExecution execution) =>
        new() { IsSuccess = true, Execution = execution };

    public static BackupResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Restore result wrapper
/// </summary>
public class RestoreResult
{
    public bool IsSuccess { get; set; }
    public RestoreJob? Job { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static RestoreResult Success(RestoreJob job) =>
        new() { IsSuccess = true, Job = job };

    public static RestoreResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}