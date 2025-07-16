using BizCore.Backup.Models;

namespace BizCore.Backup.Interfaces;

/// <summary>
/// Backup scheduler interface
/// </summary>
public interface IBackupScheduler
{
    /// <summary>
    /// Schedule a backup job
    /// </summary>
    Task<bool> ScheduleJobAsync(BackupJob job);

    /// <summary>
    /// Unschedule a backup job
    /// </summary>
    Task<bool> UnscheduleJobAsync(string jobId);

    /// <summary>
    /// Update job schedule
    /// </summary>
    Task<bool> UpdateScheduleAsync(string jobId, BackupSchedule schedule);

    /// <summary>
    /// Get next execution time
    /// </summary>
    Task<DateTime?> GetNextExecutionTimeAsync(string jobId);

    /// <summary>
    /// Get scheduled jobs
    /// </summary>
    Task<IEnumerable<ScheduledBackupJob>> GetScheduledJobsAsync(string tenantId);

    /// <summary>
    /// Trigger job immediately
    /// </summary>
    Task<bool> TriggerJobAsync(string jobId);

    /// <summary>
    /// Pause job schedule
    /// </summary>
    Task<bool> PauseJobAsync(string jobId);

    /// <summary>
    /// Resume job schedule
    /// </summary>
    Task<bool> ResumeJobAsync(string jobId);

    /// <summary>
    /// Get job execution history
    /// </summary>
    Task<IEnumerable<BackupExecution>> GetJobHistoryAsync(string jobId, int count = 10);

    /// <summary>
    /// Check if job is due for execution
    /// </summary>
    Task<bool> IsJobDueAsync(string jobId);

    /// <summary>
    /// Get running jobs
    /// </summary>
    Task<IEnumerable<RunningBackupJob>> GetRunningJobsAsync(string? tenantId = null);

    /// <summary>
    /// Validate schedule expression
    /// </summary>
    bool ValidateSchedule(BackupSchedule schedule);
}

/// <summary>
/// Scheduled backup job
/// </summary>
public class ScheduledBackupJob
{
    public string JobId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public BackupSchedule Schedule { get; set; } = new();
    public DateTime? NextExecution { get; set; }
    public DateTime? LastExecution { get; set; }
    public bool IsPaused { get; set; }
    public int ExecutionCount { get; set; }
    public int FailureCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Running backup job
/// </summary>
public class RunningBackupJob
{
    public string JobId { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public TimeSpan RunningTime { get; set; }
    public double ProgressPercentage { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public long ProcessedSize { get; set; }
    public long TotalSize { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}