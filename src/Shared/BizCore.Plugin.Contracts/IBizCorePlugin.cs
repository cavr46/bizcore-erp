namespace BizCore.Plugin.Contracts;

/// <summary>
/// Core interface for BizCore ERP plugins
/// Enables extensible functionality through community-driven plugins
/// </summary>
public interface IBizCorePlugin
{
    /// <summary>
    /// Unique plugin identifier
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display name for the plugin
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Plugin version (semantic versioning)
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Plugin author/organization
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// Plugin license (MIT, Apache, etc.)
    /// </summary>
    string License { get; }
    
    /// <summary>
    /// Plugin price (0 for free, >0 for paid)
    /// </summary>
    decimal Price { get; }
    
    /// <summary>
    /// Plugin category (Accounting, Inventory, etc.)
    /// </summary>
    PluginCategory Category { get; }
    
    /// <summary>
    /// Dependencies on other plugins
    /// </summary>
    IEnumerable<PluginDependency> Dependencies { get; }
    
    /// <summary>
    /// Supported BizCore versions
    /// </summary>
    string SupportedVersions { get; }
    
    /// <summary>
    /// Initialize plugin with DI container
    /// </summary>
    Task<PluginInitializationResult> InitializeAsync(IServiceCollection services, IConfiguration configuration);
    
    /// <summary>
    /// Configure plugin routes and middleware
    /// </summary>
    Task ConfigureAsync(IApplicationBuilder app);
    
    /// <summary>
    /// Execute plugin functionality
    /// </summary>
    Task<PluginExecutionResult> ExecuteAsync(PluginContext context);
    
    /// <summary>
    /// Validate plugin configuration
    /// </summary>
    Task<PluginValidationResult> ValidateAsync();
    
    /// <summary>
    /// Clean up plugin resources
    /// </summary>
    Task DisposeAsync();
}

/// <summary>
/// Plugin categories for marketplace organization
/// </summary>
public enum PluginCategory
{
    Accounting,
    Inventory,
    Sales,
    Purchasing,
    HumanResources,
    Manufacturing,
    Reporting,
    Integration,
    Workflow,
    Analytics,
    Security,
    Localization,
    Industry,
    Utilities
}

/// <summary>
/// Plugin dependency specification
/// </summary>
public record PluginDependency(
    string PluginId,
    string MinVersion,
    string MaxVersion,
    bool IsOptional = false
);

/// <summary>
/// Plugin initialization result
/// </summary>
public record PluginInitializationResult(
    bool Success,
    string Message,
    IEnumerable<string> Warnings = null,
    Exception Exception = null
);

/// <summary>
/// Plugin execution result
/// </summary>
public record PluginExecutionResult(
    bool Success,
    object Result = null,
    string Message = null,
    Exception Exception = null
);

/// <summary>
/// Plugin validation result
/// </summary>
public record PluginValidationResult(
    bool IsValid,
    IEnumerable<string> Errors = null,
    IEnumerable<string> Warnings = null
);

/// <summary>
/// Plugin execution context
/// </summary>
public class PluginContext
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public IServiceProvider ServiceProvider { get; set; }
    public CancellationToken CancellationToken { get; set; }
}