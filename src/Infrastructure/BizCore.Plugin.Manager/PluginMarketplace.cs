using BizCore.Plugin.Contracts;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BizCore.Plugin.Manager;

/// <summary>
/// Plugin marketplace for discovering, installing, and managing plugins
/// Similar to VS Code extensions marketplace
/// </summary>
public class PluginMarketplace : IPluginMarketplace
{
    private readonly ILogger<PluginMarketplace> _logger;
    private readonly IPluginRepository _repository;
    private readonly IPluginSecurity _security;
    private readonly HttpClient _httpClient;

    public PluginMarketplace(
        ILogger<PluginMarketplace> logger,
        IPluginRepository repository,
        IPluginSecurity security,
        HttpClient httpClient)
    {
        _logger = logger;
        _repository = repository;
        _security = security;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Search plugins in marketplace
    /// </summary>
    public async Task<IEnumerable<MarketplacePlugin>> SearchPluginsAsync(string query, 
        PluginCategory? category = null, int skip = 0, int take = 20)
    {
        try
        {
            var searchParams = new Dictionary<string, string>
            {
                ["q"] = query,
                ["skip"] = skip.ToString(),
                ["take"] = take.ToString()
            };

            if (category.HasValue)
                searchParams["category"] = category.Value.ToString();

            var queryString = string.Join("&", searchParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var response = await _httpClient.GetAsync($"https://marketplace.bizcore.com/api/plugins/search?{queryString}");
            
            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<MarketplacePlugin>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MarketplaceSearchResult>(json);
            
            return result?.Plugins ?? Enumerable.Empty<MarketplacePlugin>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search plugins for query: {Query}", query);
            return Enumerable.Empty<MarketplacePlugin>();
        }
    }

    /// <summary>
    /// Get plugin details from marketplace
    /// </summary>
    public async Task<MarketplacePlugin> GetPluginAsync(string pluginId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://marketplace.bizcore.com/api/plugins/{pluginId}");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MarketplacePlugin>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get plugin details for: {PluginId}", pluginId);
            return null;
        }
    }

    /// <summary>
    /// Install plugin from marketplace
    /// </summary>
    public async Task<PluginInstallResult> InstallPluginAsync(string pluginId, string version = null)
    {
        try
        {
            _logger.LogInformation("Installing plugin {PluginId} version {Version}", pluginId, version ?? "latest");

            // Get plugin details
            var plugin = await GetPluginAsync(pluginId);
            if (plugin == null)
            {
                return new PluginInstallResult(false, $"Plugin {pluginId} not found in marketplace");
            }

            // Select version
            var targetVersion = version ?? plugin.LatestVersion;
            var versionInfo = plugin.Versions.FirstOrDefault(v => v.Version == targetVersion);
            if (versionInfo == null)
            {
                return new PluginInstallResult(false, $"Version {targetVersion} not found for plugin {pluginId}");
            }

            // Security check
            var securityResult = await _security.ValidatePluginAsync(plugin, versionInfo);
            if (!securityResult.IsValid)
            {
                return new PluginInstallResult(false, $"Security validation failed: {string.Join(", ", securityResult.Issues)}");
            }

            // Check dependencies
            var dependencyResult = await CheckDependenciesAsync(versionInfo.Dependencies);
            if (!dependencyResult.Success)
            {
                return new PluginInstallResult(false, $"Dependency check failed: {dependencyResult.Message}");
            }

            // Download plugin
            var downloadResult = await DownloadPluginAsync(versionInfo.DownloadUrl, pluginId, targetVersion);
            if (!downloadResult.Success)
            {
                return new PluginInstallResult(false, $"Download failed: {downloadResult.Message}");
            }

            // Install plugin
            var installResult = await _repository.InstallPluginAsync(downloadResult.FilePath, pluginId, targetVersion);
            if (!installResult.Success)
            {
                return new PluginInstallResult(false, $"Installation failed: {installResult.Message}");
            }

            // Update installation record
            await _repository.RecordInstallationAsync(pluginId, targetVersion, DateTime.UtcNow);

            _logger.LogInformation("Plugin {PluginId} version {Version} installed successfully", pluginId, targetVersion);
            return new PluginInstallResult(true, $"Plugin {pluginId} installed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install plugin {PluginId}", pluginId);
            return new PluginInstallResult(false, $"Installation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Update plugin to latest version
    /// </summary>
    public async Task<PluginInstallResult> UpdatePluginAsync(string pluginId)
    {
        try
        {
            var installed = await _repository.GetInstalledPluginAsync(pluginId);
            if (installed == null)
            {
                return new PluginInstallResult(false, $"Plugin {pluginId} is not installed");
            }

            var plugin = await GetPluginAsync(pluginId);
            if (plugin == null)
            {
                return new PluginInstallResult(false, $"Plugin {pluginId} not found in marketplace");
            }

            if (installed.Version == plugin.LatestVersion)
            {
                return new PluginInstallResult(true, $"Plugin {pluginId} is already up to date");
            }

            // Backup current version
            await _repository.BackupPluginAsync(pluginId, installed.Version);

            // Install new version
            var installResult = await InstallPluginAsync(pluginId, plugin.LatestVersion);
            if (!installResult.Success)
            {
                // Restore backup
                await _repository.RestorePluginAsync(pluginId, installed.Version);
                return installResult;
            }

            _logger.LogInformation("Plugin {PluginId} updated from {OldVersion} to {NewVersion}", 
                pluginId, installed.Version, plugin.LatestVersion);
            
            return new PluginInstallResult(true, $"Plugin {pluginId} updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update plugin {PluginId}", pluginId);
            return new PluginInstallResult(false, $"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    public async Task<bool> UninstallPluginAsync(string pluginId)
    {
        try
        {
            var result = await _repository.UninstallPluginAsync(pluginId);
            if (result.Success)
            {
                _logger.LogInformation("Plugin {PluginId} uninstalled successfully", pluginId);
            }
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall plugin {PluginId}", pluginId);
            return false;
        }
    }

    /// <summary>
    /// Get installed plugins
    /// </summary>
    public async Task<IEnumerable<InstalledPlugin>> GetInstalledPluginsAsync()
    {
        return await _repository.GetInstalledPluginsAsync();
    }

    /// <summary>
    /// Check for plugin updates
    /// </summary>
    public async Task<IEnumerable<PluginUpdate>> CheckUpdatesAsync()
    {
        var installed = await GetInstalledPluginsAsync();
        var updates = new List<PluginUpdate>();

        foreach (var plugin in installed)
        {
            try
            {
                var marketplace = await GetPluginAsync(plugin.Id);
                if (marketplace != null && marketplace.LatestVersion != plugin.Version)
                {
                    updates.Add(new PluginUpdate(
                        plugin.Id,
                        plugin.Version,
                        marketplace.LatestVersion,
                        marketplace.Versions.FirstOrDefault(v => v.Version == marketplace.LatestVersion)?.ReleaseNotes
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check updates for plugin {PluginId}", plugin.Id);
            }
        }

        return updates;
    }

    private async Task<DependencyCheckResult> CheckDependenciesAsync(IEnumerable<PluginDependency> dependencies)
    {
        var missing = new List<string>();
        var installed = await GetInstalledPluginsAsync();

        foreach (var dependency in dependencies)
        {
            var installedDep = installed.FirstOrDefault(p => p.Id == dependency.PluginId);
            if (installedDep == null)
            {
                if (!dependency.IsOptional)
                    missing.Add(dependency.PluginId);
            }
            else
            {
                // Check version compatibility
                if (!IsVersionCompatible(installedDep.Version, dependency.MinVersion, dependency.MaxVersion))
                {
                    missing.Add($"{dependency.PluginId} (version conflict)");
                }
            }
        }

        return new DependencyCheckResult(missing.Count == 0, 
            missing.Count > 0 ? $"Missing dependencies: {string.Join(", ", missing)}" : null);
    }

    private async Task<DownloadResult> DownloadPluginAsync(string downloadUrl, string pluginId, string version)
    {
        try
        {
            var response = await _httpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            var fileName = $"{pluginId}-{version}.zip";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            await using var fileStream = File.Create(filePath);
            await response.Content.CopyToAsync(fileStream);

            return new DownloadResult(true, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download plugin {PluginId} from {Url}", pluginId, downloadUrl);
            return new DownloadResult(false, null, ex.Message);
        }
    }

    private bool IsVersionCompatible(string installedVersion, string minVersion, string maxVersion)
    {
        // Simple version comparison - in production, use a proper semver library
        return true; // Simplified for demo
    }
}

/// <summary>
/// Plugin marketplace interface
/// </summary>
public interface IPluginMarketplace
{
    Task<IEnumerable<MarketplacePlugin>> SearchPluginsAsync(string query, PluginCategory? category = null, int skip = 0, int take = 20);
    Task<MarketplacePlugin> GetPluginAsync(string pluginId);
    Task<PluginInstallResult> InstallPluginAsync(string pluginId, string version = null);
    Task<PluginInstallResult> UpdatePluginAsync(string pluginId);
    Task<bool> UninstallPluginAsync(string pluginId);
    Task<IEnumerable<InstalledPlugin>> GetInstalledPluginsAsync();
    Task<IEnumerable<PluginUpdate>> CheckUpdatesAsync();
}

/// <summary>
/// Marketplace plugin information
/// </summary>
public class MarketplacePlugin
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public string License { get; set; }
    public decimal Price { get; set; }
    public PluginCategory Category { get; set; }
    public string LatestVersion { get; set; }
    public List<PluginVersion> Versions { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string IconUrl { get; set; }
    public string DocumentationUrl { get; set; }
    public string RepositoryUrl { get; set; }
    public string SupportUrl { get; set; }
    public int Downloads { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Plugin version information
/// </summary>
public class PluginVersion
{
    public string Version { get; set; }
    public string ReleaseNotes { get; set; }
    public string DownloadUrl { get; set; }
    public long FileSize { get; set; }
    public string FileHash { get; set; }
    public DateTime ReleasedAt { get; set; }
    public List<PluginDependency> Dependencies { get; set; } = new();
}

/// <summary>
/// Installed plugin information
/// </summary>
public class InstalledPlugin
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public DateTime InstalledAt { get; set; }
    public string InstallPath { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Plugin update information
/// </summary>
public record PluginUpdate(
    string PluginId,
    string CurrentVersion,
    string LatestVersion,
    string ReleaseNotes
);

/// <summary>
/// Plugin installation result
/// </summary>
public record PluginInstallResult(
    bool Success,
    string Message,
    Exception Exception = null
);

/// <summary>
/// Dependency check result
/// </summary>
public record DependencyCheckResult(
    bool Success,
    string Message = null
);

/// <summary>
/// Download result
/// </summary>
public record DownloadResult(
    bool Success,
    string FilePath,
    string Message = null
);

/// <summary>
/// Marketplace search result
/// </summary>
public class MarketplaceSearchResult
{
    public List<MarketplacePlugin> Plugins { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}