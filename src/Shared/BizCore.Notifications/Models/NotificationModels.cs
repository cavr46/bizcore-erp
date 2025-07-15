using System.ComponentModel.DataAnnotations;

namespace BizCore.Notifications.Models;

/// <summary>
/// Notification entity representing a single notification
/// </summary>
public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Category { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledFor { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? GroupId { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool IsRead { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    public bool IsPinned { get; set; } = false;
    public bool RequiresAction { get; set; } = false;
    public string? CreatedByUserId { get; set; }
    public string? CreatedBySystem { get; set; }
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();
    public Dictionary<string, object> LocalizedContent { get; set; } = new();
    public NotificationTracking Tracking { get; set; } = new();
}

/// <summary>
/// Notification types for categorization
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    System,
    Business,
    Marketing,
    Security,
    Approval,
    Reminder,
    Alert,
    Update,
    Announcement,
    Task,
    Message,
    Event
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent,
    Critical
}

/// <summary>
/// Notification status tracking
/// </summary>
public enum NotificationStatus
{
    Pending,
    Queued,
    Processing,
    Delivered,
    Failed,
    Cancelled,
    Expired,
    Retrying
}

/// <summary>
/// Notification delivery channels
/// </summary>
public enum NotificationChannel
{
    InApp,
    Email,
    SMS,
    Push,
    WebSocket,
    Webhook,
    Slack,
    Teams,
    WhatsApp,
    Telegram
}

/// <summary>
/// Notification template for reusable content
/// </summary>
public class NotificationTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;
    public string ContentTemplate { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public NotificationChannel[] DefaultChannels { get; set; } = Array.Empty<NotificationChannel>();
    public Dictionary<string, string> Variables { get; set; } = new();
    public Dictionary<string, object> DefaultMetadata { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public Dictionary<string, NotificationTemplateLocalization> Localizations { get; set; } = new();
}

/// <summary>
/// Localized content for notification templates
/// </summary>
public class NotificationTemplateLocalization
{
    public string Language { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;
    public string ContentTemplate { get; set; } = string.Empty;
    public Dictionary<string, string> Variables { get; set; } = new();
}

/// <summary>
/// User notification preferences
/// </summary>
public class NotificationPreferences
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public bool GloballyEnabled { get; set; } = true;
    public Dictionary<NotificationChannel, bool> ChannelPreferences { get; set; } = new();
    public Dictionary<string, CategoryPreference> CategoryPreferences { get; set; } = new();
    public Dictionary<string, ModulePreference> ModulePreferences { get; set; } = new();
    public TimeSpan QuietHoursStart { get; set; } = TimeSpan.FromHours(22);
    public TimeSpan QuietHoursEnd { get; set; } = TimeSpan.FromHours(8);
    public string[] QuietDays { get; set; } = Array.Empty<string>();
    public string TimeZone { get; set; } = "UTC";
    public NotificationDigestFrequency DigestFrequency { get; set; } = NotificationDigestFrequency.Daily;
    public bool EnableDigest { get; set; } = true;
    public bool EnableSounds { get; set; } = true;
    public bool EnableVibration { get; set; } = true;
    public int MaxNotificationsPerHour { get; set; } = 50;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Category-specific preferences
/// </summary>
public class CategoryPreference
{
    public string Category { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();
    public NotificationPriority MinPriority { get; set; } = NotificationPriority.Normal;
    public bool EnableImmediate { get; set; } = true;
    public bool EnableDigest { get; set; } = true;
}

/// <summary>
/// Module-specific preferences
/// </summary>
public class ModulePreference
{
    public string Module { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();
    public Dictionary<string, bool> EventTypes { get; set; } = new();
}

/// <summary>
/// Notification digest frequency
/// </summary>
public enum NotificationDigestFrequency
{
    Disabled,
    Realtime,
    Hourly,
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Notification tracking for analytics
/// </summary>
public class NotificationTracking
{
    public int ViewCount { get; set; } = 0;
    public int ClickCount { get; set; } = 0;
    public DateTime? FirstViewed { get; set; }
    public DateTime? LastViewed { get; set; }
    public DateTime? FirstClicked { get; set; }
    public DateTime? LastClicked { get; set; }
    public string[] DeviceTypes { get; set; } = Array.Empty<string>();
    public Dictionary<string, int> ChannelMetrics { get; set; } = new();
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Notification subscription for real-time updates
/// </summary>
public class NotificationSubscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string[] Categories { get; set; } = Array.Empty<string>();
    public string[] Modules { get; set; } = Array.Empty<string>();
    public NotificationPriority MinPriority { get; set; } = NotificationPriority.Normal;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification group for batch operations
/// </summary>
public class NotificationGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public int NotificationCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsCollapsed { get; set; } = false;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification event for real-time streaming
/// </summary>
public class NotificationEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public NotificationEventType Type { get; set; }
    public string NotificationId { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Notification event types
/// </summary>
public enum NotificationEventType
{
    Created,
    Updated,
    Delivered,
    Read,
    Clicked,
    Dismissed,
    Archived,
    Pinned,
    Unpinned,
    Expired,
    Failed,
    Retried
}

/// <summary>
/// Notification delivery result
/// </summary>
public class NotificationDeliveryResult
{
    public bool IsSuccess { get; set; }
    public string NotificationId { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public int AttemptCount { get; set; } = 1;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification statistics for analytics
/// </summary>
public class NotificationStatistics
{
    public string TenantId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int TotalNotifications { get; set; }
    public int PendingNotifications { get; set; }
    public int DeliveredNotifications { get; set; }
    public int ReadNotifications { get; set; }
    public int ClickedNotifications { get; set; }
    public int FailedNotifications { get; set; }
    public double DeliveryRate { get; set; }
    public double ReadRate { get; set; }
    public double ClickRate { get; set; }
    public Dictionary<NotificationChannel, int> ChannelStats { get; set; } = new();
    public Dictionary<NotificationType, int> TypeStats { get; set; } = new();
    public Dictionary<string, int> CategoryStats { get; set; } = new();
    public Dictionary<string, int> ModuleStats { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan TimeRange { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Notification request for creating notifications
/// </summary>
public class CreateNotificationRequest
{
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string Category { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? IconUrl { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? GroupId { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool RequiresAction { get; set; } = false;
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();
    public Dictionary<string, object> LocalizedContent { get; set; } = new();
}

/// <summary>
/// Batch notification request
/// </summary>
public class CreateBatchNotificationRequest
{
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    [Required]
    public string[] UserIds { get; set; } = Array.Empty<string>();
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string Category { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? IconUrl { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? GroupId { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool RequiresAction { get; set; } = false;
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();
    public Dictionary<string, object> LocalizedContent { get; set; } = new();
    public Dictionary<string, Dictionary<string, object>> UserSpecificData { get; set; } = new();
}

/// <summary>
/// Template notification request
/// </summary>
public class CreateTemplateNotificationRequest
{
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string TemplateId { get; set; } = string.Empty;
    
    public Dictionary<string, object> Variables { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? GroupId { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public NotificationChannel[]? OverrideChannels { get; set; }
    public string? Language { get; set; }
}

/// <summary>
/// Notification query parameters
/// </summary>
public class NotificationQuery
{
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public NotificationType? Type { get; set; }
    public NotificationPriority? Priority { get; set; }
    public NotificationStatus? Status { get; set; }
    public string? Category { get; set; }
    public string? Module { get; set; }
    public bool? IsRead { get; set; }
    public bool? IsArchived { get; set; }
    public bool? IsPinned { get; set; }
    public bool? RequiresAction { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? GroupId { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string? SearchTerm { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Notification result wrapper
/// </summary>
public class NotificationResult
{
    public bool IsSuccess { get; set; }
    public Notification? Notification { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static NotificationResult Success(Notification notification) =>
        new() { IsSuccess = true, Notification = notification };

    public static NotificationResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Batch notification result
/// </summary>
public class BatchNotificationResult
{
    public bool IsSuccess { get; set; }
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string[] NotificationIds { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Failures { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static BatchNotificationResult Success(int total, int success, string[] notificationIds) =>
        new() { IsSuccess = true, TotalRequested = total, SuccessCount = success, NotificationIds = notificationIds };

    public static BatchNotificationResult Failure(int total, Dictionary<string, string> failures) =>
        new() { IsSuccess = false, TotalRequested = total, FailureCount = failures.Count, Failures = failures };
}