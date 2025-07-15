namespace BizCore.Plugin.Contracts;

/// <summary>
/// Attribute to mark and configure BizCore plugins
/// Enables automatic plugin discovery and registration
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class BizCorePluginAttribute : Attribute
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string License { get; set; } = "MIT";
    public decimal Price { get; set; } = 0m;
    public PluginCategory Category { get; set; }
    public string SupportedVersions { get; set; } = "1.0.0+";
    public bool IsEnabled { get; set; } = true;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string IconUrl { get; set; }
    public string DocumentationUrl { get; set; }
    public string RepositoryUrl { get; set; }
    public string SupportUrl { get; set; }

    public BizCorePluginAttribute(string id, string name, string version)
    {
        Id = id;
        Name = name;
        Version = version;
    }
}

/// <summary>
/// Base class for BizCore plugins with common functionality
/// </summary>
public abstract class BizCorePluginBase : IBizCorePlugin
{
    protected ILogger Logger { get; private set; }
    protected IConfiguration Configuration { get; private set; }
    protected IServiceProvider ServiceProvider { get; private set; }

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Version { get; }
    public abstract string Author { get; }
    public virtual string License => "MIT";
    public virtual decimal Price => 0m;
    public abstract PluginCategory Category { get; }
    public virtual IEnumerable<PluginDependency> Dependencies => Enumerable.Empty<PluginDependency>();
    public virtual string SupportedVersions => "1.0.0+";

    public virtual async Task<PluginInitializationResult> InitializeAsync(IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            Configuration = configuration;
            
            // Register logger
            services.AddLogging();
            
            // Call derived class initialization
            await OnInitializeAsync(services, configuration);
            
            return new PluginInitializationResult(true, $"Plugin {Name} initialized successfully");
        }
        catch (Exception ex)
        {
            return new PluginInitializationResult(false, $"Plugin {Name} initialization failed: {ex.Message}", Exception: ex);
        }
    }

    public virtual async Task ConfigureAsync(IApplicationBuilder app)
    {
        ServiceProvider = app.ApplicationServices;
        Logger = ServiceProvider.GetRequiredService<ILogger<BizCorePluginBase>>();
        
        await OnConfigureAsync(app);
    }

    public virtual async Task<PluginExecutionResult> ExecuteAsync(PluginContext context)
    {
        try
        {
            var result = await OnExecuteAsync(context);
            return new PluginExecutionResult(true, result);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Plugin {PluginName} execution failed", Name);
            return new PluginExecutionResult(false, Exception: ex);
        }
    }

    public virtual async Task<PluginValidationResult> ValidateAsync()
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Basic validation
        if (string.IsNullOrEmpty(Id))
            errors.Add("Plugin ID cannot be empty");
        
        if (string.IsNullOrEmpty(Name))
            errors.Add("Plugin name cannot be empty");
        
        if (string.IsNullOrEmpty(Version))
            errors.Add("Plugin version cannot be empty");

        // Call derived class validation
        await OnValidateAsync(errors, warnings);

        return new PluginValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors,
            Warnings: warnings
        );
    }

    public virtual async Task DisposeAsync()
    {
        await OnDisposeAsync();
    }

    // Virtual methods for derived classes to override
    protected virtual Task OnInitializeAsync(IServiceCollection services, IConfiguration configuration) => Task.CompletedTask;
    protected virtual Task OnConfigureAsync(IApplicationBuilder app) => Task.CompletedTask;
    protected virtual Task<object> OnExecuteAsync(PluginContext context) => Task.FromResult<object>(null);
    protected virtual Task OnValidateAsync(List<string> errors, List<string> warnings) => Task.CompletedTask;
    protected virtual Task OnDisposeAsync() => Task.CompletedTask;
}