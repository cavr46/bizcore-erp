using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using BizCore.Identity.Models;
using BizCore.Identity.Services;
using BizCore.Identity.Repositories;

namespace BizCore.Identity.Grains;

/// <summary>
/// Tenant grain for distributed multi-tenant management
/// </summary>
public interface ITenantGrain : IGrainWithStringKey
{
    Task<Tenant?> GetTenantAsync();
    Task<bool> SetTenantAsync(Tenant tenant);
    Task<bool> ActivateAsync();
    Task<bool> DeactivateAsync();
    Task<bool> UpdateSettingsAsync(TenantSettings settings);
    Task<bool> UpdateSubscriptionAsync(TenantSubscription subscription);
    Task<bool> EnableFeatureAsync(string featureName);
    Task<bool> DisableFeatureAsync(string featureName);
    Task<bool> HasFeatureAsync(string featureName);
    Task<string[]> GetEnabledFeaturesAsync();
    Task<int> GetUserCountAsync();
    Task<int> GetActiveUserCountAsync();
    Task<bool> IsWithinUserLimitAsync();
    Task<bool> IsWithinStorageLimitAsync(long currentStorageBytes);
    Task<bool> CanCreateUserAsync();
    Task<bool> ValidateAsync();
    Task<TenantMetrics> GetMetricsAsync();
    Task<bool> UpdateUsageAsync(long storageBytes, int userCount);
    Task<bool> IsSubscriptionActiveAsync();
    Task<bool> IsTrialActiveAsync();
    Task<DateTime?> GetTrialExpirationAsync();
    Task<bool> ExtendTrialAsync(DateTime newExpirationDate);
    Task<bool> UpgradeSubscriptionAsync(string newPlanId, decimal newPrice);
    Task<bool> CancelSubscriptionAsync();
    Task<SecurityMetrics> GetSecurityMetricsAsync();
}

[Reentrant]
public class TenantGrain : Grain, ITenantGrain
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISecurityEventRepository _securityEventRepository;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<TenantGrain> _logger;
    private readonly IPersistentState<TenantState> _tenantState;

    private Tenant? _tenant;
    private DateTime _lastAccessed;
    private bool _isLoaded;
    private TenantMetrics? _cachedMetrics;
    private DateTime _metricsLastUpdated;

    public TenantGrain(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityAuditService auditService,
        ILogger<TenantGrain> logger,
        [PersistentState("tenant", "tenantStore")] IPersistentState<TenantState> tenantState)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _securityEventRepository = securityEventRepository;
        _auditService = auditService;
        _logger = logger;
        _tenantState = tenantState;
    }

    public override async Task OnActivateAsync()
    {
        await LoadTenantAsync();
        _lastAccessed = DateTime.UtcNow;
        
        // Set up deactivation timer
        RegisterTimer(CheckDeactivation, null, TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(60));
        
        // Set up metrics refresh timer
        RegisterTimer(RefreshMetrics, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<Tenant?> GetTenantAsync()
    {
        await EnsureLoadedAsync();
        _lastAccessed = DateTime.UtcNow;
        return _tenant;
    }

    public async Task<bool> SetTenantAsync(Tenant tenant)
    {
        try
        {
            _tenant = tenant;
            _tenantState.State.Tenant = tenant;
            _tenantState.State.LastModified = DateTime.UtcNow;
            await _tenantState.WriteStateAsync();
            
            // Also update in repository
            await _tenantRepository.UpdateAsync(tenant);
            
            await LogSecurityEventAsync(SecurityEventType.TenantUpdated, "Tenant data updated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set tenant {TenantId}", this.GetPrimaryKeyString());
            return false;
        }
    }

    public async Task<bool> ActivateAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            _tenant.IsActive = true;
            _tenant.UpdatedAt = DateTime.UtcNow;
            _tenant.ActivatedAt = DateTime.UtcNow;
            
            await _tenantRepository.ActivateAsync(_tenant.Id);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.TenantUpdated, "Tenant activated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<bool> DeactivateAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            _tenant.IsActive = false;
            _tenant.UpdatedAt = DateTime.UtcNow;
            _tenant.DeactivatedAt = DateTime.UtcNow;
            
            await _tenantRepository.DeactivateAsync(_tenant.Id);
            await UpdateStateAsync();
            
            // End all user sessions for this tenant
            await EndAllTenantSessionsAsync();
            
            await LogSecurityEventAsync(SecurityEventType.TenantDeleted, "Tenant deactivated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<bool> UpdateSettingsAsync(TenantSettings settings)
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            _tenant.Settings = settings;
            _tenant.UpdatedAt = DateTime.UtcNow;
            
            await _tenantRepository.UpdateSettingsAsync(_tenant.Id, settings);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.ConfigurationChanged, "Tenant settings updated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings for tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<bool> UpdateSubscriptionAsync(TenantSubscription subscription)
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            _tenant.Subscription = subscription;
            _tenant.UpdatedAt = DateTime.UtcNow;
            
            await _tenantRepository.UpdateSubscriptionAsync(_tenant.Id, subscription);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.TenantUpdated, "Subscription updated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subscription for tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<bool> EnableFeatureAsync(string featureName)
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            await _tenantRepository.EnableFeatureAsync(_tenant.Id, featureName);
            await LogSecurityEventAsync(SecurityEventType.ConfigurationChanged, $"Feature {featureName} enabled");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable feature {FeatureName} for tenant {TenantId}", featureName, _tenant.Id);
            return false;
        }
    }

    public async Task<bool> DisableFeatureAsync(string featureName)
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            await _tenantRepository.DisableFeatureAsync(_tenant.Id, featureName);
            await LogSecurityEventAsync(SecurityEventType.ConfigurationChanged, $"Feature {featureName} disabled");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable feature {FeatureName} for tenant {TenantId}", featureName, _tenant.Id);
            return false;
        }
    }

    public async Task<bool> HasFeatureAsync(string featureName)
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            return await _tenantRepository.HasFeatureAsync(_tenant.Id, featureName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check feature {FeatureName} for tenant {TenantId}", featureName, _tenant.Id);
            return false;
        }
    }

    public async Task<string[]> GetEnabledFeaturesAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return Array.Empty<string>();

        try
        {
            var features = await _tenantRepository.GetTenantFeaturesAsync(_tenant.Id);
            return features.Where(f => f.IsEnabled).Select(f => f.FeatureName).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enabled features for tenant {TenantId}", _tenant.Id);
            return Array.Empty<string>();
        }
    }

    public async Task<int> GetUserCountAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return 0;

        try
        {
            return await _userRepository.GetUserCountAsync(_tenant.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user count for tenant {TenantId}", _tenant.Id);
            return 0;
        }
    }

    public async Task<int> GetActiveUserCountAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return 0;

        try
        {
            var users = await _userRepository.GetActiveUsersAsync(_tenant.Id);
            return users.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active user count for tenant {TenantId}", _tenant.Id);
            return 0;
        }
    }

    public async Task<bool> IsWithinUserLimitAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            var userCount = await GetUserCountAsync();
            return userCount < _tenant.Settings.MaxUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check user limit for tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<bool> IsWithinStorageLimitAsync(long currentStorageBytes)
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            var storageLimitBytes = (long)_tenant.Settings.StorageQuotaGB * 1024 * 1024 * 1024;
            return currentStorageBytes < storageLimitBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check storage limit for tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<bool> CanCreateUserAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        // Check if tenant is active
        if (!_tenant.IsActive) return false;

        // Check subscription status
        if (!await IsSubscriptionActiveAsync()) return false;

        // Check user limit
        if (!await IsWithinUserLimitAsync()) return false;

        return true;
    }

    public async Task<bool> ValidateAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        // Check if tenant is active
        if (!_tenant.IsActive) return false;

        // Check subscription status
        if (!await IsSubscriptionActiveAsync()) return false;

        return true;
    }

    public async Task<TenantMetrics> GetMetricsAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return new TenantMetrics();

        // Return cached metrics if still fresh
        if (_cachedMetrics != null && DateTime.UtcNow - _metricsLastUpdated < TimeSpan.FromMinutes(5))
        {
            return _cachedMetrics;
        }

        try
        {
            var metrics = new TenantMetrics
            {
                TenantId = _tenant.Id,
                TenantName = _tenant.Name,
                IsActive = _tenant.IsActive,
                UserCount = await GetUserCountAsync(),
                ActiveUserCount = await GetActiveUserCountAsync(),
                MaxUsers = _tenant.Settings.MaxUsers,
                StorageQuotaGB = _tenant.Settings.StorageQuotaGB,
                SubscriptionPlan = _tenant.Subscription.PlanName,
                SubscriptionActive = await IsSubscriptionActiveAsync(),
                TrialActive = await IsTrialActiveAsync(),
                TrialExpiration = await GetTrialExpirationAsync(),
                CreatedAt = _tenant.CreatedAt,
                LastUpdated = DateTime.UtcNow
            };

            _cachedMetrics = metrics;
            _metricsLastUpdated = DateTime.UtcNow;
            
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for tenant {TenantId}", _tenant.Id);
            return new TenantMetrics();
        }
    }

    public async Task<bool> UpdateUsageAsync(long storageBytes, int userCount)
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            // Update cached metrics
            if (_cachedMetrics != null)
            {
                _cachedMetrics.StorageUsageBytes = storageBytes;
                _cachedMetrics.UserCount = userCount;
                _cachedMetrics.LastUpdated = DateTime.UtcNow;
            }

            // Check limits
            if (!await IsWithinStorageLimitAsync(storageBytes))
            {
                await LogSecurityEventAsync(SecurityEventType.SuspiciousActivity, "Storage limit exceeded");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update usage for tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<bool> IsSubscriptionActiveAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        var subscription = _tenant.Subscription;
        
        // Check if subscription is active
        if (!subscription.IsActive) return false;

        // Check if subscription has expired
        if (subscription.SubscriptionEnd.HasValue && subscription.SubscriptionEnd < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> IsTrialActiveAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        var subscription = _tenant.Subscription;
        
        return subscription.IsTrialActive && 
               subscription.TrialEnd.HasValue && 
               subscription.TrialEnd > DateTime.UtcNow;
    }

    public async Task<DateTime?> GetTrialExpirationAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return null;

        return _tenant.Subscription.TrialEnd;
    }

    public async Task<bool> ExtendTrialAsync(DateTime newExpirationDate)
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            _tenant.Subscription.TrialEnd = newExpirationDate;
            _tenant.Subscription.IsTrialActive = true;
            _tenant.UpdatedAt = DateTime.UtcNow;
            
            await _tenantRepository.UpdateSubscriptionAsync(_tenant.Id, _tenant.Subscription);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.TenantUpdated, $"Trial extended to {newExpirationDate}");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extend trial for tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<bool> UpgradeSubscriptionAsync(string newPlanId, decimal newPrice)
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            _tenant.Subscription.PlanId = newPlanId;
            _tenant.Subscription.MonthlyPrice = newPrice;
            _tenant.Subscription.IsTrialActive = false;
            _tenant.Subscription.SubscriptionStart = DateTime.UtcNow;
            _tenant.UpdatedAt = DateTime.UtcNow;
            
            await _tenantRepository.UpdateSubscriptionAsync(_tenant.Id, _tenant.Subscription);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.TenantUpdated, $"Subscription upgraded to {newPlanId}");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade subscription for tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<bool> CancelSubscriptionAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return false;

        try
        {
            _tenant.Subscription.IsActive = false;
            _tenant.Subscription.SubscriptionEnd = DateTime.UtcNow;
            _tenant.UpdatedAt = DateTime.UtcNow;
            
            await _tenantRepository.UpdateSubscriptionAsync(_tenant.Id, _tenant.Subscription);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.TenantUpdated, "Subscription canceled");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription for tenant {TenantId}", _tenant.Id);
            return false;
        }
    }

    public async Task<SecurityMetrics> GetSecurityMetricsAsync()
    {
        await EnsureLoadedAsync();
        
        if (_tenant == null) return new SecurityMetrics();

        try
        {
            return await _securityEventRepository.GetMetricsAsync(_tenant.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security metrics for tenant {TenantId}", _tenant.Id);
            return new SecurityMetrics();
        }
    }

    private async Task LoadTenantAsync()
    {
        if (_isLoaded) return;

        try
        {
            var tenantId = this.GetPrimaryKeyString();
            
            // Try to load from grain state first
            if (_tenantState.State.Tenant != null)
            {
                _tenant = _tenantState.State.Tenant;
            }
            else
            {
                // Load from repository
                _tenant = await _tenantRepository.GetByIdAsync(tenantId);
                if (_tenant != null)
                {
                    _tenantState.State.Tenant = _tenant;
                    _tenantState.State.LastModified = DateTime.UtcNow;
                    await _tenantState.WriteStateAsync();
                }
            }

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tenant {TenantId}", this.GetPrimaryKeyString());
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (!_isLoaded)
        {
            await LoadTenantAsync();
        }
    }

    private async Task UpdateStateAsync()
    {
        if (_tenant != null)
        {
            _tenantState.State.Tenant = _tenant;
            _tenantState.State.LastModified = DateTime.UtcNow;
            await _tenantState.WriteStateAsync();
        }
    }

    private async Task LogSecurityEventAsync(SecurityEventType eventType, string reason)
    {
        if (_tenant != null)
        {
            await _auditService.LogEventAsync(new SecurityEvent
            {
                Type = eventType,
                TenantId = _tenant.Id,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private async Task EndAllTenantSessionsAsync()
    {
        if (_tenant != null)
        {
            var users = await _userRepository.GetByTenantIdAsync(_tenant.Id);
            foreach (var user in users)
            {
                var userGrain = GrainFactory.GetGrain<IUserGrain>(user.Id);
                await userGrain.EndAllSessionsAsync();
            }
        }
    }

    private async Task CheckDeactivation(object _)
    {
        if (DateTime.UtcNow - _lastAccessed > TimeSpan.FromHours(2))
        {
            this.DeactivateOnIdle();
        }
    }

    private async Task RefreshMetrics(object _)
    {
        if (_tenant != null)
        {
            // Refresh metrics cache
            _cachedMetrics = null;
            await GetMetricsAsync();
        }
    }
}

/// <summary>
/// Tenant grain state for persistence
/// </summary>
[GenerateSerializer]
public class TenantState
{
    [Id(0)]
    public Tenant? Tenant { get; set; }
    
    [Id(1)]
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Tenant metrics for monitoring
/// </summary>
[GenerateSerializer]
public class TenantMetrics
{
    [Id(0)]
    public string TenantId { get; set; } = string.Empty;
    
    [Id(1)]
    public string TenantName { get; set; } = string.Empty;
    
    [Id(2)]
    public bool IsActive { get; set; }
    
    [Id(3)]
    public int UserCount { get; set; }
    
    [Id(4)]
    public int ActiveUserCount { get; set; }
    
    [Id(5)]
    public int MaxUsers { get; set; }
    
    [Id(6)]
    public long StorageUsageBytes { get; set; }
    
    [Id(7)]
    public int StorageQuotaGB { get; set; }
    
    [Id(8)]
    public string SubscriptionPlan { get; set; } = string.Empty;
    
    [Id(9)]
    public bool SubscriptionActive { get; set; }
    
    [Id(10)]
    public bool TrialActive { get; set; }
    
    [Id(11)]
    public DateTime? TrialExpiration { get; set; }
    
    [Id(12)]
    public DateTime CreatedAt { get; set; }
    
    [Id(13)]
    public DateTime LastUpdated { get; set; }
}