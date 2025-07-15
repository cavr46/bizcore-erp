namespace BizCore.ApiGateway.Services;

public interface ITenantService
{
    Task<TenantInfo?> GetTenantAsync(string tenantId);
    Task<bool> ValidateTenantAsync(string tenantId);
    Task<List<TenantInfo>> GetTenantsAsync();
}

public class TenantService : ITenantService
{
    private readonly ILogger<TenantService> _logger;
    // In a real implementation, this would use a database or cache
    private readonly Dictionary<string, TenantInfo> _tenants = new()
    {
        { "demo", new TenantInfo { Id = "demo", Name = "Demo Company", IsActive = true } },
        { "acme", new TenantInfo { Id = "acme", Name = "ACME Corp", IsActive = true } },
        { "contoso", new TenantInfo { Id = "contoso", Name = "Contoso Ltd", IsActive = true } }
    };

    public TenantService(ILogger<TenantService> logger)
    {
        _logger = logger;
    }

    public Task<TenantInfo?> GetTenantAsync(string tenantId)
    {
        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<bool> ValidateTenantAsync(string tenantId)
    {
        var tenant = _tenants.GetValueOrDefault(tenantId);
        return Task.FromResult(tenant != null && tenant.IsActive);
    }

    public Task<List<TenantInfo>> GetTenantsAsync()
    {
        return Task.FromResult(_tenants.Values.ToList());
    }
}

public class TenantInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Settings { get; set; } = new();
}