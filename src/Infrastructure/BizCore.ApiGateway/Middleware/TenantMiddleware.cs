using System.Security.Claims;

namespace BizCore.ApiGateway.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = ExtractTenantId(context);
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            context.Items["TenantId"] = tenantId;
            context.Response.Headers.Add("X-Tenant-Id", tenantId);
        }

        await _next(context);
    }

    private string? ExtractTenantId(HttpContext context)
    {
        // Try to get tenant ID from various sources
        
        // 1. From header
        var headerTenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerTenantId))
        {
            return headerTenantId;
        }

        // 2. From JWT claims
        var claimTenantId = context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(claimTenantId))
        {
            return claimTenantId;
        }

        // 3. From query parameter
        var queryTenantId = context.Request.Query["tenant_id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(queryTenantId))
        {
            return queryTenantId;
        }

        // 4. From subdomain (if using subdomain-based multitenancy)
        var host = context.Request.Host.Host;
        if (host.Contains('.') && !host.StartsWith("www."))
        {
            var subdomain = host.Split('.')[0];
            if (!string.IsNullOrEmpty(subdomain))
            {
                return subdomain;
            }
        }

        _logger.LogWarning("No tenant ID found in request from {RemoteIpAddress}", 
            context.Connection.RemoteIpAddress);

        return null;
    }
}