using BizCore.Backup.Interfaces;
using BizCore.Backup.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace BizCore.Backup.Grains;

/// <summary>
/// Orleans grain for tenant-level backup management
/// </summary>
public interface IBackupTenantGrain : IGrainWithStringKey
{
    /// <summary>
    /// Initialize tenant backup configuration
    /// </summary>
    Task InitializeAsync(string tenantId);

    /// <summary>
    /// Create backup job for tenant
    /// </summary>
    Task<BackupResult> CreateBackupJobAsync(CreateBackupJobRequest request);

    /// <summary>
    /// Get all backup jobs for tenant
    /// </summary>
    Task<IEnumerable<BackupJob>> GetBackupJobsAsync();

    /// <summary>
    /// Get backup statistics for tenant
    /// </summary>
    Task<BackupStatistics> GetStatisticsAsync();

    /// <summary>
    /// Get backup health status
    /// </summary>
    Task<BackupHealthStatus> GetHealthStatusAsync();

    /// <summary>
    /// Execute all scheduled backups
    /// </summary>
    Task<IEnumerable<BackupResult>> ExecuteScheduledBackupsAsync();

    /// <summary>
    /// Get disaster recovery status
    /// </summary>
    Task<DRStatus> GetDRStatusAsync();

    /// <summary>
    /// Trigger emergency backup
    /// </summary>
    Task<BackupResult> TriggerEmergencyBackupAsync(string reason);

    /// <summary>
    /// Clean up expired backups
    /// </summary>
    Task<int> CleanupExpiredBackupsAsync();

    /// <summary>
    /// Get storage usage for tenant
    /// </summary>
    Task<StorageUsageReport> GetStorageUsageAsync();
}

/// <summary>
/// Backup tenant grain state
/// </summary>
public class BackupTenantState
{
    public string TenantId { get; set; } = string.Empty;
    public List<string> BackupJobIds { get; set; } = new();
    public BackupStatistics Statistics { get; set; } = new();
    public DateTime LastHealthCheck { get; set; }
    public DateTime LastCleanup { get; set; }
    public BackupTenantConfiguration Configuration { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup tenant configuration
/// </summary>
public class BackupTenantConfiguration
{
    public int MaxConcurrentBackups { get; set; } = 3;
    public long MaxStorageGB { get; set; } = 1000;
    public int DefaultRetentionDays { get; set; } = 30;
    public bool EnableAutoCleanup { get; set; } = true;
    public bool EnableEmergencyBackups { get; set; } = true;
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}

/// <summary>
/// Backup tenant grain implementation
/// </summary>
public class BackupTenantGrain : Grain, IBackupTenantGrain
{
    private readonly IPersistentState<BackupTenantState> _state;
    private readonly ILogger<BackupTenantGrain> _logger;
    private readonly IBackupMonitoringService _monitoringService;
    private readonly IGrainFactory _grainFactory;
    private IDisposable? _healthCheckTimer;
    private IDisposable? _cleanupTimer;

    public BackupTenantGrain(
        [PersistentState("backup-tenant", "backup-storage")] IPersistentState<BackupTenantState> state,
        ILogger<BackupTenantGrain> logger,
        IBackupMonitoringService monitoringService,
        IGrainFactory grainFactory)
    {
        _state = state;
        _logger = logger;
        _monitoringService = monitoringService;
        _grainFactory = grainFactory;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var tenantId = this.GetPrimaryKeyString();
        _logger.LogInformation("BackupTenantGrain activated for tenant: {TenantId}", tenantId);

        // Initialize if not already done
        if (string.IsNullOrEmpty(_state.State.TenantId))
        {
            _state.State.TenantId = tenantId;
        }

        // Start background timers
        StartBackgroundTasks();

        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _healthCheckTimer?.Dispose();
        _cleanupTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task InitializeAsync(string tenantId)
    {
        try
        {
            _logger.LogInformation("Initializing backup configuration for tenant: {TenantId}", tenantId);

            _state.State.TenantId = tenantId;
            _state.State.BackupJobIds = new List<string>();
            _state.State.Statistics = new BackupStatistics();
            _state.State.LastHealthCheck = DateTime.UtcNow;
            _state.State.LastCleanup = DateTime.UtcNow;
            _state.State.Configuration = new BackupTenantConfiguration();

            await _state.WriteStateAsync();

            StartBackgroundTasks();

            _logger.LogInformation("Successfully initialized backup for tenant: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize backup for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<BackupResult> CreateBackupJobAsync(CreateBackupJobRequest request)
    {
        try
        {
            _logger.LogInformation("Creating backup job for tenant: {TenantId}", request.TenantId);

            // Create the backup job grain
            var jobGrain = _grainFactory.GetGrain<IBackupJobGrain>(Guid.NewGuid().ToString());
            
            var job = new BackupJob
            {
                Name = request.Name,
                Description = request.Description,
                TenantId = request.TenantId,
                Type = request.Type,
                Scope = request.Scope,
                Schedule = request.Schedule,
                Configuration = request.Configuration,
                Encryption = request.Encryption,
                Retention = request.Retention,
                Destinations = request.Destinations,
                Priority = request.Priority,
                Tags = request.Tags,
                CreatedBy = "system", // TODO: Get from context
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await jobGrain.InitializeAsync(job);
            
            if (result.IsSuccess)
            {
                _state.State.BackupJobIds.Add(job.Id);
                await _state.WriteStateAsync();

                _logger.LogInformation("Successfully created backup job: {JobId} for tenant: {TenantId}", job.Id, request.TenantId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup job for tenant: {TenantId}", request.TenantId);
            return BackupResult.Failure($"Failed to create backup job: {ex.Message}");
        }
    }

    public async Task<IEnumerable<BackupJob>> GetBackupJobsAsync()
    {
        var jobs = new List<BackupJob>();
        
        foreach (var jobId in _state.State.BackupJobIds)
        {
            try
            {
                var jobGrain = _grainFactory.GetGrain<IBackupJobGrain>(jobId);
                var job = await jobGrain.GetJobAsync();
                if (job != null)
                {
                    jobs.Add(job);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get backup job: {JobId}", jobId);
            }
        }

        return jobs;
    }

    public async Task<BackupStatistics> GetStatisticsAsync()
    {
        try
        {
            // Aggregate statistics from all jobs
            var jobs = await GetBackupJobsAsync();
            var statistics = new BackupStatistics();

            foreach (var job in jobs)
            {
                statistics.TotalExecutions += job.Statistics.TotalExecutions;
                statistics.SuccessfulExecutions += job.Statistics.SuccessfulExecutions;
                statistics.FailedExecutions += job.Statistics.FailedExecutions;
                statistics.TotalBackupSize += job.Statistics.TotalBackupSize;

                if (job.Statistics.LastSuccessfulBackup.HasValue &&
                    (!statistics.LastSuccessfulBackup.HasValue || 
                     job.Statistics.LastSuccessfulBackup > statistics.LastSuccessfulBackup))
                {
                    statistics.LastSuccessfulBackup = job.Statistics.LastSuccessfulBackup;
                }

                if (job.Statistics.LastFailedBackup.HasValue &&
                    (!statistics.LastFailedBackup.HasValue || 
                     job.Statistics.LastFailedBackup > statistics.LastFailedBackup))
                {
                    statistics.LastFailedBackup = job.Statistics.LastFailedBackup;
                }
            }

            // Calculate derived statistics
            if (statistics.TotalExecutions > 0)
            {
                statistics.SuccessRate = (double)statistics.SuccessfulExecutions / statistics.TotalExecutions * 100;
                statistics.AverageBackupSize = statistics.TotalBackupSize / statistics.SuccessfulExecutions;
            }

            _state.State.Statistics = statistics;
            await _state.WriteStateAsync();

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics for tenant: {TenantId}", _state.State.TenantId);
            return _state.State.Statistics;
        }
    }

    public async Task<BackupHealthStatus> GetHealthStatusAsync()
    {
        try
        {
            var healthStatus = new BackupHealthStatus
            {
                Overall = HealthLevel.Healthy,
                LastChecked = DateTime.UtcNow,
                Components = new Dictionary<string, ComponentHealth>(),
                Issues = new List<HealthIssue>(),
                HealthScore = 100.0
            };

            var jobs = await GetBackupJobsAsync();
            var statistics = await GetStatisticsAsync();

            // Check backup job health
            var activeJobs = jobs.Count(j => j.IsActive);
            var scheduledJobs = jobs.Count(j => j.Schedule.IsEnabled);

            healthStatus.Components["BackupJobs"] = new ComponentHealth
            {
                Name = "Backup Jobs",
                Status = activeJobs > 0 ? HealthLevel.Healthy : HealthLevel.Degraded,
                Message = $"{activeJobs} active jobs, {scheduledJobs} scheduled",
                LastChecked = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>
                {
                    ["ActiveJobs"] = activeJobs,
                    ["ScheduledJobs"] = scheduledJobs
                }
            };

            // Check recent backup success rate
            var recentSuccessRate = statistics.SuccessRate;
            var backupHealthLevel = recentSuccessRate switch
            {
                >= 95 => HealthLevel.Healthy,
                >= 80 => HealthLevel.Degraded,
                >= 50 => HealthLevel.Unhealthy,
                _ => HealthLevel.Critical
            };

            healthStatus.Components["BackupSuccess"] = new ComponentHealth
            {
                Name = "Backup Success Rate",
                Status = backupHealthLevel,
                Message = $"{recentSuccessRate:F1}% success rate",
                LastChecked = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>
                {
                    ["SuccessRate"] = recentSuccessRate,
                    ["TotalExecutions"] = statistics.TotalExecutions
                }
            };

            // Check storage usage
            var storageUsage = await GetStorageUsageAsync();
            var storageHealthLevel = storageUsage.UsagePercentage switch
            {
                < 70 => HealthLevel.Healthy,
                < 85 => HealthLevel.Degraded,
                < 95 => HealthLevel.Unhealthy,
                _ => HealthLevel.Critical
            };

            healthStatus.Components["Storage"] = new ComponentHealth
            {
                Name = "Storage Usage",
                Status = storageHealthLevel,
                Message = $"{storageUsage.UsagePercentage:F1}% used",
                LastChecked = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>
                {
                    ["UsagePercentage"] = storageUsage.UsagePercentage,
                    ["UsedStorageGB"] = storageUsage.UsedStorageBytes / (1024 * 1024 * 1024)
                }
            };

            // Determine overall health
            var componentStatuses = healthStatus.Components.Values.Select(c => c.Status).ToList();
            healthStatus.Overall = componentStatuses.Any(s => s == HealthLevel.Critical) ? HealthLevel.Critical :
                                 componentStatuses.Any(s => s == HealthLevel.Unhealthy) ? HealthLevel.Unhealthy :
                                 componentStatuses.Any(s => s == HealthLevel.Degraded) ? HealthLevel.Degraded :
                                 HealthLevel.Healthy;

            // Calculate health score
            var scores = componentStatuses.Select(s => s switch
            {
                HealthLevel.Healthy => 100.0,
                HealthLevel.Degraded => 70.0,
                HealthLevel.Unhealthy => 40.0,
                HealthLevel.Critical => 0.0,
                _ => 50.0
            });
            healthStatus.HealthScore = scores.Average();

            _state.State.LastHealthCheck = DateTime.UtcNow;
            await _state.WriteStateAsync();

            return healthStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status for tenant: {TenantId}", _state.State.TenantId);
            return new BackupHealthStatus
            {
                Overall = HealthLevel.Critical,
                LastChecked = DateTime.UtcNow,
                Issues = new[] { new HealthIssue 
                { 
                    Component = "System", 
                    Issue = ex.Message, 
                    Severity = HealthLevel.Critical 
                }}
            };
        }
    }

    public async Task<IEnumerable<BackupResult>> ExecuteScheduledBackupsAsync()
    {
        var results = new List<BackupResult>();

        try
        {
            _logger.LogInformation("Executing scheduled backups for tenant: {TenantId}", _state.State.TenantId);

            var concurrentBackups = 0;
            var maxConcurrent = _state.State.Configuration.MaxConcurrentBackups;

            foreach (var jobId in _state.State.BackupJobIds)
            {
                if (concurrentBackups >= maxConcurrent)
                {
                    _logger.LogInformation("Maximum concurrent backups reached ({MaxConcurrent}), skipping remaining jobs", maxConcurrent);
                    break;
                }

                try
                {
                    var jobGrain = _grainFactory.GetGrain<IBackupJobGrain>(jobId);
                    var job = await jobGrain.GetJobAsync();
                    
                    if (job != null && job.Schedule.IsEnabled && job.IsActive)
                    {
                        // Execute backup asynchronously
                        var executeTask = jobGrain.ExecuteAsync();
                        concurrentBackups++;

                        // Don't await here to allow concurrent execution
                        _ = executeTask.ContinueWith(async task =>
                        {
                            try
                            {
                                var result = await task;
                                results.Add(result);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error executing backup job: {JobId}", jobId);
                                results.Add(BackupResult.Failure($"Error executing job {jobId}: {ex.Message}"));
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute backup job: {JobId}", jobId);
                    results.Add(BackupResult.Failure($"Failed to execute job {jobId}: {ex.Message}"));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute scheduled backups for tenant: {TenantId}", _state.State.TenantId);
            results.Add(BackupResult.Failure($"Failed to execute scheduled backups: {ex.Message}"));
            return results;
        }
    }

    public async Task<DRStatus> GetDRStatusAsync()
    {
        try
        {
            var statistics = await GetStatisticsAsync();
            var healthStatus = await GetHealthStatusAsync();

            var drStatus = new DRStatus
            {
                TenantId = _state.State.TenantId,
                SystemStatus = healthStatus.Overall switch
                {
                    HealthLevel.Healthy => DRSystemStatus.Normal,
                    HealthLevel.Degraded => DRSystemStatus.Warning,
                    HealthLevel.Unhealthy => DRSystemStatus.Critical,
                    HealthLevel.Critical => DRSystemStatus.Critical,
                    _ => DRSystemStatus.Warning
                },
                PrimarySite = "Primary Data Center", // TODO: Get from configuration
                SecondarySites = new[] { "Secondary Data Center" }, // TODO: Get from configuration
                LastBackup = statistics.LastSuccessfulBackup ?? DateTime.MinValue,
                LastTest = DateTime.UtcNow.AddDays(-7), // TODO: Get actual test date
                IsFailoverActive = false,
                Readiness = new DRReadiness
                {
                    IsReady = healthStatus.Overall != HealthLevel.Critical,
                    ReadinessScore = healthStatus.HealthScore,
                    LastChecked = DateTime.UtcNow,
                    ComponentReadiness = healthStatus.Components.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Status == HealthLevel.Healthy || kvp.Value.Status == HealthLevel.Degraded
                    ),
                    Issues = healthStatus.Issues.Select(i => i.Issue).ToArray()
                }
            };

            return drStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DR status for tenant: {TenantId}", _state.State.TenantId);
            throw;
        }
    }

    public async Task<BackupResult> TriggerEmergencyBackupAsync(string reason)
    {
        try
        {
            _logger.LogCritical("EMERGENCY BACKUP TRIGGERED for tenant: {TenantId} - Reason: {Reason}", 
                _state.State.TenantId, reason);

            if (!_state.State.Configuration.EnableEmergencyBackups)
            {
                return BackupResult.Failure("Emergency backups are disabled for this tenant");
            }

            // Create emergency backup job
            var request = new CreateBackupJobRequest
            {
                Name = $"Emergency Backup - {DateTime.UtcNow:yyyyMMddHHmmss}",
                Description = $"Emergency backup triggered: {reason}",
                TenantId = _state.State.TenantId,
                Type = BackupType.Full,
                Scope = BackupScope.Complete,
                Priority = 100, // Highest priority
                Tags = new[] { "emergency", "critical" }
            };

            var result = await CreateBackupJobAsync(request);
            
            if (result.IsSuccess && result.Job != null)
            {
                // Execute immediately
                var jobGrain = _grainFactory.GetGrain<IBackupJobGrain>(result.Job.Id);
                var executeResult = await jobGrain.ExecuteAsync();

                // Send critical alert
                await _monitoringService.SendAlertAsync(new BackupAlert
                {
                    TenantId = _state.State.TenantId,
                    Severity = AlertSeverity.Critical,
                    Type = AlertType.BackupFailed, // TODO: Add EmergencyBackup type
                    Title = "Emergency Backup Triggered",
                    Message = $"Emergency backup executed for tenant {_state.State.TenantId}. Reason: {reason}",
                    Context = new Dictionary<string, object>
                    {
                        ["Reason"] = reason,
                        ["JobId"] = result.Job.Id,
                        ["Success"] = executeResult.IsSuccess
                    }
                });

                return executeResult;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger emergency backup for tenant: {TenantId}", _state.State.TenantId);
            return BackupResult.Failure($"Failed to trigger emergency backup: {ex.Message}");
        }
    }

    public async Task<int> CleanupExpiredBackupsAsync()
    {
        var cleanedCount = 0;

        try
        {
            _logger.LogInformation("Starting backup cleanup for tenant: {TenantId}", _state.State.TenantId);

            if (!_state.State.Configuration.EnableAutoCleanup)
            {
                _logger.LogInformation("Auto cleanup is disabled for tenant: {TenantId}", _state.State.TenantId);
                return 0;
            }

            var jobs = await GetBackupJobsAsync();
            
            foreach (var job in jobs)
            {
                try
                {
                    var jobGrain = _grainFactory.GetGrain<IBackupJobGrain>(job.Id);
                    var executions = await jobGrain.GetExecutionHistoryAsync(1000); // Get more history for cleanup

                    var retentionDate = DateTime.UtcNow.AddDays(-job.Retention.DailyRetentionDays);
                    var expiredExecutions = executions.Where(e => e.CompletedAt < retentionDate).ToList();

                    foreach (var execution in expiredExecutions)
                    {
                        // TODO: Delete actual backup files from storage
                        cleanedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup backups for job: {JobId}", job.Id);
                }
            }

            _state.State.LastCleanup = DateTime.UtcNow;
            await _state.WriteStateAsync();

            _logger.LogInformation("Backup cleanup completed for tenant: {TenantId}. Cleaned {CleanedCount} backups", 
                _state.State.TenantId, cleanedCount);

            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired backups for tenant: {TenantId}", _state.State.TenantId);
            return cleanedCount;
        }
    }

    public async Task<StorageUsageReport> GetStorageUsageAsync()
    {
        try
        {
            // TODO: Implement actual storage usage calculation
            var report = new StorageUsageReport
            {
                TenantId = _state.State.TenantId,
                TotalStorageBytes = _state.State.Configuration.MaxStorageGB * 1024 * 1024 * 1024,
                UsedStorageBytes = 0, // TODO: Calculate actual usage
                GeneratedAt = DateTime.UtcNow
            };

            // Calculate from job statistics
            var statistics = await GetStatisticsAsync();
            report.UsedStorageBytes = statistics.TotalBackupSize;
            report.AvailableStorageBytes = report.TotalStorageBytes - report.UsedStorageBytes;
            report.UsagePercentage = (double)report.UsedStorageBytes / report.TotalStorageBytes * 100;

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage usage for tenant: {TenantId}", _state.State.TenantId);
            throw;
        }
    }

    private void StartBackgroundTasks()
    {
        // Health check timer
        _healthCheckTimer = RegisterTimer(
            async _ => await PerformHealthCheckAsync(),
            null,
            _state.State.Configuration.HealthCheckInterval,
            _state.State.Configuration.HealthCheckInterval);

        // Cleanup timer
        _cleanupTimer = RegisterTimer(
            async _ => await CleanupExpiredBackupsAsync(),
            null,
            _state.State.Configuration.CleanupInterval,
            _state.State.Configuration.CleanupInterval);
    }

    private async Task PerformHealthCheckAsync()
    {
        try
        {
            var healthStatus = await GetHealthStatusAsync();
            
            // Send alerts for critical health issues
            if (healthStatus.Overall == HealthLevel.Critical || healthStatus.Overall == HealthLevel.Unhealthy)
            {
                await _monitoringService.SendAlertAsync(new BackupAlert
                {
                    TenantId = _state.State.TenantId,
                    Severity = healthStatus.Overall == HealthLevel.Critical ? AlertSeverity.Critical : AlertSeverity.Error,
                    Type = AlertType.BackupFailed, // TODO: Add HealthCheck type
                    Title = "Backup Health Check Alert",
                    Message = $"Backup system health is {healthStatus.Overall}. Health score: {healthStatus.HealthScore:F1}%",
                    Context = new Dictionary<string, object>
                    {
                        ["HealthLevel"] = healthStatus.Overall.ToString(),
                        ["HealthScore"] = healthStatus.HealthScore,
                        ["Issues"] = healthStatus.Issues.Select(i => i.Issue).ToArray()
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform health check for tenant: {TenantId}", _state.State.TenantId);
        }
    }
}