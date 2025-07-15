using System.Diagnostics;
using System.Text;

namespace BizCore.ApiGateway.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        
        // Add request ID to context
        context.Items["RequestId"] = requestId;
        context.Response.Headers.Add("X-Request-Id", requestId);

        // Log request
        await LogRequestAsync(context, requestId);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);
            
            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        var request = context.Request;
        var tenantId = context.Items["TenantId"]?.ToString();
        var userId = context.User?.Identity?.Name;

        _logger.LogInformation(
            "Request {RequestId} started: {Method} {Path} from {RemoteIpAddress} " +
            "(Tenant: {TenantId}, User: {UserId})",
            requestId,
            request.Method,
            request.Path,
            context.Connection.RemoteIpAddress,
            tenantId,
            userId);

        // Log request body for POST/PUT requests (be careful with sensitive data)
        if (request.Method == "POST" || request.Method == "PUT")
        {
            if (request.ContentLength > 0 && request.ContentType?.Contains("application/json") == true)
            {
                request.EnableBuffering();
                var body = await ReadStreamAsync(request.Body);
                request.Body.Position = 0;
                
                _logger.LogDebug("Request {RequestId} body: {Body}", requestId, body);
            }
        }
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMilliseconds)
    {
        var response = context.Response;
        var tenantId = context.Items["TenantId"]?.ToString();
        var userId = context.User?.Identity?.Name;

        _logger.LogInformation(
            "Request {RequestId} completed: {StatusCode} in {ElapsedMilliseconds}ms " +
            "(Tenant: {TenantId}, User: {UserId})",
            requestId,
            response.StatusCode,
            elapsedMilliseconds,
            tenantId,
            userId);

        // Log response body for errors
        if (response.StatusCode >= 400)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await ReadStreamAsync(context.Response.Body);
            
            _logger.LogError(
                "Request {RequestId} error response: {ResponseBody}",
                requestId,
                responseBody);
        }
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        var buffer = new byte[stream.Length];
        await stream.ReadAsync(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer);
    }
}