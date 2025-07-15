using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using BizCore.Notifications.Services;
using BizCore.Notifications.Hubs;
using BizCore.Notifications.Models;
using BizCore.Notifications.Grains;

namespace BizCore.Notifications.Extensions;

/// <summary>
/// Service collection extensions for BizCore Notifications
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add BizCore Notifications services
    /// </summary>
    public static IServiceCollection AddBizCoreNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure notification options
        services.Configure<NotificationConfiguration>(configuration.GetSection("Notifications"));
        
        // Add core notification services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationDeliveryService, NotificationDeliveryService>();
        services.AddScoped<INotificationTemplateEngine, NotificationTemplateEngine>();
        services.AddScoped<INotificationScheduler, NotificationScheduler>();
        services.AddScoped<INotificationAnalytics, NotificationAnalytics>();
        services.AddScoped<INotificationDigestService, NotificationDigestService>();
        services.AddScoped<INotificationEventPublisher, NotificationEventPublisher>();
        
        // Add repositories
        services.AddScoped<INotificationRepository, SqlNotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, SqlNotificationTemplateRepository>();
        services.AddScoped<INotificationPreferencesRepository, SqlNotificationPreferencesRepository>();
        services.AddScoped<INotificationSubscriptionRepository, SqlNotificationSubscriptionRepository>();
        services.AddScoped<INotificationGroupRepository, SqlNotificationGroupRepository>();
        
        // Add delivery channels
        services.AddScoped<IEmailNotificationChannel, EmailNotificationChannel>();
        services.AddScoped<ISmsNotificationChannel, SmsNotificationChannel>();
        services.AddScoped<IPushNotificationChannel, PushNotificationChannel>();
        services.AddScoped<IWebhookNotificationChannel, WebhookNotificationChannel>();
        services.AddScoped<ISlackNotificationChannel, SlackNotificationChannel>();
        services.AddScoped<ITeamsNotificationChannel, TeamsNotificationChannel>();
        
        // Add background services
        services.AddHostedService<NotificationProcessingService>();
        services.AddHostedService<NotificationDigestService>();
        services.AddHostedService<NotificationCleanupService>();
        
        // Add SignalR
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
            options.StreamBufferCapacity = 10;
        });
        
        // Add connection manager
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        
        // Add memory cache for notifications
        services.AddMemoryCache();
        
        // Add HTTP client for webhooks
        services.AddHttpClient();
        
        return services;
    }

    /// <summary>
    /// Add notification delivery channels
    /// </summary>
    public static IServiceCollection AddNotificationChannels(this IServiceCollection services, IConfiguration configuration)
    {
        // Email channel
        services.Configure<EmailChannelConfiguration>(configuration.GetSection("Notifications:Email"));
        services.AddScoped<IEmailNotificationChannel, EmailNotificationChannel>();
        
        // SMS channel
        services.Configure<SmsChannelConfiguration>(configuration.GetSection("Notifications:SMS"));
        services.AddScoped<ISmsNotificationChannel, SmsNotificationChannel>();
        
        // Push notification channel
        services.Configure<PushChannelConfiguration>(configuration.GetSection("Notifications:Push"));
        services.AddScoped<IPushNotificationChannel, PushNotificationChannel>();
        
        // Webhook channel
        services.Configure<WebhookChannelConfiguration>(configuration.GetSection("Notifications:Webhook"));
        services.AddScoped<IWebhookNotificationChannel, WebhookNotificationChannel>();
        
        // Slack channel
        services.Configure<SlackChannelConfiguration>(configuration.GetSection("Notifications:Slack"));
        services.AddScoped<ISlackNotificationChannel, SlackNotificationChannel>();
        
        // Teams channel
        services.Configure<TeamsChannelConfiguration>(configuration.GetSection("Notifications:Teams"));
        services.AddScoped<ITeamsNotificationChannel, TeamsNotificationChannel>();
        
        return services;
    }

    /// <summary>
    /// Add notification background services
    /// </summary>
    public static IServiceCollection AddNotificationBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<NotificationProcessingService>();
        services.AddHostedService<NotificationDigestGenerationService>();
        services.AddHostedService<NotificationCleanupService>();
        services.AddHostedService<NotificationAnalyticsService>();
        
        return services;
    }

    /// <summary>
    /// Add notification health checks
    /// </summary>
    public static IServiceCollection AddNotificationHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck<NotificationServiceHealthCheck>("notifications")
            .AddCheck<NotificationDatabaseHealthCheck>("notifications_database")
            .AddCheck<NotificationChannelHealthCheck>("notification_channels")
            .AddCheck<NotificationSignalRHealthCheck>("notification_signalr");

        return services;
    }

    /// <summary>
    /// Add notification Orleans grains
    /// </summary>
    public static IServiceCollection AddNotificationGrains(this IServiceCollection services)
    {
        // Register grain interfaces and implementations
        services.AddSingleton<INotificationGrain, NotificationGrain>();
        services.AddSingleton<INotificationUserGrain, NotificationUserGrain>();
        services.AddSingleton<INotificationTenantGrain, NotificationTenantGrain>();
        
        return services;
    }
}

/// <summary>
/// Application builder extensions for BizCore Notifications
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Use BizCore Notifications
    /// </summary>
    public static IApplicationBuilder UseBizCoreNotifications(this IApplicationBuilder app)
    {
        // Map SignalR hub
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<NotificationHub>("/hubs/notifications");
        });
        
        return app;
    }

    /// <summary>
    /// Initialize notification database
    /// </summary>
    public static async Task InitializeBizCoreNotificationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        
        // Initialize database
        await InitializeDatabaseAsync(scope.ServiceProvider);
        
        // Seed default templates
        await SeedDefaultTemplatesAsync(scope.ServiceProvider);
        
        // Initialize channels
        await InitializeChannelsAsync(scope.ServiceProvider);
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        // This would run database migrations for notifications
        // Implementation depends on your database provider
    }

    private static async Task SeedDefaultTemplatesAsync(IServiceProvider services)
    {
        var templateRepository = services.GetRequiredService<INotificationTemplateRepository>();
        
        var defaultTemplates = new[]
        {
            new NotificationTemplate
            {
                Name = "WelcomeUser",
                TitleTemplate = "Welcome to {CompanyName}!",
                ContentTemplate = "Hi {FirstName}, welcome to {CompanyName}. We're excited to have you on board!",
                Type = NotificationType.Info,
                Priority = NotificationPriority.Normal,
                Category = "Onboarding",
                Module = "Users",
                DefaultChannels = new[] { NotificationChannel.Email, NotificationChannel.InApp },
                IsSystem = true
            },
            new NotificationTemplate
            {
                Name = "PasswordReset",
                TitleTemplate = "Password Reset Request",
                ContentTemplate = "You have requested a password reset. Click the link below to reset your password.",
                Type = NotificationType.Security,
                Priority = NotificationPriority.High,
                Category = "Security",
                Module = "Authentication",
                DefaultChannels = new[] { NotificationChannel.Email },
                IsSystem = true
            },
            new NotificationTemplate
            {
                Name = "InvoiceCreated",
                TitleTemplate = "New Invoice #{InvoiceNumber}",
                ContentTemplate = "A new invoice has been created for {CustomerName} in the amount of {Amount}.",
                Type = NotificationType.Business,
                Priority = NotificationPriority.Normal,
                Category = "Accounting",
                Module = "Invoicing",
                DefaultChannels = new[] { NotificationChannel.InApp, NotificationChannel.Email },
                IsSystem = true
            },
            new NotificationTemplate
            {
                Name = "PaymentReceived",
                TitleTemplate = "Payment Received",
                ContentTemplate = "Payment of {Amount} has been received from {CustomerName} for invoice #{InvoiceNumber}.",
                Type = NotificationType.Success,
                Priority = NotificationPriority.Normal,
                Category = "Accounting",
                Module = "Payments",
                DefaultChannels = new[] { NotificationChannel.InApp, NotificationChannel.Email },
                IsSystem = true
            },
            new NotificationTemplate
            {
                Name = "LowInventory",
                TitleTemplate = "Low Inventory Alert",
                ContentTemplate = "Product {ProductName} is running low. Current stock: {CurrentStock}, Minimum stock: {MinimumStock}.",
                Type = NotificationType.Warning,
                Priority = NotificationPriority.High,
                Category = "Inventory",
                Module = "Inventory",
                DefaultChannels = new[] { NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.SMS },
                IsSystem = true
            },
            new NotificationTemplate
            {
                Name = "TaskAssigned",
                TitleTemplate = "Task Assigned: {TaskTitle}",
                ContentTemplate = "You have been assigned a new task: {TaskTitle}. Due date: {DueDate}.",
                Type = NotificationType.Task,
                Priority = NotificationPriority.Normal,
                Category = "Tasks",
                Module = "ProjectManagement",
                DefaultChannels = new[] { NotificationChannel.InApp, NotificationChannel.Email },
                IsSystem = true
            },
            new NotificationTemplate
            {
                Name = "TaskDueSoon",
                TitleTemplate = "Task Due Soon: {TaskTitle}",
                ContentTemplate = "Task {TaskTitle} is due in {TimeRemaining}. Please complete it soon.",
                Type = NotificationType.Reminder,
                Priority = NotificationPriority.High,
                Category = "Tasks",
                Module = "ProjectManagement",
                DefaultChannels = new[] { NotificationChannel.InApp, NotificationChannel.Email },
                IsSystem = true
            },
            new NotificationTemplate
            {
                Name = "SystemMaintenance",
                TitleTemplate = "Scheduled System Maintenance",
                ContentTemplate = "System maintenance is scheduled for {MaintenanceDate} from {StartTime} to {EndTime}. Please save your work.",
                Type = NotificationType.System,
                Priority = NotificationPriority.Urgent,
                Category = "System",
                Module = "Maintenance",
                DefaultChannels = new[] { NotificationChannel.InApp, NotificationChannel.Email },
                IsSystem = true
            },
            new NotificationTemplate
            {
                Name = "SecurityAlert",
                TitleTemplate = "Security Alert",
                ContentTemplate = "Suspicious activity detected on your account. If this wasn't you, please contact support immediately.",
                Type = NotificationType.Security,
                Priority = NotificationPriority.Critical,
                Category = "Security",
                Module = "Security",
                DefaultChannels = new[] { NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.SMS },
                IsSystem = true
            },
            new NotificationTemplate
            {
                Name = "ApprovalRequired",
                TitleTemplate = "Approval Required: {DocumentType}",
                ContentTemplate = "{DocumentType} #{DocumentNumber} requires your approval. Amount: {Amount}.",
                Type = NotificationType.Approval,
                Priority = NotificationPriority.High,
                Category = "Approvals",
                Module = "Workflow",
                DefaultChannels = new[] { NotificationChannel.InApp, NotificationChannel.Email },
                IsSystem = true
            }
        };

        foreach (var template in defaultTemplates)
        {
            var existing = await templateRepository.GetByNameAsync(template.Name);
            if (existing == null)
            {
                await templateRepository.CreateAsync(template);
            }
        }
    }

    private static async Task InitializeChannelsAsync(IServiceProvider services)
    {
        // Initialize email channel
        var emailChannel = services.GetService<IEmailNotificationChannel>();
        if (emailChannel != null)
        {
            await emailChannel.InitializeAsync();
        }

        // Initialize SMS channel
        var smsChannel = services.GetService<ISmsNotificationChannel>();
        if (smsChannel != null)
        {
            await smsChannel.InitializeAsync();
        }

        // Initialize push channel
        var pushChannel = services.GetService<IPushNotificationChannel>();
        if (pushChannel != null)
        {
            await pushChannel.InitializeAsync();
        }

        // Initialize webhook channel
        var webhookChannel = services.GetService<IWebhookNotificationChannel>();
        if (webhookChannel != null)
        {
            await webhookChannel.InitializeAsync();
        }
    }
}

/// <summary>
/// Notification configuration
/// </summary>
public class NotificationConfiguration
{
    public bool EnableRealTimeNotifications { get; set; } = true;
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnableSmsNotifications { get; set; } = true;
    public bool EnablePushNotifications { get; set; } = true;
    public bool EnableWebhookNotifications { get; set; } = true;
    public int MaxNotificationsPerUser { get; set; } = 1000;
    public int MaxNotificationsPerTenant { get; set; } = 100000;
    public int NotificationRetentionDays { get; set; } = 90;
    public int MaxDeliveryAttempts { get; set; } = 3;
    public int DeliveryRetryDelayMinutes { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
    public int ProcessingIntervalSeconds { get; set; } = 30;
    public int DigestGenerationIntervalMinutes { get; set; } = 60;
    public int CleanupIntervalHours { get; set; } = 24;
    public int AnalyticsIntervalMinutes { get; set; } = 15;
    public string DefaultTimeZone { get; set; } = "UTC";
    public string DefaultLanguage { get; set; } = "en-US";
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Email channel configuration
/// </summary>
public class EmailChannelConfiguration
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ReplyToAddress { get; set; } = string.Empty;
    public bool EnableTracking { get; set; } = true;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}

/// <summary>
/// SMS channel configuration
/// </summary>
public class SmsChannelConfiguration
{
    public string Provider { get; set; } = "Twilio"; // Twilio, AWS SNS, etc.
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
    public int MaxMessageLength { get; set; } = 160;
    public bool EnableDeliveryReports { get; set; } = true;
    public Dictionary<string, string> ProviderSettings { get; set; } = new();
}

/// <summary>
/// Push notification channel configuration
/// </summary>
public class PushChannelConfiguration
{
    public string Provider { get; set; } = "Firebase"; // Firebase, Azure, etc.
    public string ServerKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public bool EnableBadgeCount { get; set; } = true;
    public bool EnableSound { get; set; } = true;
    public Dictionary<string, string> ProviderSettings { get; set; } = new();
}

/// <summary>
/// Webhook channel configuration
/// </summary>
public class WebhookChannelConfiguration
{
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public bool VerifySSL { get; set; } = true;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public string SigningSecret { get; set; } = string.Empty;
    public string SigningAlgorithm { get; set; } = "SHA256";
}

/// <summary>
/// Slack channel configuration
/// </summary>
public class SlackChannelConfiguration
{
    public string BotToken { get; set; } = string.Empty;
    public string AppToken { get; set; } = string.Empty;
    public string SigningSecret { get; set; } = string.Empty;
    public string DefaultChannel { get; set; } = "#notifications";
    public bool EnableThreads { get; set; } = true;
    public Dictionary<string, string> ChannelMappings { get; set; } = new();
}

/// <summary>
/// Teams channel configuration
/// </summary>
public class TeamsChannelConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string DefaultTeamId { get; set; } = string.Empty;
    public string DefaultChannelId { get; set; } = string.Empty;
    public Dictionary<string, string> TeamMappings { get; set; } = new();
}

// Background service implementations
public class NotificationProcessingService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationProcessingService> _logger;
    private readonly NotificationConfiguration _config;

    public NotificationProcessingService(
        IServiceProvider services,
        ILogger<NotificationProcessingService> logger,
        IOptions<NotificationConfiguration> config)
    {
        _services = services;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                // Process pending notifications
                await ProcessPendingNotificationsAsync(notificationService);
                
                // Process scheduled notifications
                await ProcessScheduledNotificationsAsync(notificationService);
                
                // Retry failed notifications
                await RetryFailedNotificationsAsync(notificationService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification processing service");
            }

            await Task.Delay(TimeSpan.FromSeconds(_config.ProcessingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessPendingNotificationsAsync(INotificationService notificationService)
    {
        // Implementation would process pending notifications
    }

    private async Task ProcessScheduledNotificationsAsync(INotificationService notificationService)
    {
        // Implementation would process scheduled notifications
    }

    private async Task RetryFailedNotificationsAsync(INotificationService notificationService)
    {
        // Implementation would retry failed notifications
    }
}

public class NotificationDigestGenerationService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationDigestGenerationService> _logger;
    private readonly NotificationConfiguration _config;

    public NotificationDigestGenerationService(
        IServiceProvider services,
        ILogger<NotificationDigestGenerationService> logger,
        IOptions<NotificationConfiguration> config)
    {
        _services = services;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                // Generate daily digests
                await GenerateDailyDigestsAsync(notificationService);
                
                // Generate weekly digests
                await GenerateWeeklyDigestsAsync(notificationService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification digest generation service");
            }

            await Task.Delay(TimeSpan.FromMinutes(_config.DigestGenerationIntervalMinutes), stoppingToken);
        }
    }

    private async Task GenerateDailyDigestsAsync(INotificationService notificationService)
    {
        // Implementation would generate daily digests
    }

    private async Task GenerateWeeklyDigestsAsync(INotificationService notificationService)
    {
        // Implementation would generate weekly digests
    }
}

public class NotificationCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationCleanupService> _logger;
    private readonly NotificationConfiguration _config;

    public NotificationCleanupService(
        IServiceProvider services,
        ILogger<NotificationCleanupService> logger,
        IOptions<NotificationConfiguration> config)
    {
        _services = services;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                // Cleanup expired notifications
                await CleanupExpiredNotificationsAsync(notificationService);
                
                // Archive old notifications
                await ArchiveOldNotificationsAsync(notificationService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification cleanup service");
            }

            await Task.Delay(TimeSpan.FromHours(_config.CleanupIntervalHours), stoppingToken);
        }
    }

    private async Task CleanupExpiredNotificationsAsync(INotificationService notificationService)
    {
        // Implementation would cleanup expired notifications
    }

    private async Task ArchiveOldNotificationsAsync(INotificationService notificationService)
    {
        // Implementation would archive old notifications
    }
}

public class NotificationAnalyticsService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationAnalyticsService> _logger;
    private readonly NotificationConfiguration _config;

    public NotificationAnalyticsService(
        IServiceProvider services,
        ILogger<NotificationAnalyticsService> logger,
        IOptions<NotificationConfiguration> config)
    {
        _services = services;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                // Calculate analytics
                await CalculateAnalyticsAsync(notificationService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification analytics service");
            }

            await Task.Delay(TimeSpan.FromMinutes(_config.AnalyticsIntervalMinutes), stoppingToken);
        }
    }

    private async Task CalculateAnalyticsAsync(INotificationService notificationService)
    {
        // Implementation would calculate analytics
    }
}

// Health check implementations
public class NotificationServiceHealthCheck : IHealthCheck
{
    private readonly INotificationService _notificationService;

    public NotificationServiceHealthCheck(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _notificationService.IsHealthyAsync();
            return isHealthy ? HealthCheckResult.Healthy("Notification service is healthy") : HealthCheckResult.Unhealthy("Notification service is unhealthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Notification service is unhealthy", ex);
        }
    }
}

public class NotificationDatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _services;

    public NotificationDatabaseHealthCheck(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            
            // Test database connectivity
            var query = new NotificationQuery { Take = 1 };
            await repository.QueryAsync(query);
            
            return HealthCheckResult.Healthy("Notification database is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Notification database is unhealthy", ex);
        }
    }
}

public class NotificationChannelHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _services;

    public NotificationChannelHealthCheck(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _services.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            // Test email channel
            var emailHealthy = await notificationService.TestChannelAsync(NotificationChannel.Email, "Health check");
            
            // Test SMS channel
            var smsHealthy = await notificationService.TestChannelAsync(NotificationChannel.SMS, "Health check");
            
            var healthyCount = (emailHealthy ? 1 : 0) + (smsHealthy ? 1 : 0);
            
            return healthyCount > 0 ? 
                HealthCheckResult.Healthy($"Notification channels are healthy ({healthyCount}/2)") : 
                HealthCheckResult.Unhealthy("All notification channels are unhealthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Notification channels are unhealthy", ex);
        }
    }
}

public class NotificationSignalRHealthCheck : IHealthCheck
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    public NotificationSignalRHealthCheck(IHubContext<NotificationHub, INotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test SignalR hub
            await _hubContext.Clients.All.ConnectionStatus("healthy");
            return HealthCheckResult.Healthy("SignalR hub is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SignalR hub is unhealthy", ex);
        }
    }
}