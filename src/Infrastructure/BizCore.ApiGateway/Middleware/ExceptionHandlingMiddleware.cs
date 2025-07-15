using System.Net;
using System.Text.Json;

namespace BizCore.ApiGateway.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.Items["RequestId"]?.ToString();
        var tenantId = context.Items["TenantId"]?.ToString();
        var userId = context.User?.Identity?.Name;

        _logger.LogError(exception,
            "Unhandled exception occurred for request {RequestId} " +
            "(Tenant: {TenantId}, User: {UserId}): {ExceptionMessage}",
            requestId,
            tenantId,
            userId,
            exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            RequestId = requestId,
            Message = "An error occurred while processing your request."
        };

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = validationEx.Message;
                errorResponse.Details = validationEx.Errors;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = "You are not authorized to access this resource.";
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = "The requested resource was not found.";
                break;

            case TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Message = "The request timed out. Please try again.";
                break;

            case InvalidOperationException invalidOpEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = invalidOpEx.Message;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "An internal server error occurred.";
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public string? RequestId { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ValidationException : Exception
{
    public object? Errors { get; }

    public ValidationException(string message, object? errors = null) : base(message)
    {
        Errors = errors;
    }
}