using BizCore.Backup.Models;

namespace BizCore.Backup.Interfaces;

/// <summary>
/// Restore service interface
/// </summary>
public interface IRestoreService
{
    /// <summary>
    /// Create a new restore job
    /// </summary>
    Task<RestoreResult> CreateRestoreJobAsync(CreateRestoreJobRequest request);

    /// <summary>
    /// Execute restore job
    /// </summary>
    Task<RestoreResult> ExecuteRestoreAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get restore job by ID
    /// </summary>
    Task<RestoreJob?> GetRestoreJobAsync(string jobId);

    /// <summary>
    /// Query restore jobs
    /// </summary>
    Task<IEnumerable<RestoreJob>> QueryRestoreJobsAsync(string tenantId, int skip = 0, int take = 50);

    /// <summary>
    /// Cancel running restore
    /// </summary>
    Task<bool> CancelRestoreAsync(string jobId);

    /// <summary>
    /// Verify restore completion
    /// </summary>
    Task<RestoreVerification> VerifyRestoreAsync(string jobId);

    /// <summary>
    /// Get available backups for restore
    /// </summary>
    Task<IEnumerable<BackupExecution>> GetAvailableBackupsAsync(string tenantId, RestoreType restoreType);

    /// <summary>
    /// Estimate restore size and time
    /// </summary>
    Task<RestoreEstimate> EstimateRestoreAsync(string backupId, RestoreOptions options);

    /// <summary>
    /// Perform point-in-time restore
    /// </summary>
    Task<RestoreResult> RestoreToPointInTimeAsync(string tenantId, DateTime targetTime, RestoreOptions options);

    /// <summary>
    /// Test restore without applying changes
    /// </summary>
    Task<RestoreResult> TestRestoreAsync(string backupId, RestoreOptions options);
}

/// <summary>
/// Restore estimate
/// </summary>
public class RestoreEstimate
{
    public long EstimatedSize { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public long RequiredDiskSpace { get; set; }
    public string[] AffectedObjects { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}