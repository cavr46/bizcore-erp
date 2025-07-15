using BizCore.Plugin.Contracts;
using Microsoft.Extensions.Logging;

namespace BizCore.Plugin.WhatsApp;

/// <summary>
/// WhatsApp Business API integration plugin
/// Enables WhatsApp notifications and customer communication
/// </summary>
[BizCorePlugin(
    id: "whatsapp-business",
    name: "WhatsApp Business Integration",
    version: "1.0.0"
)]
public class WhatsAppPlugin : BizCorePluginBase
{
    public override string Id => "whatsapp-business";
    public override string Name => "WhatsApp Business Integration";
    public override string Description => "Send notifications and communicate with customers via WhatsApp Business API";
    public override string Version => "1.0.0";
    public override string Author => "BizCore Community";
    public override PluginCategory Category => PluginCategory.Integration;

    private IWhatsAppService _whatsAppService;
    private ILogger<WhatsAppPlugin> _logger;

    protected override async Task OnInitializeAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register WhatsApp service
        services.AddScoped<IWhatsAppService, WhatsAppService>();
        services.AddScoped<IWhatsAppMessageTemplateService, WhatsAppMessageTemplateService>();
        
        // Configure WhatsApp client
        var whatsAppConfig = configuration.GetSection("WhatsApp");
        services.Configure<WhatsAppConfiguration>(whatsAppConfig);
        
        services.AddHttpClient<WhatsAppClient>(client =>
        {
            client.BaseAddress = new Uri("https://graph.facebook.com/v18.0/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {whatsAppConfig["AccessToken"]}");
        });

        await base.OnInitializeAsync(services, configuration);
    }

    protected override async Task OnConfigureAsync(IApplicationBuilder app)
    {
        _logger = app.ApplicationServices.GetRequiredService<ILogger<WhatsAppPlugin>>();
        _whatsAppService = app.ApplicationServices.GetRequiredService<IWhatsAppService>();

        // Register webhook endpoints
        app.Map("/api/plugins/whatsapp/webhook", webhookApp =>
        {
            webhookApp.Run(async context =>
            {
                if (context.Request.Method == "POST")
                {
                    await HandleWebhookAsync(context);
                }
                else if (context.Request.Method == "GET")
                {
                    await HandleWebhookVerificationAsync(context);
                }
            });
        });

        _logger.LogInformation("WhatsApp Business plugin configured successfully");
        await base.OnConfigureAsync(app);
    }

    protected override async Task<object> OnExecuteAsync(PluginContext context)
    {
        switch (context.Action.ToLower())
        {
            case "send_message":
                return await SendMessageAsync(context);
            
            case "send_template":
                return await SendTemplateMessageAsync(context);
            
            case "get_business_profile":
                return await GetBusinessProfileAsync(context);
            
            case "send_order_notification":
                return await SendOrderNotificationAsync(context);
            
            case "send_invoice_reminder":
                return await SendInvoiceReminderAsync(context);
            
            default:
                throw new InvalidOperationException($"Unknown action: {context.Action}");
        }
    }

    private async Task<object> SendMessageAsync(PluginContext context)
    {
        var phoneNumber = context.Parameters["phone_number"]?.ToString();
        var message = context.Parameters["message"]?.ToString();
        var messageType = context.Parameters["message_type"]?.ToString() ?? "text";

        if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("Phone number and message are required");
        }

        var result = await _whatsAppService.SendMessageAsync(phoneNumber, message, messageType);
        return new { success = result.Success, messageId = result.MessageId, error = result.Error };
    }

    private async Task<object> SendTemplateMessageAsync(PluginContext context)
    {
        var phoneNumber = context.Parameters["phone_number"]?.ToString();
        var templateName = context.Parameters["template_name"]?.ToString();
        var parameters = context.Parameters["parameters"] as Dictionary<string, object>;

        var result = await _whatsAppService.SendTemplateMessageAsync(phoneNumber, templateName, parameters);
        return new { success = result.Success, messageId = result.MessageId, error = result.Error };
    }

    private async Task<object> GetBusinessProfileAsync(PluginContext context)
    {
        var profile = await _whatsAppService.GetBusinessProfileAsync();
        return profile;
    }

    private async Task<object> SendOrderNotificationAsync(PluginContext context)
    {
        var customerId = context.Parameters["customer_id"]?.ToString();
        var orderId = context.Parameters["order_id"]?.ToString();
        var orderStatus = context.Parameters["order_status"]?.ToString();

        // Get customer phone from CRM
        var customerGrain = context.ServiceProvider.GetRequiredService<IGrainFactory>()
            .GetGrain<ICustomerGrain>(Guid.Parse(customerId));
        var customer = await customerGrain.GetCustomerAsync();

        if (customer == null || string.IsNullOrEmpty(customer.PhoneNumber))
        {
            return new { success = false, error = "Customer phone number not found" };
        }

        var message = orderStatus.ToLower() switch
        {
            "confirmed" => $"¡Hola {customer.Name}! Tu pedido #{orderId} ha sido confirmado. Te notificaremos cuando esté listo. 📦",
            "shipped" => $"¡Buenas noticias {customer.Name}! Tu pedido #{orderId} ha sido enviado. Recibirás el código de seguimiento pronto. 🚚",
            "delivered" => $"¡Perfecto {customer.Name}! Tu pedido #{orderId} ha sido entregado. ¡Gracias por tu compra! 🎉",
            _ => $"Hola {customer.Name}, tu pedido #{orderId} ha sido actualizado a: {orderStatus}"
        };

        var result = await _whatsAppService.SendMessageAsync(customer.PhoneNumber, message);
        return new { success = result.Success, messageId = result.MessageId };
    }

    private async Task<object> SendInvoiceReminderAsync(PluginContext context)
    {
        var customerId = context.Parameters["customer_id"]?.ToString();
        var invoiceId = context.Parameters["invoice_id"]?.ToString();
        var amount = context.Parameters["amount"]?.ToString();
        var dueDate = context.Parameters["due_date"]?.ToString();

        // Get customer info
        var customerGrain = context.ServiceProvider.GetRequiredService<IGrainFactory>()
            .GetGrain<ICustomerGrain>(Guid.Parse(customerId));
        var customer = await customerGrain.GetCustomerAsync();

        if (customer == null || string.IsNullOrEmpty(customer.PhoneNumber))
        {
            return new { success = false, error = "Customer phone number not found" };
        }

        var message = $"Hola {customer.Name}, recordatorio amistoso: Tu factura #{invoiceId} por ${amount} vence el {dueDate}. " +
                     $"Puedes pagarla desde nuestro portal web. ¡Gracias! 💳";

        var result = await _whatsAppService.SendMessageAsync(customer.PhoneNumber, message);
        return new { success = result.Success, messageId = result.MessageId };
    }

    private async Task HandleWebhookAsync(HttpContext context)
    {
        try
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var webhook = JsonSerializer.Deserialize<WhatsAppWebhook>(body);

            foreach (var entry in webhook.Entry)
            {
                foreach (var change in entry.Changes)
                {
                    if (change.Field == "messages")
                    {
                        await ProcessIncomingMessageAsync(change.Value);
                    }
                }
            }

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WhatsApp webhook");
            context.Response.StatusCode = 500;
        }
    }

    private async Task HandleWebhookVerificationAsync(HttpContext context)
    {
        var mode = context.Request.Query["hub.mode"];
        var token = context.Request.Query["hub.verify_token"];
        var challenge = context.Request.Query["hub.challenge"];

        if (mode == "subscribe" && token == Configuration["WhatsApp:WebhookVerifyToken"])
        {
            await context.Response.WriteAsync(challenge);
        }
        else
        {
            context.Response.StatusCode = 403;
        }
    }

    private async Task ProcessIncomingMessageAsync(WhatsAppWebhookValue value)
    {
        foreach (var message in value.Messages ?? Enumerable.Empty<WhatsAppMessage>())
        {
            _logger.LogInformation("Received WhatsApp message from {Phone}: {Text}", 
                message.From, message.Text?.Body);

            // Process the message - could trigger workflows, create support tickets, etc.
            // For now, just log it
            
            // Auto-reply for common queries
            if (message.Text?.Body?.ToLower().Contains("horario") == true)
            {
                await _whatsAppService.SendMessageAsync(message.From, 
                    "Nuestro horario de atención es de Lunes a Viernes de 9:00 AM a 6:00 PM. " +
                    "Los sábados de 9:00 AM a 1:00 PM. 🕒");
            }
            else if (message.Text?.Body?.ToLower().Contains("estado") == true)
            {
                await _whatsAppService.SendMessageAsync(message.From,
                    "Para consultar el estado de tu pedido, por favor proporciónanos tu número de orden. " +
                    "También puedes verificarlo en nuestro portal web. 📦");
            }
        }
    }

    protected override async Task OnValidateAsync(List<string> errors, List<string> warnings)
    {
        if (string.IsNullOrEmpty(Configuration["WhatsApp:AccessToken"]))
        {
            errors.Add("WhatsApp Access Token is required");
        }

        if (string.IsNullOrEmpty(Configuration["WhatsApp:PhoneNumberId"]))
        {
            errors.Add("WhatsApp Phone Number ID is required");
        }

        if (string.IsNullOrEmpty(Configuration["WhatsApp:WebhookVerifyToken"]))
        {
            warnings.Add("Webhook verify token is not configured - incoming messages will not work");
        }

        await base.OnValidateAsync(errors, warnings);
    }
}

/// <summary>
/// WhatsApp service interface
/// </summary>
public interface IWhatsAppService
{
    Task<WhatsAppResult> SendMessageAsync(string phoneNumber, string message, string messageType = "text");
    Task<WhatsAppResult> SendTemplateMessageAsync(string phoneNumber, string templateName, Dictionary<string, object> parameters);
    Task<WhatsAppBusinessProfile> GetBusinessProfileAsync();
}

/// <summary>
/// WhatsApp service implementation
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient httpClient, IOptions<WhatsAppConfiguration> config, ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<WhatsAppResult> SendMessageAsync(string phoneNumber, string message, string messageType = "text")
    {
        try
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = messageType,
                text = new { body = message }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_config.PhoneNumberId}/messages", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<WhatsAppSendResponse>(responseContent);
                return new WhatsAppResult(true, result?.Messages?.FirstOrDefault()?.Id);
            }
            else
            {
                _logger.LogError("Failed to send WhatsApp message: {Response}", responseContent);
                return new WhatsAppResult(false, null, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp message");
            return new WhatsAppResult(false, null, ex.Message);
        }
    }

    public async Task<WhatsAppResult> SendTemplateMessageAsync(string phoneNumber, string templateName, Dictionary<string, object> parameters)
    {
        try
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = "es" },
                    components = new[]
                    {
                        new
                        {
                            type = "body",
                            parameters = parameters?.Select(p => new { type = "text", text = p.Value.ToString() }).ToArray()
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_config.PhoneNumberId}/messages", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<WhatsAppSendResponse>(responseContent);
                return new WhatsAppResult(true, result?.Messages?.FirstOrDefault()?.Id);
            }
            else
            {
                return new WhatsAppResult(false, null, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp template message");
            return new WhatsAppResult(false, null, ex.Message);
        }
    }

    public async Task<WhatsAppBusinessProfile> GetBusinessProfileAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_config.PhoneNumberId}?fields=business_profile");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<WhatsAppBusinessProfile>(responseContent);
            }
            else
            {
                _logger.LogError("Failed to get business profile: {Response}", responseContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WhatsApp business profile");
            return null;
        }
    }
}

// DTOs and Configuration classes
public class WhatsAppConfiguration
{
    public string AccessToken { get; set; }
    public string PhoneNumberId { get; set; }
    public string WebhookVerifyToken { get; set; }
}

public record WhatsAppResult(bool Success, string MessageId, string Error = null);

public class WhatsAppBusinessProfile
{
    public string Id { get; set; }
    public BusinessProfile BusinessProfile { get; set; }
}

public class BusinessProfile
{
    public string About { get; set; }
    public string Address { get; set; }
    public string Description { get; set; }
    public string Email { get; set; }
    public string ProfilePictureUrl { get; set; }
    public string Website { get; set; }
}

public class WhatsAppSendResponse
{
    public WhatsAppMessage[] Messages { get; set; }
}

public class WhatsAppMessage
{
    public string Id { get; set; }
    public string From { get; set; }
    public string Timestamp { get; set; }
    public WhatsAppText Text { get; set; }
}

public class WhatsAppText
{
    public string Body { get; set; }
}

public class WhatsAppWebhook
{
    public string Object { get; set; }
    public WhatsAppWebhookEntry[] Entry { get; set; }
}

public class WhatsAppWebhookEntry
{
    public string Id { get; set; }
    public WhatsAppWebhookChange[] Changes { get; set; }
}

public class WhatsAppWebhookChange
{
    public string Field { get; set; }
    public WhatsAppWebhookValue Value { get; set; }
}

public class WhatsAppWebhookValue
{
    public string MessagingProduct { get; set; }
    public WhatsAppMessage[] Messages { get; set; }
}