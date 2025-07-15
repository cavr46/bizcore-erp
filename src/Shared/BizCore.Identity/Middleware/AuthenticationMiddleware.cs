using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BizCore.Identity.Services;
using BizCore.Identity.Models;

namespace BizCore.Identity.Middleware;

/// <summary>
/// Authentication middleware for JWT token validation and tenant resolution
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;
    private readonly IdentityConfiguration _config;

    public AuthenticationMiddleware(
        RequestDelegate next,
        ILogger<AuthenticationMiddleware> logger,
        IOptions<IdentityConfiguration> config)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService, IIdentityService identityService)
    {
        try
        {
            // Skip authentication for certain paths
            if (ShouldSkipAuthentication(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Extract token from Authorization header
            var token = ExtractTokenFromHeader(context.Request);
            if (string.IsNullOrEmpty(token))
            {
                // Try to extract from cookie
                token = ExtractTokenFromCookie(context.Request);
            }

            if (!string.IsNullOrEmpty(token))
            {
                // Validate token
                var principal = await tokenService.ValidateTokenAsync(token);
                if (principal != null)
                {
                    // Set user context
                    context.User = principal;

                    // Extract tenant information
                    var tenantId = principal.FindFirst("tenant_id")?.Value;
                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        context.Items["TenantId"] = tenantId;
                    }

                    if (!string.IsNullOrEmpty(userId))
                    {
                        context.Items["UserId"] = userId;
                        
                        // Update last activity for session tracking
                        await UpdateUserActivity(context, userId, identityService);
                    }

                    // Add security headers
                    AddSecurityHeaders(context.Response, tenantId);
                }
                else
                {
                    // Token is invalid, clear any existing authentication
                    context.User = new ClaimsPrincipal();
                    _logger.LogWarning("Invalid token provided from IP: {IpAddress}", GetClientIpAddress(context));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication middleware");
            // Continue processing even if authentication fails
        }

        await _next(context);
    }

    private string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }

    private string? ExtractTokenFromCookie(HttpRequest request)
    {
        return request.Cookies["access_token"];
    }

    private bool ShouldSkipAuthentication(PathString path)
    {
        var skipPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/forgot-password",
            "/api/auth/reset-password",
            "/api/auth/confirm-email",
            "/api/health",
            "/api/ping",
            "/swagger",
            "/favicon.ico",
            "/_blazor",
            "/_framework",
            "/css",
            "/js",
            "/images"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private async Task UpdateUserActivity(HttpContext context, string userId, IIdentityService identityService)
    {
        try
        {
            var sessionId = context.Items["SessionId"]?.ToString();
            if (!string.IsNullOrEmpty(sessionId))
            {
                // This would update the session's last activity
                // Implementation depends on your session management strategy
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user activity for user {UserId}", userId);
        }
    }

    private void AddSecurityHeaders(HttpResponse response, string? tenantId)
    {
        response.Headers.Add("X-Content-Type-Options", "nosniff");
        response.Headers.Add("X-Frame-Options", "DENY");
        response.Headers.Add("X-XSS-Protection", "1; mode=block");
        response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            response.Headers.Add("X-Tenant-ID", tenantId);
        }
    }

    private string GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

/// <summary>
/// Authorization middleware for role and permission-based access control
/// </summary>
public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdentityService identityService)
    {
        try
        {
            // Skip authorization for certain paths
            if (ShouldSkipAuthorization(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Only check authorization for authenticated users
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tenantId = context.User.FindFirst("tenant_id")?.Value;

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(tenantId))
                {
                    // Check if user has required permissions for this endpoint
                    var requiredPermission = GetRequiredPermission(context.Request);
                    if (!string.IsNullOrEmpty(requiredPermission))
                    {
                        var hasPermission = await identityService.HasPermissionAsync(userId, requiredPermission);
                        if (!hasPermission)
                        {
                            _logger.LogWarning("User {UserId} attempted to access {Path} without required permission {Permission}",
                                userId, context.Request.Path, requiredPermission);
                            
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("Forbidden: Insufficient permissions");
                            return;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authorization middleware");
            // Continue processing even if authorization check fails
        }

        await _next(context);
    }

    private bool ShouldSkipAuthorization(PathString path)
    {
        var skipPaths = new[]
        {
            "/api/auth/",
            "/api/health",
            "/api/ping",
            "/swagger",
            "/favicon.ico",
            "/_blazor",
            "/_framework",
            "/css",
            "/js",
            "/images"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private string? GetRequiredPermission(HttpRequest request)
    {
        // Map API endpoints to required permissions
        var path = request.Path.Value?.ToLowerInvariant();
        var method = request.Method.ToUpperInvariant();

        if (path == null) return null;

        // Accounting module permissions
        if (path.StartsWith("/api/accounting/"))
        {
            return method switch
            {
                "GET" => "Accounting.Read",
                "POST" => "Accounting.Create",
                "PUT" => "Accounting.Update",
                "PATCH" => "Accounting.Update",
                "DELETE" => "Accounting.Delete",
                _ => "Accounting.Read"
            };
        }

        // Sales module permissions
        if (path.StartsWith("/api/sales/"))
        {
            return method switch
            {
                "GET" => "Sales.Read",
                "POST" => "Sales.Create",
                "PUT" => "Sales.Update",
                "PATCH" => "Sales.Update",
                "DELETE" => "Sales.Delete",
                _ => "Sales.Read"
            };
        }

        // Admin permissions
        if (path.StartsWith("/api/admin/"))
        {
            return "Admin.Full";
        }

        // User management permissions
        if (path.StartsWith("/api/users/"))
        {
            return method switch
            {
                "GET" => "Users.Read",
                "POST" => "Users.Create",
                "PUT" => "Users.Update",
                "PATCH" => "Users.Update",
                "DELETE" => "Users.Delete",
                _ => "Users.Read"
            };
        }

        // Reports permissions
        if (path.StartsWith("/api/reports/"))
        {
            return "Reports.Read";
        }

        // Default permission for authenticated endpoints
        return null;
    }
}

/// <summary>
/// Tenant resolution middleware for multi-tenant applications
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdentityService identityService)
    {
        try
        {
            // Skip tenant resolution for certain paths
            if (ShouldSkipTenantResolution(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Try to resolve tenant from various sources
            var tenantId = ResolveTenantId(context);
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                // Validate tenant exists and is active
                var tenant = await identityService.GetTenantAsync(tenantId);
                if (tenant.IsSuccess && tenant.Tenant.IsActive)
                {
                    context.Items["TenantId"] = tenantId;
                    context.Items["Tenant"] = tenant.Tenant;
                    
                    // Add tenant information to response headers
                    context.Response.Headers.Add("X-Tenant-ID", tenantId);
                    context.Response.Headers.Add("X-Tenant-Name", tenant.Tenant.Name);
                }
                else
                {
                    _logger.LogWarning("Invalid or inactive tenant {TenantId} accessed from {IpAddress}", 
                        tenantId, GetClientIpAddress(context));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant resolution middleware");
            // Continue processing even if tenant resolution fails
        }

        await _next(context);
    }

    private string? ResolveTenantId(HttpContext context)
    {
        // 1. Check user claims first (for authenticated users)
        var tenantFromClaims = context.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantFromClaims))
        {
            return tenantFromClaims;
        }

        // 2. Check query parameter
        var tenantFromQuery = context.Request.Query["tenant"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantFromQuery))
        {
            return tenantFromQuery;
        }

        // 3. Check header
        var tenantFromHeader = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantFromHeader))
        {
            return tenantFromHeader;
        }

        // 4. Check subdomain
        var tenantFromSubdomain = ResolveTenantFromSubdomain(context.Request.Host.Host);
        if (!string.IsNullOrEmpty(tenantFromSubdomain))
        {
            return tenantFromSubdomain;
        }

        // 5. Check custom domain
        var tenantFromDomain = ResolveTenantFromDomain(context.Request.Host.Host);
        if (!string.IsNullOrEmpty(tenantFromDomain))
        {
            return tenantFromDomain;
        }

        return null;
    }

    private string? ResolveTenantFromSubdomain(string host)
    {
        // Extract subdomain from host (e.g., "company.bizcore.com" -> "company")
        var parts = host.Split('.');
        if (parts.Length >= 3 && !parts[0].Equals("www", StringComparison.OrdinalIgnoreCase))
        {
            return parts[0];
        }
        return null;
    }

    private string? ResolveTenantFromDomain(string host)
    {
        // This would check if the domain is mapped to a specific tenant
        // Implementation would depend on your domain mapping strategy
        return null;
    }

    private bool ShouldSkipTenantResolution(PathString path)
    {
        var skipPaths = new[]
        {
            "/api/auth/register",
            "/api/tenants/",
            "/api/health",
            "/api/ping",
            "/swagger",
            "/_blazor",
            "/_framework"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private string GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

/// <summary>
/// Rate limiting middleware for API endpoints
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IdentityConfiguration _config;
    private readonly IMemoryCache _cache;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<IdentityConfiguration> config,
        IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (!_config.EnableRateLimiting)
            {
                await _next(context);
                return;
            }

            // Skip rate limiting for certain paths
            if (ShouldSkipRateLimit(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Get rate limit key (IP address + user ID if authenticated)
            var rateLimitKey = GetRateLimitKey(context);
            
            // Check rate limit
            if (await IsRateLimitExceededAsync(rateLimitKey))
            {
                _logger.LogWarning("Rate limit exceeded for key: {Key}", rateLimitKey);
                
                context.Response.StatusCode = 429;
                context.Response.Headers.Add("Retry-After", "900"); // 15 minutes
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Update rate limit counter
            await UpdateRateLimitAsync(rateLimitKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware");
            // Continue processing even if rate limiting fails
        }

        await _next(context);
    }

    private string GetRateLimitKey(HttpContext context)
    {
        var ip = GetClientIpAddress(context);
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        return !string.IsNullOrEmpty(userId) ? $"user:{userId}" : $"ip:{ip}";
    }

    private async Task<bool> IsRateLimitExceededAsync(string key)
    {
        var rateLimitKey = $"rate_limit:{key}";
        var requestCount = _cache.Get<int>(rateLimitKey);
        
        return requestCount >= _config.RateLimitRequests;
    }

    private async Task UpdateRateLimitAsync(string key)
    {
        var rateLimitKey = $"rate_limit:{key}";
        var currentCount = _cache.Get<int>(rateLimitKey);
        
        _cache.Set(rateLimitKey, currentCount + 1, TimeSpan.FromMinutes(_config.RateLimitWindowMinutes));
    }

    private bool ShouldSkipRateLimit(PathString path)
    {
        var skipPaths = new[]
        {
            "/api/health",
            "/api/ping",
            "/_blazor",
            "/_framework",
            "/css",
            "/js",
            "/images"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private string GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}