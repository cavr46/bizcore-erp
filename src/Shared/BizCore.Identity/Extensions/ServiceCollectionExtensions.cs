using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BizCore.Identity.Services;
using BizCore.Identity.Repositories;
using BizCore.Identity.Models;
using BizCore.Identity.Middleware;

namespace BizCore.Identity.Extensions;

/// <summary>
/// Service collection extensions for BizCore Identity
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add BizCore Identity services
    /// </summary>
    public static IServiceCollection AddBizCoreIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Identity options
        services.Configure<IdentityConfiguration>(configuration.GetSection("Identity"));
        
        // Add core identity services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        
        // Add repositories (these would need concrete implementations)
        services.AddScoped<IUserRepository, SqlUserRepository>();
        services.AddScoped<IRoleRepository, SqlRoleRepository>();
        services.AddScoped<IPermissionRepository, SqlPermissionRepository>();
        services.AddScoped<ITenantRepository, SqlTenantRepository>();
        services.AddScoped<ISessionRepository, SqlSessionRepository>();
        services.AddScoped<ISecurityEventRepository, SqlSecurityEventRepository>();
        services.AddScoped<ISsoProviderRepository, SqlSsoProviderRepository>();
        services.AddScoped<IApiKeyRepository, SqlApiKeyRepository>();
        
        // Add memory cache for tokens and rate limiting
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        
        // Add HTTP context accessor
        services.AddHttpContextAccessor();
        
        return services;
    }

    /// <summary>
    /// Add JWT authentication
    /// </summary>
    public static IServiceCollection AddBizCoreJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var identityConfig = configuration.GetSection("Identity").Get<IdentityConfiguration>();
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = identityConfig.JwtIssuer,
                ValidAudience = identityConfig.JwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(identityConfig.JwtSecretKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Allow token from query parameter for SignalR
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
                    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        var isRevoked = await tokenService.IsTokenRevokedAsync(token);
                        if (isRevoked)
                        {
                            context.Fail("Token has been revoked");
                        }
                    }
                },
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Add authorization policies
    /// </summary>
    public static IServiceCollection AddBizCoreAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Admin policy
            options.AddPolicy("Admin", policy =>
                policy.RequireRole("Admin").RequireClaim("permission", "Admin.Full"));

            // User management policies
            options.AddPolicy("UserManagement", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("permission", "Users.Read") ||
                    context.User.HasClaim("permission", "Users.Create") ||
                    context.User.HasClaim("permission", "Users.Update") ||
                    context.User.HasClaim("permission", "Users.Delete")));

            // Module-specific policies
            options.AddPolicy("Accounting", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("permission", "Accounting.Read") ||
                    context.User.HasClaim("permission", "Accounting.Create") ||
                    context.User.HasClaim("permission", "Accounting.Update") ||
                    context.User.HasClaim("permission", "Accounting.Delete")));

            options.AddPolicy("Sales", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("permission", "Sales.Read") ||
                    context.User.HasClaim("permission", "Sales.Create") ||
                    context.User.HasClaim("permission", "Sales.Update") ||
                    context.User.HasClaim("permission", "Sales.Delete")));

            options.AddPolicy("Inventory", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("permission", "Inventory.Read") ||
                    context.User.HasClaim("permission", "Inventory.Create") ||
                    context.User.HasClaim("permission", "Inventory.Update") ||
                    context.User.HasClaim("permission", "Inventory.Delete")));

            options.AddPolicy("Reports", policy =>
                policy.RequireClaim("permission", "Reports.Read"));

            // Multi-tenant policy
            options.AddPolicy("SameTenant", policy =>
                policy.RequireAssertion(context =>
                {
                    var userTenantId = context.User.FindFirst("tenant_id")?.Value;
                    var requestTenantId = context.Resource?.ToString();
                    return userTenantId == requestTenantId;
                }));

            // MFA required policy
            options.AddPolicy("MfaRequired", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("mfa_verified", "true")));

            // API key policy
            options.AddPolicy("ApiKey", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("auth_type", "api_key")));
        });

        return services;
    }

    /// <summary>
    /// Add CORS policies for multi-tenant support
    /// </summary>
    public static IServiceCollection AddBizCoreCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("BizCorePolicy", builder =>
            {
                builder
                    .WithOrigins(
                        "https://localhost:7000",
                        "https://localhost:7001",
                        "https://*.bizcore.com",
                        "https://*.bizcore.local"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowedToAllowWildcardSubdomains();
            });

            options.AddPolicy("ApiPolicy", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                    .WithExposedHeaders("X-Total-Count", "X-Pagination");
            });
        });

        return services;
    }

    /// <summary>
    /// Add background services for identity management
    /// </summary>
    public static IServiceCollection AddBizCoreBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<TokenCleanupService>();
        services.AddHostedService<SessionCleanupService>();
        services.AddHostedService<SecurityEventCleanupService>();
        
        return services;
    }

    /// <summary>
    /// Add health checks for identity services
    /// </summary>
    public static IServiceCollection AddBizCoreHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck<IdentityHealthCheck>("identity")
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<TokenServiceHealthCheck>("token_service")
            .AddCheck<MfaServiceHealthCheck>("mfa_service");

        return services;
    }
}

/// <summary>
/// Application builder extensions for BizCore Identity
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Use BizCore Identity middleware
    /// </summary>
    public static IApplicationBuilder UseBizCoreIdentity(this IApplicationBuilder app)
    {
        // Add security headers
        app.UseMiddleware<SecurityHeadersMiddleware>();
        
        // Add tenant resolution
        app.UseMiddleware<TenantResolutionMiddleware>();
        
        // Add rate limiting
        app.UseMiddleware<RateLimitingMiddleware>();
        
        // Add authentication
        app.UseAuthentication();
        app.UseMiddleware<AuthenticationMiddleware>();
        
        // Add authorization
        app.UseAuthorization();
        app.UseMiddleware<AuthorizationMiddleware>();
        
        return app;
    }

    /// <summary>
    /// Use BizCore CORS
    /// </summary>
    public static IApplicationBuilder UseBizCoreCors(this IApplicationBuilder app)
    {
        app.UseCors("BizCorePolicy");
        return app;
    }

    /// <summary>
    /// Initialize BizCore Identity database
    /// </summary>
    public static async Task InitializeBizCoreIdentityAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        
        // Initialize database
        await InitializeDatabaseAsync(scope.ServiceProvider);
        
        // Seed default data
        await SeedDefaultDataAsync(scope.ServiceProvider);
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        // This would run database migrations
        // Implementation depends on your database provider
    }

    private static async Task SeedDefaultDataAsync(IServiceProvider services)
    {
        var permissionRepo = services.GetRequiredService<IPermissionRepository>();
        var roleRepo = services.GetRequiredService<IRoleRepository>();
        
        // Seed default permissions
        var permissions = new[]
        {
            new Permission { Name = "Admin.Full", Description = "Full administrative access", Module = "Admin", Category = "Administration" },
            new Permission { Name = "Users.Read", Description = "View users", Module = "Users", Category = "User Management" },
            new Permission { Name = "Users.Create", Description = "Create users", Module = "Users", Category = "User Management" },
            new Permission { Name = "Users.Update", Description = "Update users", Module = "Users", Category = "User Management" },
            new Permission { Name = "Users.Delete", Description = "Delete users", Module = "Users", Category = "User Management" },
            new Permission { Name = "Accounting.Read", Description = "View accounting data", Module = "Accounting", Category = "Financial" },
            new Permission { Name = "Accounting.Create", Description = "Create accounting entries", Module = "Accounting", Category = "Financial" },
            new Permission { Name = "Accounting.Update", Description = "Update accounting entries", Module = "Accounting", Category = "Financial" },
            new Permission { Name = "Accounting.Delete", Description = "Delete accounting entries", Module = "Accounting", Category = "Financial" },
            new Permission { Name = "Sales.Read", Description = "View sales data", Module = "Sales", Category = "Sales" },
            new Permission { Name = "Sales.Create", Description = "Create sales entries", Module = "Sales", Category = "Sales" },
            new Permission { Name = "Sales.Update", Description = "Update sales entries", Module = "Sales", Category = "Sales" },
            new Permission { Name = "Sales.Delete", Description = "Delete sales entries", Module = "Sales", Category = "Sales" },
            new Permission { Name = "Inventory.Read", Description = "View inventory", Module = "Inventory", Category = "Operations" },
            new Permission { Name = "Inventory.Create", Description = "Create inventory items", Module = "Inventory", Category = "Operations" },
            new Permission { Name = "Inventory.Update", Description = "Update inventory items", Module = "Inventory", Category = "Operations" },
            new Permission { Name = "Inventory.Delete", Description = "Delete inventory items", Module = "Inventory", Category = "Operations" },
            new Permission { Name = "Reports.Read", Description = "View reports", Module = "Reports", Category = "Analytics" }
        };

        foreach (var permission in permissions)
        {
            if (!await permissionRepo.ExistsAsync(permission.Id))
            {
                await permissionRepo.CreateAsync(permission);
            }
        }

        // Seed default roles
        var roles = new[]
        {
            new Role { Name = "Admin", Description = "System Administrator", IsSystem = true },
            new Role { Name = "Manager", Description = "Department Manager", IsSystem = true },
            new Role { Name = "User", Description = "Regular User", IsSystem = true },
            new Role { Name = "Accountant", Description = "Accounting Staff", IsSystem = true },
            new Role { Name = "Sales", Description = "Sales Staff", IsSystem = true },
            new Role { Name = "Viewer", Description = "Read-only access", IsSystem = true }
        };

        foreach (var role in roles)
        {
            if (!await roleRepo.ExistsAsync(role.Id))
            {
                await roleRepo.CreateAsync(role);
            }
        }
    }
}

/// <summary>
/// Security headers middleware
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");
        
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }

        await _next(context);
    }
}

// Background service implementations
public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TokenCleanupService> _logger;

    public TokenCleanupService(IServiceProvider services, ILogger<TokenCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
                var apiKeyRepo = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
                
                var cleanedSessions = await sessionRepo.CleanupExpiredSessionsAsync();
                var cleanedApiKeys = await apiKeyRepo.CleanupExpiredKeysAsync();
                
                if (cleanedSessions > 0 || cleanedApiKeys > 0)
                {
                    _logger.LogInformation("Cleaned up {Sessions} expired sessions and {ApiKeys} expired API keys", 
                        cleanedSessions, cleanedApiKeys);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(IServiceProvider services, ILogger<SessionCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
                
                var cutoffDate = DateTime.UtcNow.AddDays(-30);
                var cleanedSessions = await sessionRepo.CleanupOldSessionsAsync(cutoffDate);
                
                if (cleanedSessions > 0)
                {
                    _logger.LogInformation("Cleaned up {Sessions} old sessions", cleanedSessions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}

public class SecurityEventCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SecurityEventCleanupService> _logger;

    public SecurityEventCleanupService(IServiceProvider services, ILogger<SecurityEventCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var eventRepo = scope.ServiceProvider.GetRequiredService<ISecurityEventRepository>();
                
                var cutoffDate = DateTime.UtcNow.AddDays(-90);
                var cleanedEvents = await eventRepo.CleanupOldEventsAsync(cutoffDate);
                
                if (cleanedEvents > 0)
                {
                    _logger.LogInformation("Cleaned up {Events} old security events", cleanedEvents);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security event cleanup");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}

// Health check implementations
public class IdentityHealthCheck : IHealthCheck
{
    private readonly IIdentityService _identityService;

    public IdentityHealthCheck(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a simple health check
            var tenants = await _identityService.GetTenantsAsync();
            return HealthCheckResult.Healthy("Identity service is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Identity service is unhealthy", ex);
        }
    }
}

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _services;

    public DatabaseHealthCheck(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _services.CreateScope();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            
            // Test database connectivity
            var count = await userRepo.GetUserCountAsync();
            return HealthCheckResult.Healthy($"Database is healthy. User count: {count}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unhealthy", ex);
        }
    }
}

public class TokenServiceHealthCheck : IHealthCheck
{
    private readonly ITokenService _tokenService;

    public TokenServiceHealthCheck(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test token service by checking if a dummy token is revoked
            var isRevoked = await _tokenService.IsTokenRevokedAsync("dummy-token");
            return HealthCheckResult.Healthy("Token service is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Token service is unhealthy", ex);
        }
    }
}

public class MfaServiceHealthCheck : IHealthCheck
{
    private readonly IMfaService _mfaService;

    public MfaServiceHealthCheck(IMfaService mfaService)
    {
        _mfaService = mfaService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test MFA service by generating a dummy token
            var token = await _mfaService.GenerateMfaTokenAsync("dummy-user");
            return HealthCheckResult.Healthy("MFA service is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MFA service is unhealthy", ex);
        }
    }
}