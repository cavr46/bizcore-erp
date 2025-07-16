using BizCore.Backup.Interfaces;
using BizCore.Backup.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace BizCore.Backup.Grains;

/// <summary>
/// Orleans grain for distributed backup job management
/// </summary>
public interface IBackupJobGrain : IGrainWithStringKey
{
    /// <summary>
    /// Initialize backup job
    /// </summary>
    Task<BackupResult> InitializeAsync(BackupJob job);

    /// <summary>
    /// Execute backup job
    /// </summary>
    Task<BackupResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update job configuration
    /// </summary>
    Task<BackupResult> UpdateConfigurationAsync(BackupJob job);

    /// <summary>
    /// Get job status
    /// </summary>
    Task<BackupJob?> GetJobAsync();

    /// <summary>
    /// Cancel running backup
    /// </summary>
    Task<bool> CancelAsync();

    /// <summary>
    /// Schedule next execution
    /// </summary>
    Task<bool> ScheduleNextExecutionAsync();

    /// <summary>
    /// Get execution history
    /// </summary>
    Task<IEnumerable<BackupExecution>> GetExecutionHistoryAsync(int count = 10);

    /// <summary>
    /// Delete job
    /// </summary>
    Task<bool> DeleteAsync();
}

/// <summary>
/// Backup job grain state
/// </summary>
public class BackupJobState
{
    public BackupJob? Job { get; set; }
    public List<BackupExecution> Executions { get; set; } = new();
    public BackupExecution? CurrentExecution { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public DateTime? NextExecutionTime { get; set; }
}

/// <summary>
/// Backup job grain implementation
/// </summary>
public class BackupJobGrain : Grain, IBackupJobGrain
{
    private readonly IPersistentState<BackupJobState> _state;
    private readonly ILogger<BackupJobGrain> _logger;
    private readonly IBackupService _backupService;
    private readonly IBackupMonitoringService _monitoringService;
    private IDisposable? _timer;

    public BackupJobGrain(
        [PersistentState("backup-job", "backup-storage")] IPersistentState<BackupJobState> state,
        ILogger<BackupJobGrain> logger,
        IBackupService backupService,
        IBackupMonitoringService monitoringService)
    {
        _state = state;
        _logger = logger;
        _backupService = backupService;
        _monitoringService = monitoringService;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackupJobGrain activated: {GrainId}", this.GetPrimaryKeyString());
        
        // Schedule next execution if job exists and is scheduled
        if (_state.State.Job != null && _state.State.Job.Schedule.IsEnabled && !_state.State.IsRunning)
        {
            ScheduleNextExecution();
        }

        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<BackupResult> InitializeAsync(BackupJob job)
    {
        try
        {
            _logger.LogInformation("Initializing backup job: {JobName} for tenant: {TenantId}", job.Name, job.TenantId);

            _state.State.Job = job;
            _state.State.Executions = new List<BackupExecution>();
            _state.State.IsRunning = false;

            await _state.WriteStateAsync();

            if (job.Schedule.IsEnabled)
            {
                ScheduleNextExecution();
            }

            _logger.LogInformation("Successfully initialized backup job: {JobId}", job.Id);
            return BackupResult.Success(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize backup job");
            return BackupResult.Failure($"Failed to initialize backup job: {ex.Message}");
        }
    }

    public async Task<BackupResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_state.State.Job == null)
        {
            return BackupResult.Failure("Job not initialized");
        }

        if (_state.State.IsRunning)
        {
            return BackupResult.Failure("Job is already running");
        }

        try
        {
            _logger.LogInformation("Starting backup execution for job: {JobId}", _state.State.Job.Id);

            _state.State.IsRunning = true;
            _state.State.LastExecutionTime = DateTime.UtcNow;

            var execution = new BackupExecution
            {
                JobId = _state.State.Job.Id,
                TenantId = _state.State.Job.TenantId,
                Type = _state.State.Job.Type,
                Status = BackupStatus.Running,
                StartedAt = DateTime.UtcNow
            };

            _state.State.CurrentExecution = execution;
            await _state.WriteStateAsync();

            // Track execution start
            await _monitoringService.TrackExecutionAsync(execution);

            // Perform the actual backup
            var result = await PerformBackupAsync(execution, cancellationToken);

            // Update execution status
            execution.Status = result.IsSuccess ? BackupStatus.Completed : BackupStatus.Failed;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Duration = execution.CompletedAt.Value - execution.StartedAt;

            if (!result.IsSuccess)
            {
                execution.ErrorMessage = result.ErrorMessage;
                execution.FailedAt = DateTime.UtcNow;
            }

            // Add to history
            _state.State.Executions.Add(execution);
            
            // Keep only last 100 executions
            if (_state.State.Executions.Count > 100)
            {
                _state.State.Executions.RemoveAt(0);
            }

            // Update job statistics
            UpdateJobStatistics(execution);

            _state.State.IsRunning = false;
            _state.State.CurrentExecution = null;
            await _state.WriteStateAsync();

            // Schedule next execution
            if (_state.State.Job.Schedule.IsEnabled)
            {
                ScheduleNextExecution();
            }

            // Send completion notification
            await _monitoringService.SendAlertAsync(new BackupAlert
            {
                TenantId = _state.State.Job.TenantId,
                Severity = result.IsSuccess ? AlertSeverity.Information : AlertSeverity.Error,
                Type = result.IsSuccess ? AlertType.BackupFailed : AlertType.BackupFailed, // TODO: Add BackupCompleted type
                Title = result.IsSuccess ? "Backup Completed" : "Backup Failed",
                Message = result.IsSuccess 
                    ? $"Backup '{_state.State.Job.Name}' completed successfully"
                    : $"Backup '{_state.State.Job.Name}' failed: {result.ErrorMessage}",
                JobId = _state.State.Job.Id,
                ExecutionId = execution.Id
            });

            _logger.LogInformation("Backup execution completed: {ExecutionId} - Success: {IsSuccess}", 
                execution.Id, result.IsSuccess);

            return result.IsSuccess ? BackupResult.Success(execution) : result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute backup job: {JobId}", _state.State.Job.Id);
            
            _state.State.IsRunning = false;
            _state.State.CurrentExecution = null;
            await _state.WriteStateAsync();

            await _monitoringService.SendAlertAsync(new BackupAlert
            {
                TenantId = _state.State.Job.TenantId,
                Severity = AlertSeverity.Critical,
                Type = AlertType.BackupFailed,
                Title = "Backup Execution Error",
                Message = $"Critical error during backup execution: {ex.Message}",
                JobId = _state.State.Job.Id
            });

            return BackupResult.Failure($"Failed to execute backup: {ex.Message}");
        }
    }

    private async Task<BackupResult> PerformBackupAsync(BackupExecution execution, CancellationToken cancellationToken)
    {
        try
        {
            // Update progress periodically
            var progressTimer = RegisterTimer(
                async _ => await UpdateProgressAsync(execution),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30));

            try
            {
                // Delegate to backup service for actual implementation
                var result = await _backupService.ExecuteBackupAsync(_state.State.Job!.Id, cancellationToken);
                return result;
            }
            finally
            {
                progressTimer.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during backup execution");
            return BackupResult.Failure(ex.Message);
        }
    }

    private async Task UpdateProgressAsync(BackupExecution execution)
    {
        try
        {
            // Simulate progress update - in real implementation, this would get actual progress
            execution.ProgressPercentage = Math.Min(100, execution.ProgressPercentage + 10);
            
            await _monitoringService.UpdateProgressAsync(execution.Id, new BackupProgress
            {
                PercentageComplete = execution.ProgressPercentage,
                CurrentOperation = "Processing data...",
                ElapsedTime = DateTime.UtcNow - execution.StartedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update backup progress");
        }
    }

    private void UpdateJobStatistics(BackupExecution execution)
    {
        if (_state.State.Job == null) return;

        _state.State.Job.Statistics.TotalExecutions++;
        
        if (execution.Status == BackupStatus.Completed)
        {
            _state.State.Job.Statistics.SuccessfulExecutions++;
            _state.State.Job.Statistics.LastSuccessfulBackup = execution.CompletedAt;
        }
        else
        {
            _state.State.Job.Statistics.FailedExecutions++;
            _state.State.Job.Statistics.LastFailedBackup = execution.FailedAt;
        }

        // Calculate success rate
        _state.State.Job.Statistics.SuccessRate = 
            (double)_state.State.Job.Statistics.SuccessfulExecutions / _state.State.Job.Statistics.TotalExecutions * 100;

        // Update average backup size and duration
        if (execution.Status == BackupStatus.Completed)
        {
            var successfulExecutions = _state.State.Executions.Where(e => e.Status == BackupStatus.Completed).ToList();
            if (successfulExecutions.Any())
            {
                _state.State.Job.Statistics.AverageBackupSize = (long)successfulExecutions.Average(e => e.TotalSize);
                _state.State.Job.Statistics.AverageBackupDuration = new TimeSpan((long)successfulExecutions.Average(e => e.Duration.Ticks));
                _state.State.Job.Statistics.FastestBackupDuration = successfulExecutions.Min(e => e.Duration);
                _state.State.Job.Statistics.SlowestBackupDuration = successfulExecutions.Max(e => e.Duration);
            }
        }
    }

    private void ScheduleNextExecution()
    {
        _timer?.Dispose();

        if (_state.State.Job?.Schedule == null || !_state.State.Job.Schedule.IsEnabled)
            return;

        var nextExecution = CalculateNextExecutionTime(_state.State.Job.Schedule);
        if (nextExecution.HasValue)
        {
            _state.State.NextExecutionTime = nextExecution;
            
            var delay = nextExecution.Value - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                _timer = RegisterTimer(
                    async _ => await ExecuteAsync(),
                    null,
                    delay,
                    TimeSpan.FromMilliseconds(-1)); // One-time execution

                _logger.LogInformation("Scheduled next backup execution for: {NextExecution}", nextExecution);
            }
        }
    }

    private DateTime? CalculateNextExecutionTime(BackupSchedule schedule)
    {
        var now = DateTime.UtcNow;
        
        return schedule.Frequency switch
        {
            BackupFrequency.Hourly => now.AddHours(1),
            BackupFrequency.Daily => now.Date.AddDays(1).Add(schedule.Time),
            BackupFrequency.Weekly => GetNextWeeklyExecution(now, schedule),
            BackupFrequency.Monthly => GetNextMonthlyExecution(now, schedule),
            BackupFrequency.Custom when !string.IsNullOrEmpty(schedule.CronExpression) => 
                CalculateNextCronExecution(schedule.CronExpression, now),
            _ => null
        };
    }

    private DateTime? GetNextWeeklyExecution(DateTime now, BackupSchedule schedule)
    {
        if (schedule.DaysOfWeek == null || schedule.DaysOfWeek.Length == 0)
            return null;

        var nextDate = now.Date.AddDays(1);
        while (!schedule.DaysOfWeek.Contains(nextDate.DayOfWeek))
        {
            nextDate = nextDate.AddDays(1);
        }
        
        return nextDate.Add(schedule.Time);
    }

    private DateTime? GetNextMonthlyExecution(DateTime now, BackupSchedule schedule)
    {
        if (schedule.DaysOfMonth == null || schedule.DaysOfMonth.Length == 0)
            return null;

        var nextMonth = now.AddMonths(1);
        var firstDayOfMonth = new DateTime(nextMonth.Year, nextMonth.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        
        var validDay = schedule.DaysOfMonth.FirstOrDefault(d => d <= daysInMonth);
        if (validDay == 0) return null;

        return firstDayOfMonth.AddDays(validDay - 1).Add(schedule.Time);
    }

    private DateTime? CalculateNextCronExecution(string cronExpression, DateTime now)
    {
        // TODO: Implement cron expression parsing
        // For now, return daily execution
        return now.Date.AddDays(1).AddHours(2);
    }

    public async Task<BackupResult> UpdateConfigurationAsync(BackupJob job)
    {
        try
        {
            if (_state.State.Job == null)
            {
                return BackupResult.Failure("Job not initialized");
            }

            var oldScheduleEnabled = _state.State.Job.Schedule.IsEnabled;
            
            _state.State.Job = job;
            _state.State.Job.UpdatedAt = DateTime.UtcNow;
            await _state.WriteStateAsync();

            // Reschedule if schedule changed
            if (job.Schedule.IsEnabled != oldScheduleEnabled || job.Schedule.IsEnabled)
            {
                ScheduleNextExecution();
            }

            _logger.LogInformation("Updated backup job configuration: {JobId}", job.Id);
            return BackupResult.Success(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job configuration");
            return BackupResult.Failure($"Failed to update configuration: {ex.Message}");
        }
    }

    public Task<BackupJob?> GetJobAsync()
    {
        return Task.FromResult(_state.State.Job);
    }

    public async Task<bool> CancelAsync()
    {
        try
        {
            if (!_state.State.IsRunning || _state.State.CurrentExecution == null)
            {
                return false;
            }

            _logger.LogInformation("Cancelling backup execution: {ExecutionId}", _state.State.CurrentExecution.Id);

            _state.State.CurrentExecution.Status = BackupStatus.Cancelled;
            _state.State.CurrentExecution.CompletedAt = DateTime.UtcNow;
            _state.State.CurrentExecution.Duration = _state.State.CurrentExecution.CompletedAt.Value - _state.State.CurrentExecution.StartedAt;

            _state.State.Executions.Add(_state.State.CurrentExecution);
            _state.State.IsRunning = false;
            _state.State.CurrentExecution = null;
            
            await _state.WriteStateAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel backup");
            return false;
        }
    }

    public async Task<bool> ScheduleNextExecutionAsync()
    {
        try
        {
            ScheduleNextExecution();
            await _state.WriteStateAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule next execution");
            return false;
        }
    }

    public Task<IEnumerable<BackupExecution>> GetExecutionHistoryAsync(int count = 10)
    {
        var executions = _state.State.Executions
            .OrderByDescending(e => e.StartedAt)
            .Take(count);
        
        return Task.FromResult(executions);
    }

    public async Task<bool> DeleteAsync()
    {
        try
        {
            _timer?.Dispose();
            
            if (_state.State.IsRunning)
            {
                await CancelAsync();
            }

            await _state.ClearStateAsync();
            DeactivateOnIdle();

            _logger.LogInformation("Deleted backup job: {JobId}", _state.State.Job?.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup job");
            return false;
        }
    }
}