using BizCore.Plugin.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace BizCore.Plugin.Manager;

/// <summary>
/// Service collection extensions for plugin system
/// </summary>
public static class PluginServiceExtensions
{
    /// <summary>
    /// Add BizCore plugin system to services
    /// </summary>
    public static IServiceCollection AddBizCorePlugins(this IServiceCollection services, 
        Action<PluginConfiguration> configureOptions = null)
    {
        var configuration = new PluginConfiguration();
        configureOptions?.Invoke(configuration);

        services.AddSingleton(configuration);
        services.AddSingleton<IPluginManager, PluginManager>();
        services.AddSingleton<IPluginMarketplace, PluginMarketplace>();
        services.AddSingleton<IPluginRepository, PluginRepository>();
        services.AddSingleton<IPluginSecurity, PluginSecurity>();

        return services;
    }

    /// <summary>
    /// Use BizCore plugin system in application
    /// </summary>
    public static async Task<IApplicationBuilder> UseBizCorePlugins(this IApplicationBuilder app)
    {
        var pluginManager = app.ApplicationServices.GetRequiredService<IPluginManager>();
        
        // Auto-discover and load plugins
        var plugins = await pluginManager.DiscoverPluginsAsync();
        foreach (var plugin in plugins.Where(p => p.IsEnabled))
        {
            await pluginManager.LoadPluginAsync(plugin.Id);
        }

        // Configure loaded plugins
        await pluginManager.ConfigurePluginsAsync(app);

        return app;
    }
}