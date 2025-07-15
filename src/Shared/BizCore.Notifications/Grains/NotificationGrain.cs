using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Streams;
using BizCore.Notifications.Models;
using BizCore.Notifications.Services;

namespace BizCore.Notifications.Grains;

/// <summary>
/// Notification grain for distributed notification management
/// </summary>
public interface INotificationGrain : IGrainWithStringKey
{
    Task<Notification?> GetNotificationAsync();
    Task<bool> SetNotificationAsync(Notification notification);
    Task<bool> MarkAsReadAsync(string userId);
    Task<bool> MarkAsUnreadAsync(string userId);
    Task<bool> ArchiveAsync(string userId);
    Task<bool> UnarchiveAsync(string userId);
    Task<bool> PinAsync(string userId);
    Task<bool> UnpinAsync(string userId);
    Task<bool> DismissAsync(string userId);
    Task<bool> TrackViewAsync(string userId);
    Task<bool> TrackClickAsync(string userId);
    Task<bool> UpdateStatusAsync(NotificationStatus status);
    Task<bool> ScheduleAsync(DateTime scheduleTime);
    Task<bool> CancelScheduleAsync();
    Task<bool> ExpireAsync();
    Task<bool> AddToGroupAsync(string groupId);
    Task<bool> RemoveFromGroupAsync();
    Task<NotificationTracking> GetTrackingAsync();
    Task<bool> IsExpiredAsync();
    Task<bool> ShouldDeliverAsync();
    Task<bool> DeliverAsync(NotificationChannel channel);
    Task<bool> RetryDeliveryAsync(NotificationChannel channel, int attemptCount);
    Task<bool> UpdateMetadataAsync(Dictionary<string, object> metadata);
    Task<bool> DeleteAsync();
}

[Reentrant]
public class NotificationGrain : Grain, INotificationGrain
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDeliveryService _deliveryService;
    private readonly INotificationAnalytics _analytics;
    private readonly ILogger<NotificationGrain> _logger;
    private readonly IPersistentState<NotificationGrainState> _state;
    private readonly IAsyncStream<NotificationEvent> _eventStream;

    private Notification? _notification;
    private DateTime _lastAccessed;
    private bool _isLoaded;

    public NotificationGrain(
        INotificationRepository notificationRepository,
        INotificationDeliveryService deliveryService,
        INotificationAnalytics analytics,
        ILogger<NotificationGrain> logger,
        [PersistentState("notification", "notificationStore")] IPersistentState<NotificationGrainState> state,
        IStreamProvider streamProvider)
    {
        _notificationRepository = notificationRepository;
        _deliveryService = deliveryService;
        _analytics = analytics;
        _logger = logger;
        _state = state;
        _eventStream = streamProvider.GetStream<NotificationEvent>(this.GetPrimaryKey(), "NotificationEvents");
    }

    public override async Task OnActivateAsync()
    {
        await LoadNotificationAsync();
        _lastAccessed = DateTime.UtcNow;
        
        // Set up deactivation timer
        RegisterTimer(CheckDeactivation, null, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
        
        // Set up expiration check
        RegisterTimer(CheckExpiration, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task<Notification?> GetNotificationAsync()
    {
        await EnsureLoadedAsync();
        _lastAccessed = DateTime.UtcNow;
        return _notification;
    }

    public async Task<bool> SetNotificationAsync(Notification notification)
    {
        try
        {
            _notification = notification;
            _state.State.Notification = notification;
            _state.State.LastModified = DateTime.UtcNow;
            await _state.WriteStateAsync();
            
            // Also update in repository
            await _notificationRepository.UpdateAsync(notification);
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Updated,
                NotificationId = notification.Id,
                TenantId = notification.TenantId,
                UserId = notification.UserId,
                Data = new Dictionary<string, object>
                {
                    ["Status"] = notification.Status.ToString(),
                    ["IsRead"] = notification.IsRead,
                    ["IsArchived"] = notification.IsArchived
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set notification {NotificationId}", this.GetPrimaryKeyString());
            return false;
        }
    }

    public async Task<bool> MarkAsReadAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null || _notification.UserId != userId) return false;

        try
        {
            _notification.IsRead = true;
            _notification.ReadAt = DateTime.UtcNow;
            
            // Update tracking
            _notification.Tracking.ViewCount++;
            if (_notification.Tracking.FirstViewed == null)
            {
                _notification.Tracking.FirstViewed = DateTime.UtcNow;
            }
            _notification.Tracking.LastViewed = DateTime.UtcNow;
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Read,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId,
                Data = new Dictionary<string, object>
                {
                    ["ReadAt"] = _notification.ReadAt.Value
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", _notification.Id);
            return false;
        }
    }

    public async Task<bool> MarkAsUnreadAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null || _notification.UserId != userId) return false;

        try
        {
            _notification.IsRead = false;
            _notification.ReadAt = null;
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Updated,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId,
                Data = new Dictionary<string, object>
                {
                    ["IsRead"] = false
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as unread", _notification.Id);
            return false;
        }
    }

    public async Task<bool> ArchiveAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null || _notification.UserId != userId) return false;

        try
        {
            _notification.IsArchived = true;
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Archived,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> UnarchiveAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null || _notification.UserId != userId) return false;

        try
        {
            _notification.IsArchived = false;
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Updated,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId,
                Data = new Dictionary<string, object>
                {
                    ["IsArchived"] = false
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unarchive notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> PinAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null || _notification.UserId != userId) return false;

        try
        {
            _notification.IsPinned = true;
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Pinned,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pin notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> UnpinAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null || _notification.UserId != userId) return false;

        try
        {
            _notification.IsPinned = false;
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Unpinned,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unpin notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> DismissAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null || _notification.UserId != userId) return false;

        try
        {
            _notification.Status = NotificationStatus.Cancelled;
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Dismissed,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> TrackViewAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null || _notification.UserId != userId) return false;

        try
        {
            _notification.Tracking.ViewCount++;
            if (_notification.Tracking.FirstViewed == null)
            {
                _notification.Tracking.FirstViewed = DateTime.UtcNow;
            }
            _notification.Tracking.LastViewed = DateTime.UtcNow;
            
            await UpdateStateAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track view for notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> TrackClickAsync(string userId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null || _notification.UserId != userId) return false;

        try
        {
            _notification.Tracking.ClickCount++;
            if (_notification.Tracking.FirstClicked == null)
            {
                _notification.Tracking.FirstClicked = DateTime.UtcNow;
            }
            _notification.Tracking.LastClicked = DateTime.UtcNow;
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Clicked,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId,
                Data = new Dictionary<string, object>
                {
                    ["ClickCount"] = _notification.Tracking.ClickCount,
                    ["ActionUrl"] = _notification.ActionUrl
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track click for notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> UpdateStatusAsync(NotificationStatus status)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;

        try
        {
            _notification.Status = status;
            
            if (status == NotificationStatus.Delivered)
            {
                _notification.DeliveredAt = DateTime.UtcNow;
            }
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Updated,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId,
                Data = new Dictionary<string, object>
                {
                    ["Status"] = status.ToString(),
                    ["DeliveredAt"] = _notification.DeliveredAt?.ToString()
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> ScheduleAsync(DateTime scheduleTime)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;

        try
        {
            _notification.ScheduledFor = scheduleTime;
            _notification.Status = NotificationStatus.Queued;
            
            await UpdateStateAsync();
            
            // Schedule delivery
            RegisterTimer(async _ => await DeliverIfScheduled(), null, 
                scheduleTime - DateTime.UtcNow, TimeSpan.FromMilliseconds(-1));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> CancelScheduleAsync()
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;

        try
        {
            _notification.ScheduledFor = null;
            _notification.Status = NotificationStatus.Cancelled;
            
            await UpdateStateAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel schedule for notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> ExpireAsync()
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;

        try
        {
            _notification.Status = NotificationStatus.Expired;
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Expired,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expire notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> AddToGroupAsync(string groupId)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;

        try
        {
            _notification.GroupId = groupId;
            
            await UpdateStateAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add notification {NotificationId} to group {GroupId}", _notification.Id, groupId);
            return false;
        }
    }

    public async Task<bool> RemoveFromGroupAsync()
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;

        try
        {
            _notification.GroupId = null;
            
            await UpdateStateAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove notification {NotificationId} from group", _notification.Id);
            return false;
        }
    }

    public async Task<NotificationTracking> GetTrackingAsync()
    {
        await EnsureLoadedAsync();
        
        return _notification?.Tracking ?? new NotificationTracking();
    }

    public async Task<bool> IsExpiredAsync()
    {
        await EnsureLoadedAsync();
        
        if (_notification?.ExpiresAt == null) return false;
        
        return DateTime.UtcNow > _notification.ExpiresAt;
    }

    public async Task<bool> ShouldDeliverAsync()
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;
        
        // Check if expired
        if (await IsExpiredAsync()) return false;
        
        // Check if already delivered
        if (_notification.Status == NotificationStatus.Delivered) return false;
        
        // Check if cancelled
        if (_notification.Status == NotificationStatus.Cancelled) return false;
        
        // Check if scheduled for future
        if (_notification.ScheduledFor.HasValue && _notification.ScheduledFor > DateTime.UtcNow) return false;
        
        return true;
    }

    public async Task<bool> DeliverAsync(NotificationChannel channel)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;
        
        if (!await ShouldDeliverAsync()) return false;

        try
        {
            _notification.Status = NotificationStatus.Processing;
            await UpdateStateAsync();
            
            var result = await _deliveryService.DeliverAsync(_notification, channel);
            
            if (result.IsSuccess)
            {
                _notification.Status = NotificationStatus.Delivered;
                _notification.DeliveredAt = DateTime.UtcNow;
                
                // Update channel metrics
                _notification.Tracking.ChannelMetrics[channel.ToString()] = 
                    _notification.Tracking.ChannelMetrics.GetValueOrDefault(channel.ToString(), 0) + 1;
            }
            else
            {
                _notification.Status = NotificationStatus.Failed;
            }
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = result.IsSuccess ? NotificationEventType.Delivered : NotificationEventType.Failed,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId,
                Data = new Dictionary<string, object>
                {
                    ["Channel"] = channel.ToString(),
                    ["Success"] = result.IsSuccess,
                    ["ErrorMessage"] = result.ErrorMessage ?? string.Empty
                }
            });
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver notification {NotificationId} via {Channel}", _notification.Id, channel);
            return false;
        }
    }

    public async Task<bool> RetryDeliveryAsync(NotificationChannel channel, int attemptCount)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;

        try
        {
            _notification.Status = NotificationStatus.Retrying;
            await UpdateStateAsync();
            
            var result = await _deliveryService.RetryDeliveryAsync(_notification, channel, attemptCount);
            
            if (result.IsSuccess)
            {
                _notification.Status = NotificationStatus.Delivered;
                _notification.DeliveredAt = DateTime.UtcNow;
            }
            else
            {
                _notification.Status = NotificationStatus.Failed;
            }
            
            await UpdateStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = result.IsSuccess ? NotificationEventType.Delivered : NotificationEventType.Retried,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId,
                Data = new Dictionary<string, object>
                {
                    ["Channel"] = channel.ToString(),
                    ["AttemptCount"] = attemptCount,
                    ["Success"] = result.IsSuccess,
                    ["ErrorMessage"] = result.ErrorMessage ?? string.Empty
                }
            });
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry delivery for notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> UpdateMetadataAsync(Dictionary<string, object> metadata)
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;

        try
        {
            foreach (var kvp in metadata)
            {
                _notification.Metadata[kvp.Key] = kvp.Value;
            }
            
            await UpdateStateAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update metadata for notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync()
    {
        await EnsureLoadedAsync();
        
        if (_notification == null) return false;

        try
        {
            await _notificationRepository.DeleteAsync(_notification.Id);
            await _state.ClearStateAsync();
            
            // Publish event
            await _eventStream.OnNextAsync(new NotificationEvent
            {
                Type = NotificationEventType.Updated,
                NotificationId = _notification.Id,
                TenantId = _notification.TenantId,
                UserId = _notification.UserId,
                Data = new Dictionary<string, object>
                {
                    ["Deleted"] = true
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", _notification.Id);
            return false;
        }
    }

    // Helper methods
    private async Task LoadNotificationAsync()
    {
        if (_isLoaded) return;

        try
        {
            var notificationId = this.GetPrimaryKeyString();
            
            // Try to load from grain state first
            if (_state.State.Notification != null)
            {
                _notification = _state.State.Notification;
            }
            else
            {
                // Load from repository
                _notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (_notification != null)
                {
                    _state.State.Notification = _notification;
                    _state.State.LastModified = DateTime.UtcNow;
                    await _state.WriteStateAsync();
                }
            }

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notification {NotificationId}", this.GetPrimaryKeyString());
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (!_isLoaded)
        {
            await LoadNotificationAsync();
        }
    }

    private async Task UpdateStateAsync()
    {
        if (_notification != null)
        {
            _state.State.Notification = _notification;
            _state.State.LastModified = DateTime.UtcNow;
            await _state.WriteStateAsync();
            
            // Also update in repository
            await _notificationRepository.UpdateAsync(_notification);
        }
    }

    private async Task CheckDeactivation(object _)
    {
        if (DateTime.UtcNow - _lastAccessed > TimeSpan.FromMinutes(30))
        {
            this.DeactivateOnIdle();
        }
    }

    private async Task CheckExpiration(object _)
    {
        if (_notification != null && await IsExpiredAsync())
        {
            await ExpireAsync();
        }
    }

    private async Task DeliverIfScheduled()
    {
        if (_notification?.ScheduledFor.HasValue == true && 
            _notification.ScheduledFor <= DateTime.UtcNow &&
            await ShouldDeliverAsync())
        {
            foreach (var channel in _notification.Channels)
            {
                await DeliverAsync(channel);
            }
        }
    }
}

/// <summary>
/// Notification grain state for persistence
/// </summary>
[GenerateSerializer]
public class NotificationGrainState
{
    [Id(0)]
    public Notification? Notification { get; set; }
    
    [Id(1)]
    public DateTime LastModified { get; set; }
    
    [Id(2)]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification user grain for user-specific operations
/// </summary>
public interface INotificationUserGrain : IGrainWithStringKey
{
    Task<IEnumerable<string>> GetNotificationIdsAsync(NotificationQuery query);
    Task<int> GetUnreadCountAsync();
    Task<bool> MarkAllAsReadAsync();
    Task<bool> ArchiveAllAsync();
    Task<bool> DeleteAllAsync();
    Task<NotificationStatistics> GetStatisticsAsync(TimeSpan timeRange);
    Task<bool> UpdatePreferencesAsync(NotificationPreferences preferences);
    Task<NotificationPreferences> GetPreferencesAsync();
    Task<bool> AddNotificationAsync(string notificationId);
    Task<bool> RemoveNotificationAsync(string notificationId);
    Task<bool> ShouldReceiveNotificationAsync(string category, string module, NotificationPriority priority);
    Task<bool> IsWithinQuietHoursAsync();
}

[Reentrant]
public class NotificationUserGrain : Grain, INotificationUserGrain
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPreferencesRepository _preferencesRepository;
    private readonly ILogger<NotificationUserGrain> _logger;
    private readonly IPersistentState<NotificationUserGrainState> _state;

    public NotificationUserGrain(
        INotificationRepository notificationRepository,
        INotificationPreferencesRepository preferencesRepository,
        ILogger<NotificationUserGrain> logger,
        [PersistentState("notificationUser", "notificationUserStore")] IPersistentState<NotificationUserGrainState> state)
    {
        _notificationRepository = notificationRepository;
        _preferencesRepository = preferencesRepository;
        _logger = logger;
        _state = state;
    }

    public async Task<IEnumerable<string>> GetNotificationIdsAsync(NotificationQuery query)
    {
        try
        {
            var userId = this.GetPrimaryKeyString();
            query.UserId = userId;
            
            var notifications = await _notificationRepository.QueryAsync(query);
            return notifications.Select(n => n.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification IDs for user {UserId}", this.GetPrimaryKeyString());
            return Enumerable.Empty<string>();
        }
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            var userId = this.GetPrimaryKeyString();
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread count for user {UserId}", this.GetPrimaryKeyString());
            return 0;
        }
    }

    public async Task<bool> MarkAllAsReadAsync()
    {
        try
        {
            var userId = this.GetPrimaryKeyString();
            var query = new NotificationQuery
            {
                UserId = userId,
                IsRead = false,
                Take = 1000
            };
            
            var notifications = await _notificationRepository.QueryAsync(query);
            
            foreach (var notification in notifications)
            {
                var grain = GrainFactory.GetGrain<INotificationGrain>(notification.Id);
                await grain.MarkAsReadAsync(userId);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", this.GetPrimaryKeyString());
            return false;
        }
    }

    // Implement other methods...
    public Task<bool> ArchiveAllAsync() => throw new NotImplementedException();
    public Task<bool> DeleteAllAsync() => throw new NotImplementedException();
    public Task<NotificationStatistics> GetStatisticsAsync(TimeSpan timeRange) => throw new NotImplementedException();
    public Task<bool> UpdatePreferencesAsync(NotificationPreferences preferences) => throw new NotImplementedException();
    public Task<NotificationPreferences> GetPreferencesAsync() => throw new NotImplementedException();
    public Task<bool> AddNotificationAsync(string notificationId) => throw new NotImplementedException();
    public Task<bool> RemoveNotificationAsync(string notificationId) => throw new NotImplementedException();
    public Task<bool> ShouldReceiveNotificationAsync(string category, string module, NotificationPriority priority) => throw new NotImplementedException();
    public Task<bool> IsWithinQuietHoursAsync() => throw new NotImplementedException();
}

/// <summary>
/// Notification user grain state
/// </summary>
[GenerateSerializer]
public class NotificationUserGrainState
{
    [Id(0)]
    public NotificationPreferences? Preferences { get; set; }
    
    [Id(1)]
    public DateTime LastModified { get; set; }
    
    [Id(2)]
    public Dictionary<string, object> Metadata { get; set; } = new();
}