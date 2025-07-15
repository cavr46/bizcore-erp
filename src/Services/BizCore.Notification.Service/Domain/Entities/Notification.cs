using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Notification.Domain.Entities;

public class Notification : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationPriority Priority { get; private set; }
    public NotificationStatus Status { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? RoleId { get; private set; }
    public string? Channel { get; private set; }
    public DateTime? ScheduledFor { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public Dictionary<string, string> Variables { get; private set; } = new();
    
    private readonly List<NotificationDelivery> _deliveries = new();
    public IReadOnlyCollection<NotificationDelivery> Deliveries => _deliveries.AsReadOnly();

    private Notification() { }

    public Notification(
        Guid tenantId,
        string title,
        string message,
        NotificationType type,
        NotificationPriority priority = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Title = title;
        Message = message;
        Type = type;
        Priority = priority ?? NotificationPriority.Normal;
        Status = NotificationStatus.Pending;
        MaxRetries = 3;
        
        AddDomainEvent(new NotificationCreatedDomainEvent(Id, TenantId, Title, Type));
    }

    public void SetRecipient(Guid userId)
    {
        UserId = userId;
        RoleId = null;
    }

    public void SetRoleRecipient(Guid roleId)
    {
        RoleId = roleId;
        UserId = null;
    }

    public void SetChannel(string channel)
    {
        Channel = channel;
    }

    public void ScheduleFor(DateTime scheduledTime)
    {
        ScheduledFor = scheduledTime;
        Status = NotificationStatus.Scheduled;
    }

    public void SetVariables(Dictionary<string, string> variables)
    {
        Variables = variables ?? new Dictionary<string, string>();
    }

    public void SetMetadata(Dictionary<string, object> metadata)
    {
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    public Result Send()
    {
        if (Status == NotificationStatus.Sent)
            return Result.Failure("Notification already sent");

        if (ScheduledFor.HasValue && ScheduledFor.Value > DateTime.UtcNow)
            return Result.Failure("Notification is scheduled for future");

        Status = NotificationStatus.Sending;
        
        AddDomainEvent(new NotificationSendingDomainEvent(Id, TenantId, UserId, RoleId));
        
        return Result.Success();
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        
        AddDomainEvent(new NotificationSentDomainEvent(Id, TenantId, UserId, RoleId, SentAt.Value));
    }

    public void MarkAsRead()
    {
        ReadAt = DateTime.UtcNow;
        
        AddDomainEvent(new NotificationReadDomainEvent(Id, TenantId, UserId, ReadAt.Value));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        
        AddDomainEvent(new NotificationFailedDomainEvent(Id, TenantId, errorMessage, RetryCount));
    }

    public void Retry()
    {
        if (RetryCount >= MaxRetries)
            throw new BusinessRuleValidationException("Maximum retry attempts reached");

        Status = NotificationStatus.Pending;
        ErrorMessage = null;
        
        AddDomainEvent(new NotificationRetryDomainEvent(Id, TenantId, RetryCount));
    }

    public void AddDelivery(string deliveryMethod, string recipient, NotificationDeliveryStatus status)
    {
        var delivery = new NotificationDelivery(Id, deliveryMethod, recipient, status);
        _deliveries.Add(delivery);
    }

    public void UpdateDeliveryStatus(Guid deliveryId, NotificationDeliveryStatus status, string? errorMessage = null)
    {
        var delivery = _deliveries.FirstOrDefault(d => d.Id == deliveryId);
        if (delivery != null)
        {
            delivery.UpdateStatus(status, errorMessage);
        }
    }

    public bool CanRetry()
    {
        return Status == NotificationStatus.Failed && RetryCount < MaxRetries;
    }

    public bool IsExpired()
    {
        return ScheduledFor.HasValue && 
               ScheduledFor.Value.AddDays(7) < DateTime.UtcNow; // Expire after 7 days
    }
}

public class NotificationDelivery : Entity<Guid>
{
    public Guid NotificationId { get; private set; }
    public string DeliveryMethod { get; private set; }
    public string Recipient { get; private set; }
    public NotificationDeliveryStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();

    private NotificationDelivery() { }

    public NotificationDelivery(
        Guid notificationId,
        string deliveryMethod,
        string recipient,
        NotificationDeliveryStatus status)
    {
        Id = Guid.NewGuid();
        NotificationId = notificationId;
        DeliveryMethod = deliveryMethod;
        Recipient = recipient;
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(NotificationDeliveryStatus status, string? errorMessage = null)
    {
        Status = status;
        ErrorMessage = errorMessage;
        
        switch (status)
        {
            case NotificationDeliveryStatus.Sent:
                SentAt = DateTime.UtcNow;
                break;
            case NotificationDeliveryStatus.Delivered:
                DeliveredAt = DateTime.UtcNow;
                break;
        }
    }

    public void SetMetadata(Dictionary<string, object> metadata)
    {
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}

public class NotificationTemplate : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string Subject { get; private set; }
    public string Body { get; private set; }
    public NotificationType Type { get; private set; }
    public bool IsActive { get; private set; }
    public string? Language { get; private set; }
    public List<string> SupportedChannels { get; private set; } = new();
    public Dictionary<string, object> DefaultVariables { get; private set; } = new();

    private NotificationTemplate() { }

    public NotificationTemplate(
        Guid tenantId,
        string name,
        string code,
        string subject,
        string body,
        NotificationType type)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name;
        Code = code;
        Subject = subject;
        Body = body;
        Type = type;
        IsActive = true;
        Language = "en";
    }

    public void UpdateContent(string subject, string body)
    {
        Subject = subject;
        Body = body;
    }

    public void SetSupportedChannels(List<string> channels)
    {
        SupportedChannels = channels ?? new List<string>();
    }

    public void SetDefaultVariables(Dictionary<string, object> variables)
    {
        DefaultVariables = variables ?? new Dictionary<string, object>();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public string ProcessTemplate(Dictionary<string, string> variables)
    {
        var processedBody = Body;
        
        foreach (var variable in variables)
        {
            processedBody = processedBody.Replace($"{{{variable.Key}}}", variable.Value);
        }
        
        return processedBody;
    }
}

public class NotificationPreference : AuditableEntity<Guid>, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Channel { get; private set; }
    public NotificationType Type { get; private set; }
    public bool IsEnabled { get; private set; }
    public Dictionary<string, object> Settings { get; private set; } = new();

    private NotificationPreference() { }

    public NotificationPreference(
        Guid tenantId,
        Guid userId,
        string channel,
        NotificationType type,
        bool isEnabled = true)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        UserId = userId;
        Channel = channel;
        Type = type;
        IsEnabled = isEnabled;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;

    public void UpdateSettings(Dictionary<string, object> settings)
    {
        Settings = settings ?? new Dictionary<string, object>();
    }
}

public class NotificationType : SmartEnum<NotificationType>
{
    public static readonly NotificationType Info = new(1, nameof(Info));
    public static readonly NotificationType Warning = new(2, nameof(Warning));
    public static readonly NotificationType Error = new(3, nameof(Error));
    public static readonly NotificationType Success = new(4, nameof(Success));
    public static readonly NotificationType System = new(5, nameof(System));
    public static readonly NotificationType Marketing = new(6, nameof(Marketing));
    public static readonly NotificationType Transactional = new(7, nameof(Transactional));

    private NotificationType(int value, string name) : base(name, value) { }
}

public class NotificationPriority : SmartEnum<NotificationPriority>
{
    public static readonly NotificationPriority Low = new(1, nameof(Low));
    public static readonly NotificationPriority Normal = new(2, nameof(Normal));
    public static readonly NotificationPriority High = new(3, nameof(High));
    public static readonly NotificationPriority Critical = new(4, nameof(Critical));

    private NotificationPriority(int value, string name) : base(name, value) { }
}

public class NotificationStatus : SmartEnum<NotificationStatus>
{
    public static readonly NotificationStatus Pending = new(1, nameof(Pending));
    public static readonly NotificationStatus Scheduled = new(2, nameof(Scheduled));
    public static readonly NotificationStatus Sending = new(3, nameof(Sending));
    public static readonly NotificationStatus Sent = new(4, nameof(Sent));
    public static readonly NotificationStatus Failed = new(5, nameof(Failed));
    public static readonly NotificationStatus Cancelled = new(6, nameof(Cancelled));

    private NotificationStatus(int value, string name) : base(name, value) { }
}

public class NotificationDeliveryStatus : SmartEnum<NotificationDeliveryStatus>
{
    public static readonly NotificationDeliveryStatus Pending = new(1, nameof(Pending));
    public static readonly NotificationDeliveryStatus Sent = new(2, nameof(Sent));
    public static readonly NotificationDeliveryStatus Delivered = new(3, nameof(Delivered));
    public static readonly NotificationDeliveryStatus Failed = new(4, nameof(Failed));
    public static readonly NotificationDeliveryStatus Bounced = new(5, nameof(Bounced));

    private NotificationDeliveryStatus(int value, string name) : base(name, value) { }
}

// Domain Events
public record NotificationCreatedDomainEvent(
    Guid NotificationId,
    Guid TenantId,
    string Title,
    NotificationType Type) : INotification;

public record NotificationSendingDomainEvent(
    Guid NotificationId,
    Guid TenantId,
    Guid? UserId,
    Guid? RoleId) : INotification;

public record NotificationSentDomainEvent(
    Guid NotificationId,
    Guid TenantId,
    Guid? UserId,
    Guid? RoleId,
    DateTime SentAt) : INotification;

public record NotificationReadDomainEvent(
    Guid NotificationId,
    Guid TenantId,
    Guid? UserId,
    DateTime ReadAt) : INotification;

public record NotificationFailedDomainEvent(
    Guid NotificationId,
    Guid TenantId,
    string ErrorMessage,
    int RetryCount) : INotification;

public record NotificationRetryDomainEvent(
    Guid NotificationId,
    Guid TenantId,
    int RetryCount) : INotification;