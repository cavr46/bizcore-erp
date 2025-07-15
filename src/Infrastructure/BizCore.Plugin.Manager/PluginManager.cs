using System.Reflection;
using System.Runtime.Loader;
using BizCore.Plugin.Contracts;
using Microsoft.Extensions.Logging;

namespace BizCore.Plugin.Manager;

/// <summary>
/// Core plugin manager for BizCore ERP
/// Handles plugin discovery, loading, and lifecycle management
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly PluginConfiguration _configuration;
    private readonly ConcurrentDictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly ConcurrentDictionary<string, AssemblyLoadContext> _pluginContexts = new();

    public PluginManager(ILogger<PluginManager> logger, PluginConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Discover plugins from specified directories
    /// </summary>
    public async Task<IEnumerable<PluginManifest>> DiscoverPluginsAsync()
    {
        var plugins = new List<PluginManifest>();

        foreach (var directory in _configuration.PluginDirectories)
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogWarning("Plugin directory not found: {Directory}", directory);
                continue;
            }

            var pluginFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);
            
            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    var manifest = await AnalyzePluginAsync(pluginFile);
                    if (manifest != null)
                    {
                        plugins.Add(manifest);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to analyze plugin: {PluginFile}", pluginFile);
                }
            }
        }

        return plugins;
    }

    /// <summary>
    /// Load a plugin by ID
    /// </summary>
    public async Task<PluginLoadResult> LoadPluginAsync(string pluginId)
    {
        if (_loadedPlugins.ContainsKey(pluginId))
        {
            return new PluginLoadResult(true, $"Plugin {pluginId} already loaded");
        }

        try
        {
            var manifest = await FindPluginManifestAsync(pluginId);
            if (manifest == null)
            {
                return new PluginLoadResult(false, $"Plugin {pluginId} not found");
            }

            // Create isolated load context
            var context = new AssemblyLoadContext($"Plugin_{pluginId}", isCollectible: true);
            var assembly = context.LoadFromAssemblyPath(manifest.AssemblyPath);

            // Find plugin class
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => t.GetCustomAttribute<BizCorePluginAttribute>()?.Id == pluginId);

            if (pluginType == null)
            {
                return new PluginLoadResult(false, $"Plugin class not found for {pluginId}");
            }

            // Create plugin instance
            var plugin = Activator.CreateInstance(pluginType) as IBizCorePlugin;
            if (plugin == null)
            {
                return new PluginLoadResult(false, $"Failed to create plugin instance for {pluginId}");
            }

            // Validate plugin
            var validationResult = await plugin.ValidateAsync();
            if (!validationResult.IsValid)
            {
                return new PluginLoadResult(false, $"Plugin validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            // Store loaded plugin
            var loadedPlugin = new LoadedPlugin(manifest, plugin, context);
            _loadedPlugins[pluginId] = loadedPlugin;
            _pluginContexts[pluginId] = context;

            _logger.LogInformation("Plugin {PluginId} loaded successfully", pluginId);
            return new PluginLoadResult(true, $"Plugin {pluginId} loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin {PluginId}", pluginId);
            return new PluginLoadResult(false, $"Failed to load plugin {pluginId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Unload a plugin by ID
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string pluginId)
    {
        if (!_loadedPlugins.TryRemove(pluginId, out var loadedPlugin))
        {
            return false;
        }

        try
        {
            await loadedPlugin.Plugin.DisposeAsync();
            
            if (_pluginContexts.TryRemove(pluginId, out var context))
            {
                context.Unload();
            }

            _logger.LogInformation("Plugin {PluginId} unloaded successfully", pluginId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin {PluginId}", pluginId);
            return false;
        }
    }

    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    public IEnumerable<LoadedPlugin> GetLoadedPlugins()
    {
        return _loadedPlugins.Values;
    }

    /// <summary>
    /// Get plugin by ID
    /// </summary>
    public LoadedPlugin GetPlugin(string pluginId)
    {
        _loadedPlugins.TryGetValue(pluginId, out var plugin);
        return plugin;
    }

    /// <summary>
    /// Execute plugin action
    /// </summary>
    public async Task<PluginExecutionResult> ExecutePluginAsync(string pluginId, PluginContext context)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin == null)
        {
            return new PluginExecutionResult(false, Message: $"Plugin {pluginId} not found");
        }

        return await plugin.Plugin.ExecuteAsync(context);
    }

    /// <summary>
    /// Initialize all loaded plugins
    /// </summary>
    public async Task InitializePluginsAsync(IServiceCollection services, IConfiguration configuration)
    {
        var initializationTasks = _loadedPlugins.Values
            .Select(async plugin =>
            {
                try
                {
                    var result = await plugin.Plugin.InitializeAsync(services, configuration);
                    if (!result.Success)
                    {
                        _logger.LogError("Plugin {PluginId} initialization failed: {Message}", 
                            plugin.Plugin.Id, result.Message);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Plugin {PluginId} initialization error", plugin.Plugin.Id);
                    return new PluginInitializationResult(false, ex.Message, Exception: ex);
                }
            });

        await Task.WhenAll(initializationTasks);
    }

    /// <summary>
    /// Configure all loaded plugins
    /// </summary>
    public async Task ConfigurePluginsAsync(IApplicationBuilder app)
    {
        var configurationTasks = _loadedPlugins.Values
            .Select(async plugin =>
            {
                try
                {
                    await plugin.Plugin.ConfigureAsync(app);
                    _logger.LogInformation("Plugin {PluginId} configured successfully", plugin.Plugin.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Plugin {PluginId} configuration error", plugin.Plugin.Id);
                }
            });

        await Task.WhenAll(configurationTasks);
    }

    private async Task<PluginManifest> AnalyzePluginAsync(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<BizCorePluginAttribute>() != null)
                .ToList();

            if (!pluginTypes.Any())
                return null;

            var pluginType = pluginTypes.First();
            var attribute = pluginType.GetCustomAttribute<BizCorePluginAttribute>();

            return new PluginManifest
            {
                Id = attribute.Id,
                Name = attribute.Name,
                Description = attribute.Description,
                Version = attribute.Version,
                Author = attribute.Author,
                License = attribute.License,
                Price = attribute.Price,
                Category = attribute.Category,
                SupportedVersions = attribute.SupportedVersions,
                AssemblyPath = assemblyPath,
                TypeName = pluginType.FullName,
                IsEnabled = attribute.IsEnabled,
                Tags = attribute.Tags,
                IconUrl = attribute.IconUrl,
                DocumentationUrl = attribute.DocumentationUrl,
                RepositoryUrl = attribute.RepositoryUrl,
                SupportUrl = attribute.SupportUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze plugin assembly: {AssemblyPath}", assemblyPath);
            return null;
        }
    }

    private async Task<PluginManifest> FindPluginManifestAsync(string pluginId)
    {
        var plugins = await DiscoverPluginsAsync();
        return plugins.FirstOrDefault(p => p.Id == pluginId);
    }

    public void Dispose()
    {
        foreach (var plugin in _loadedPlugins.Values)
        {
            try
            {
                plugin.Plugin.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing plugin {PluginId}", plugin.Plugin.Id);
            }
        }

        foreach (var context in _pluginContexts.Values)
        {
            try
            {
                context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading plugin context");
            }
        }
    }
}

/// <summary>
/// Plugin manager interface
/// </summary>
public interface IPluginManager : IDisposable
{
    Task<IEnumerable<PluginManifest>> DiscoverPluginsAsync();
    Task<PluginLoadResult> LoadPluginAsync(string pluginId);
    Task<bool> UnloadPluginAsync(string pluginId);
    IEnumerable<LoadedPlugin> GetLoadedPlugins();
    LoadedPlugin GetPlugin(string pluginId);
    Task<PluginExecutionResult> ExecutePluginAsync(string pluginId, PluginContext context);
    Task InitializePluginsAsync(IServiceCollection services, IConfiguration configuration);
    Task ConfigurePluginsAsync(IApplicationBuilder app);
}

/// <summary>
/// Plugin manifest with metadata
/// </summary>
public class PluginManifest
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string License { get; set; }
    public decimal Price { get; set; }
    public PluginCategory Category { get; set; }
    public string SupportedVersions { get; set; }
    public string AssemblyPath { get; set; }
    public string TypeName { get; set; }
    public bool IsEnabled { get; set; }
    public string[] Tags { get; set; }
    public string IconUrl { get; set; }
    public string DocumentationUrl { get; set; }
    public string RepositoryUrl { get; set; }
    public string SupportUrl { get; set; }
}

/// <summary>
/// Loaded plugin with context
/// </summary>
public record LoadedPlugin(
    PluginManifest Manifest,
    IBizCorePlugin Plugin,
    AssemblyLoadContext Context
);

/// <summary>
/// Plugin load result
/// </summary>
public record PluginLoadResult(
    bool Success,
    string Message,
    Exception Exception = null
);

/// <summary>
/// Plugin configuration
/// </summary>
public class PluginConfiguration
{
    public string[] PluginDirectories { get; set; } = { "plugins" };
    public bool EnableHotReload { get; set; } = true;
    public bool EnableSandboxing { get; set; } = true;
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.FromMinutes(5);
}