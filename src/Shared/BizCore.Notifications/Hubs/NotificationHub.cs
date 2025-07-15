using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BizCore.Notifications.Models;
using BizCore.Notifications.Services;
using BizCore.Identity.Services;

namespace BizCore.Notifications.Hubs;

/// <summary>
/// SignalR hub for real-time notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly INotificationService _notificationService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<NotificationHub> _logger;
    private readonly IConnectionManager _connectionManager;

    public NotificationHub(
        INotificationService notificationService,
        IIdentityService identityService,
        ILogger<NotificationHub> logger,
        IConnectionManager connectionManager)
    {
        _notificationService = notificationService;
        _identityService = identityService;
        _logger = logger;
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Handle client connection
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            var tenantId = GetTenantId();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Connection attempt without valid user or tenant context");
                Context.Abort();
                return;
            }

            // Add to user group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Add to tenant group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");

            // Get user roles and add to role groups
            var userRoles = await _identityService.GetUserRolesAsync(userId);
            foreach (var role in userRoles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");
            }

            // Track connection
            await _connectionManager.AddConnectionAsync(userId, tenantId, Context.ConnectionId);

            // Send pending notifications
            await SendPendingNotificationsAsync(userId);

            // Send unread count
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            await Clients.Caller.UnreadCountUpdated(unreadCount);

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection for {ConnectionId}", Context.ConnectionId);
            Context.Abort();
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Handle client disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetUserId();
            var tenantId = GetTenantId();

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(tenantId))
            {
                // Remove from groups
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");

                // Remove from role groups
                var userRoles = await _identityService.GetUserRolesAsync(userId);
                foreach (var role in userRoles)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role_{role}");
                }

                // Remove connection tracking
                await _connectionManager.RemoveConnectionAsync(Context.ConnectionId);

                _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", userId, Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection for {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    public async Task MarkAsRead(string notificationId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _notificationService.MarkAsReadAsync(notificationId);
            
            if (success)
            {
                await Clients.Caller.NotificationMarkedAsRead(notificationId);
                
                // Update unread count
                var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
                await Clients.Caller.UnreadCountUpdated(unreadCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            await Clients.Caller.Error($"Failed to mark notification as read: {ex.Message}");
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    public async Task MarkAllAsRead()
    {
        try
        {
            var userId = GetUserId();
            var success = await _notificationService.MarkAllAsReadAsync(userId);
            
            if (success)
            {
                await Clients.Caller.AllNotificationsMarkedAsRead();
                await Clients.Caller.UnreadCountUpdated(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", GetUserId());
            await Clients.Caller.Error($"Failed to mark all notifications as read: {ex.Message}");
        }
    }

    /// <summary>
    /// Archive notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    public async Task Archive(string notificationId)
    {
        try
        {
            var success = await _notificationService.ArchiveNotificationAsync(notificationId);
            
            if (success)
            {
                await Clients.Caller.NotificationArchived(notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving notification {NotificationId}", notificationId);
            await Clients.Caller.Error($"Failed to archive notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Pin notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    public async Task Pin(string notificationId)
    {
        try
        {
            var success = await _notificationService.PinNotificationAsync(notificationId);
            
            if (success)
            {
                await Clients.Caller.NotificationPinned(notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pinning notification {NotificationId}", notificationId);
            await Clients.Caller.Error($"Failed to pin notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Unpin notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    public async Task Unpin(string notificationId)
    {
        try
        {
            var success = await _notificationService.UnpinNotificationAsync(notificationId);
            
            if (success)
            {
                await Clients.Caller.NotificationUnpinned(notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpinning notification {NotificationId}", notificationId);
            await Clients.Caller.Error($"Failed to unpin notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Dismiss notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    public async Task Dismiss(string notificationId)
    {
        try
        {
            var success = await _notificationService.DismissNotificationAsync(notificationId);
            
            if (success)
            {
                await Clients.Caller.NotificationDismissed(notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing notification {NotificationId}", notificationId);
            await Clients.Caller.Error($"Failed to dismiss notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Track notification click
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    public async Task TrackClick(string notificationId)
    {
        try
        {
            var userId = GetUserId();
            await _notificationService.TrackClickAsync(notificationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking click for notification {NotificationId}", notificationId);
        }
    }

    /// <summary>
    /// Subscribe to specific categories
    /// </summary>
    /// <param name="categories">Categories to subscribe to</param>
    public async Task SubscribeToCategories(string[] categories)
    {
        try
        {
            var userId = GetUserId();
            var tenantId = GetTenantId();

            foreach (var category in categories)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"category_{category}");
            }

            // Create subscription record
            var subscription = new NotificationSubscription
            {
                UserId = userId,
                TenantId = tenantId,
                ConnectionId = Context.ConnectionId,
                Channel = NotificationChannel.WebSocket,
                Categories = categories,
                IsActive = true
            };

            await _notificationService.CreateSubscriptionAsync(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to categories for user {UserId}", GetUserId());
            await Clients.Caller.Error($"Failed to subscribe to categories: {ex.Message}");
        }
    }

    /// <summary>
    /// Unsubscribe from categories
    /// </summary>
    /// <param name="categories">Categories to unsubscribe from</param>
    public async Task UnsubscribeFromCategories(string[] categories)
    {
        try
        {
            foreach (var category in categories)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"category_{category}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from categories for user {UserId}", GetUserId());
            await Clients.Caller.Error($"Failed to unsubscribe from categories: {ex.Message}");
        }
    }

    /// <summary>
    /// Get notifications with pagination
    /// </summary>
    /// <param name="query">Query parameters</param>
    public async Task GetNotifications(NotificationQuery query)
    {
        try
        {
            var userId = GetUserId();
            query.UserId = userId;

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, query);
            await Clients.Caller.NotificationsReceived(notifications.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", GetUserId());
            await Clients.Caller.Error($"Failed to get notifications: {ex.Message}");
        }
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    /// <param name="preferences">Updated preferences</param>
    public async Task UpdatePreferences(NotificationPreferences preferences)
    {
        try
        {
            var userId = GetUserId();
            preferences.UserId = userId;

            await _notificationService.UpdateUserPreferencesAsync(userId, preferences);
            await Clients.Caller.PreferencesUpdated(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId}", GetUserId());
            await Clients.Caller.Error($"Failed to update preferences: {ex.Message}");
        }
    }

    /// <summary>
    /// Join tenant broadcast group
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    public async Task JoinTenantBroadcast(string tenantId)
    {
        try
        {
            var userTenantId = GetTenantId();
            if (userTenantId == tenantId)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"broadcast_{tenantId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining tenant broadcast for tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Leave tenant broadcast group
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    public async Task LeaveTenantBroadcast(string tenantId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"broadcast_{tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving tenant broadcast for tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Request notification statistics
    /// </summary>
    public async Task GetStatistics()
    {
        try
        {
            var userId = GetUserId();
            var stats = await _notificationService.GetUserStatisticsAsync(userId, TimeSpan.FromDays(30));
            await Clients.Caller.StatisticsReceived(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for user {UserId}", GetUserId());
            await Clients.Caller.Error($"Failed to get statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Send real-time notification to specific user
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="notification">Notification to send</param>
    public async Task SendToUser(string userId, Notification notification)
    {
        try
        {
            // Verify sender has permission
            if (!await HasPermissionToSend())
            {
                await Clients.Caller.Error("Insufficient permissions to send notifications");
                return;
            }

            await Clients.Group($"user_{userId}").NotificationReceived(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            await Clients.Caller.Error($"Failed to send notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Broadcast notification to all tenant users
    /// </summary>
    /// <param name="notification">Notification to broadcast</param>
    public async Task BroadcastToTenant(Notification notification)
    {
        try
        {
            // Verify sender has permission
            if (!await HasPermissionToSend())
            {
                await Clients.Caller.Error("Insufficient permissions to broadcast notifications");
                return;
            }

            var tenantId = GetTenantId();
            await Clients.Group($"tenant_{tenantId}").NotificationReceived(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification to tenant {TenantId}", GetTenantId());
            await Clients.Caller.Error($"Failed to broadcast notification: {ex.Message}");
        }
    }

    // Helper methods
    private string GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private string GetTenantId()
    {
        return Context.User?.FindFirst("tenant_id")?.Value ?? string.Empty;
    }

    private async Task<bool> HasPermissionToSend()
    {
        var userId = GetUserId();
        return await _identityService.HasPermissionAsync(userId, "Notifications.Send");
    }

    private async Task SendPendingNotificationsAsync(string userId)
    {
        try
        {
            var query = new NotificationQuery
            {
                UserId = userId,
                IsRead = false,
                Take = 50,
                SortBy = "CreatedAt",
                SortDescending = true
            };

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, query);
            
            foreach (var notification in notifications)
            {
                await Clients.Caller.NotificationReceived(notification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending pending notifications to user {UserId}", userId);
        }
    }
}

/// <summary>
/// Client interface for SignalR notifications
/// </summary>
public interface INotificationClient
{
    // Receive notifications
    Task NotificationReceived(Notification notification);
    Task NotificationsReceived(Notification[] notifications);
    Task UnreadCountUpdated(int count);
    
    // Notification state changes
    Task NotificationMarkedAsRead(string notificationId);
    Task AllNotificationsMarkedAsRead();
    Task NotificationArchived(string notificationId);
    Task NotificationPinned(string notificationId);
    Task NotificationUnpinned(string notificationId);
    Task NotificationDismissed(string notificationId);
    
    // Preferences and configuration
    Task PreferencesUpdated(NotificationPreferences preferences);
    Task StatisticsReceived(NotificationStatistics statistics);
    
    // System messages
    Task Error(string message);
    Task ConnectionStatus(string status);
    Task ServiceUpdate(string message);
}

/// <summary>
/// Connection manager for tracking active connections
/// </summary>
public interface IConnectionManager
{
    Task AddConnectionAsync(string userId, string tenantId, string connectionId);
    Task RemoveConnectionAsync(string connectionId);
    Task<string[]> GetUserConnectionsAsync(string userId);
    Task<string[]> GetTenantConnectionsAsync(string tenantId);
    Task<bool> IsUserOnlineAsync(string userId);
    Task<Dictionary<string, string[]>> GetOnlineUsersAsync(string tenantId);
    Task CleanupExpiredConnectionsAsync();
}

/// <summary>
/// Connection manager implementation
/// </summary>
public class ConnectionManager : IConnectionManager
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConnectionManager> _logger;
    private readonly Timer _cleanupTimer;

    public ConnectionManager(IMemoryCache cache, ILogger<ConnectionManager> logger)
    {
        _cache = cache;
        _logger = logger;
        
        // Setup cleanup timer
        _cleanupTimer = new Timer(async _ => await CleanupExpiredConnectionsAsync(), 
            null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task AddConnectionAsync(string userId, string tenantId, string connectionId)
    {
        try
        {
            var connection = new ConnectionInfo
            {
                UserId = userId,
                TenantId = tenantId,
                ConnectionId = connectionId,
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            _cache.Set($"connection_{connectionId}", connection, TimeSpan.FromHours(2));
            
            // Update user connections
            var userConnections = await GetUserConnectionsAsync(userId);
            var updatedConnections = userConnections.Union(new[] { connectionId }).ToArray();
            _cache.Set($"user_connections_{userId}", updatedConnections, TimeSpan.FromHours(2));
            
            // Update tenant connections
            var tenantConnections = await GetTenantConnectionsAsync(tenantId);
            var updatedTenantConnections = tenantConnections.Union(new[] { connectionId }).ToArray();
            _cache.Set($"tenant_connections_{tenantId}", updatedTenantConnections, TimeSpan.FromHours(2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        try
        {
            var connection = _cache.Get<ConnectionInfo>($"connection_{connectionId}");
            if (connection != null)
            {
                _cache.Remove($"connection_{connectionId}");
                
                // Update user connections
                var userConnections = await GetUserConnectionsAsync(connection.UserId);
                var updatedConnections = userConnections.Where(c => c != connectionId).ToArray();
                _cache.Set($"user_connections_{connection.UserId}", updatedConnections, TimeSpan.FromHours(2));
                
                // Update tenant connections
                var tenantConnections = await GetTenantConnectionsAsync(connection.TenantId);
                var updatedTenantConnections = tenantConnections.Where(c => c != connectionId).ToArray();
                _cache.Set($"tenant_connections_{connection.TenantId}", updatedTenantConnections, TimeSpan.FromHours(2));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection {ConnectionId}", connectionId);
        }
    }

    public async Task<string[]> GetUserConnectionsAsync(string userId)
    {
        return _cache.Get<string[]>($"user_connections_{userId}") ?? Array.Empty<string>();
    }

    public async Task<string[]> GetTenantConnectionsAsync(string tenantId)
    {
        return _cache.Get<string[]>($"tenant_connections_{tenantId}") ?? Array.Empty<string>();
    }

    public async Task<bool> IsUserOnlineAsync(string userId)
    {
        var connections = await GetUserConnectionsAsync(userId);
        return connections.Any();
    }

    public async Task<Dictionary<string, string[]>> GetOnlineUsersAsync(string tenantId)
    {
        var connections = await GetTenantConnectionsAsync(tenantId);
        var result = new Dictionary<string, string[]>();
        
        foreach (var connectionId in connections)
        {
            var connection = _cache.Get<ConnectionInfo>($"connection_{connectionId}");
            if (connection != null)
            {
                if (!result.ContainsKey(connection.UserId))
                {
                    result[connection.UserId] = Array.Empty<string>();
                }
                result[connection.UserId] = result[connection.UserId].Union(new[] { connectionId }).ToArray();
            }
        }
        
        return result;
    }

    public async Task CleanupExpiredConnectionsAsync()
    {
        try
        {
            // This would typically iterate through all connections and remove expired ones
            // For simplicity, we rely on cache expiration
            _logger.LogDebug("Connection cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection cleanup");
        }
    }
}

/// <summary>
/// Connection information
/// </summary>
public class ConnectionInfo
{
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}