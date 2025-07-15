using BizCore.Notification.Domain.Entities;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace BizCore.Notification.Services;

public interface INotificationChannel
{
    string ChannelName { get; }
    Task<NotificationChannelResult> SendAsync(NotificationChannelRequest request);
    Task<bool> IsEnabledAsync(Guid tenantId);
}

public class EmailNotificationChannel : INotificationChannel
{
    public string ChannelName => "email";
    
    private readonly IFluentEmail _fluentEmail;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(IFluentEmail fluentEmail, ILogger<EmailNotificationChannel> logger)
    {
        _fluentEmail = fluentEmail;
        _logger = logger;
    }

    public async Task<NotificationChannelResult> SendAsync(NotificationChannelRequest request)
    {
        try
        {
            var email = _fluentEmail
                .To(request.Recipient)
                .Subject(request.Subject)
                .Body(request.Body, request.IsHtml);

            if (request.Attachments?.Any() == true)
            {
                foreach (var attachment in request.Attachments)
                {
                    email.Attach(new Attachment
                    {
                        Data = new MemoryStream(attachment.Data),
                        Filename = attachment.FileName,
                        ContentType = attachment.ContentType
                    });
                }
            }

            var result = await email.SendAsync();
            
            if (result.Successful)
            {
                _logger.LogInformation("Email sent successfully to {Recipient}", request.Recipient);
                return NotificationChannelResult.Success();
            }
            else
            {
                var errors = string.Join(", ", result.ErrorMessages);
                _logger.LogError("Failed to send email to {Recipient}: {Errors}", request.Recipient, errors);
                return NotificationChannelResult.Failure(errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending email to {Recipient}", request.Recipient);
            return NotificationChannelResult.Failure(ex.Message);
        }
    }

    public async Task<bool> IsEnabledAsync(Guid tenantId)
    {
        // Check tenant email configuration
        return await Task.FromResult(true);
    }
}

public class SmsNotificationChannel : INotificationChannel
{
    public string ChannelName => "sms";
    
    private readonly ILogger<SmsNotificationChannel> _logger;
    private readonly IConfiguration _configuration;

    public SmsNotificationChannel(ILogger<SmsNotificationChannel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<NotificationChannelResult> SendAsync(NotificationChannelRequest request)
    {
        try
        {
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var fromNumber = _configuration["Twilio:FromNumber"];

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                body: request.Body,
                from: new Twilio.Types.PhoneNumber(fromNumber),
                to: new Twilio.Types.PhoneNumber(request.Recipient)
            );

            if (message.ErrorCode == null)
            {
                _logger.LogInformation("SMS sent successfully to {Recipient}, SID: {MessageSid}", 
                    request.Recipient, message.Sid);
                return NotificationChannelResult.Success();
            }
            else
            {
                _logger.LogError("Failed to send SMS to {Recipient}: {ErrorCode} - {ErrorMessage}", 
                    request.Recipient, message.ErrorCode, message.ErrorMessage);
                return NotificationChannelResult.Failure($"{message.ErrorCode}: {message.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending SMS to {Recipient}", request.Recipient);
            return NotificationChannelResult.Failure(ex.Message);
        }
    }

    public async Task<bool> IsEnabledAsync(Guid tenantId)
    {
        return await Task.FromResult(!string.IsNullOrEmpty(_configuration["Twilio:AccountSid"]));
    }
}

public class PushNotificationChannel : INotificationChannel
{
    public string ChannelName => "push";
    
    private readonly ILogger<PushNotificationChannel> _logger;
    private readonly FirebaseMessaging _firebaseMessaging;

    public PushNotificationChannel(ILogger<PushNotificationChannel> logger)
    {
        _logger = logger;
        _firebaseMessaging = FirebaseMessaging.DefaultInstance;
    }

    public async Task<NotificationChannelResult> SendAsync(NotificationChannelRequest request)
    {
        try
        {
            var message = new Message
            {
                Token = request.Recipient, // FCM token
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = request.Subject,
                    Body = request.Body
                },
                Data = request.Data
            };

            var response = await _firebaseMessaging.SendAsync(message);
            
            _logger.LogInformation("Push notification sent successfully to {Recipient}, Response: {Response}", 
                request.Recipient, response);
            
            return NotificationChannelResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending push notification to {Recipient}", request.Recipient);
            return NotificationChannelResult.Failure(ex.Message);
        }
    }

    public async Task<bool> IsEnabledAsync(Guid tenantId)
    {
        return await Task.FromResult(FirebaseApp.DefaultInstance != null);
    }
}

public class InAppNotificationChannel : INotificationChannel
{
    public string ChannelName => "in-app";
    
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<InAppNotificationChannel> _logger;

    public InAppNotificationChannel(
        IHubContext<NotificationHub> hubContext,
        ILogger<InAppNotificationChannel> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<NotificationChannelResult> SendAsync(NotificationChannelRequest request)
    {
        try
        {
            var notification = new
            {
                id = Guid.NewGuid(),
                title = request.Subject,
                message = request.Body,
                type = request.Type,
                timestamp = DateTime.UtcNow,
                data = request.Data
            };

            await _hubContext.Clients.User(request.Recipient)
                .SendAsync("ReceiveNotification", notification);
            
            _logger.LogInformation("In-app notification sent successfully to user {UserId}", request.Recipient);
            
            return NotificationChannelResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending in-app notification to user {UserId}", request.Recipient);
            return NotificationChannelResult.Failure(ex.Message);
        }
    }

    public async Task<bool> IsEnabledAsync(Guid tenantId)
    {
        return await Task.FromResult(true);
    }
}

public class WhatsAppNotificationChannel : INotificationChannel
{
    public string ChannelName => "whatsapp";
    
    private readonly ILogger<WhatsAppNotificationChannel> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public WhatsAppNotificationChannel(
        ILogger<WhatsAppNotificationChannel> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<NotificationChannelResult> SendAsync(NotificationChannelRequest request)
    {
        try
        {
            var accessToken = _configuration["WhatsApp:AccessToken"];
            var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
            
            var message = new
            {
                messaging_product = "whatsapp",
                to = request.Recipient,
                type = "text",
                text = new { body = request.Body }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(message);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsync(
                $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages", 
                content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp message sent successfully to {Recipient}", request.Recipient);
                return NotificationChannelResult.Success();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send WhatsApp message to {Recipient}: {Error}", request.Recipient, error);
                return NotificationChannelResult.Failure(error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending WhatsApp message to {Recipient}", request.Recipient);
            return NotificationChannelResult.Failure(ex.Message);
        }
    }

    public async Task<bool> IsEnabledAsync(Guid tenantId)
    {
        return await Task.FromResult(!string.IsNullOrEmpty(_configuration["WhatsApp:AccessToken"]));
    }
}

// SignalR Hub for real-time notifications
public class NotificationHub : Hub
{
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }

    public async Task LeaveTenantGroup(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }

    public async Task MarkAsRead(string notificationId)
    {
        // Handle marking notification as read
        await Clients.User(Context.UserIdentifier!)
            .SendAsync("NotificationRead", notificationId);
    }
}

// Request/Response models
public class NotificationChannelRequest
{
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public string Type { get; set; } = "info";
    public Dictionary<string, string>? Data { get; set; }
    public List<NotificationAttachment>? Attachments { get; set; }
}

public class NotificationAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

public class NotificationChannelResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static NotificationChannelResult Success(Dictionary<string, object>? metadata = null)
    {
        return new NotificationChannelResult
        {
            IsSuccess = true,
            Metadata = metadata
        };
    }

    public static NotificationChannelResult Failure(string errorMessage)
    {
        return new NotificationChannelResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}