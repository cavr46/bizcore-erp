using BizCore.Backup.Models;

namespace BizCore.Backup.Interfaces;

/// <summary>
/// Main backup service interface
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Create a new backup job
    /// </summary>
    Task<BackupResult> CreateBackupJobAsync(CreateBackupJobRequest request);

    /// <summary>
    /// Execute a backup job immediately
    /// </summary>
    Task<BackupResult> ExecuteBackupAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backup job by ID
    /// </summary>
    Task<BackupJob?> GetBackupJobAsync(string jobId);

    /// <summary>
    /// Query backup jobs
    /// </summary>
    Task<IEnumerable<BackupJob>> QueryBackupJobsAsync(BackupQuery query);

    /// <summary>
    /// Update backup job configuration
    /// </summary>
    Task<BackupResult> UpdateBackupJobAsync(string jobId, BackupJob job);

    /// <summary>
    /// Delete backup job
    /// </summary>
    Task<bool> DeleteBackupJobAsync(string jobId);

    /// <summary>
    /// Get backup executions for a job
    /// </summary>
    Task<IEnumerable<BackupExecution>> GetBackupExecutionsAsync(string jobId, int skip = 0, int take = 50);

    /// <summary>
    /// Get backup execution by ID
    /// </summary>
    Task<BackupExecution?> GetBackupExecutionAsync(string executionId);

    /// <summary>
    /// Cancel running backup
    /// </summary>
    Task<bool> CancelBackupAsync(string executionId);

    /// <summary>
    /// Verify backup integrity
    /// </summary>
    Task<bool> VerifyBackupIntegrityAsync(string executionId);

    /// <summary>
    /// Get backup statistics
    /// </summary>
    Task<BackupStatistics> GetBackupStatisticsAsync(string tenantId);

    /// <summary>
    /// Test backup destination connectivity
    /// </summary>
    Task<bool> TestDestinationAsync(BackupDestination destination);

    /// <summary>
    /// Schedule backup job
    /// </summary>
    Task<bool> ScheduleBackupJobAsync(string jobId);

    /// <summary>
    /// Unschedule backup job
    /// </summary>
    Task<bool> UnscheduleBackupJobAsync(string jobId);
}