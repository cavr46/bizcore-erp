using BizCore.Identity.Models;

namespace BizCore.Identity.Repositories;

/// <summary>
/// Repository interface for user management
/// </summary>
public interface IUserRepository
{
    // Basic CRUD operations
    Task<User> CreateAsync(User user);
    Task<User?> GetByIdAsync(string userId);
    Task<User?> GetByEmailAsync(string email, string? tenantId = null);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(string userId);
    Task<IEnumerable<User>> GetByTenantIdAsync(string tenantId);
    Task<bool> ExistsAsync(string userId);

    // Authentication related
    Task<bool> ValidateCredentialsAsync(string email, string password, string? tenantId = null);
    Task<bool> UpdatePasswordAsync(string userId, string passwordHash);
    Task<bool> UpdateLastLoginAsync(string userId, DateTime lastLogin);
    Task<bool> IncrementFailedLoginAttemptsAsync(string userId);
    Task<bool> ResetFailedLoginAttemptsAsync(string userId);
    Task<bool> LockUserAsync(string userId, DateTime lockoutEnd);
    Task<bool> UnlockUserAsync(string userId);

    // Role management
    Task<IEnumerable<Role>> GetUserRolesAsync(string userId);
    Task<bool> AddUserRoleAsync(string userId, string roleId);
    Task<bool> RemoveUserRoleAsync(string userId, string roleId);
    Task<bool> HasRoleAsync(string userId, string roleId);

    // Permission management
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId);
    Task<bool> AddUserPermissionAsync(string userId, string permissionId);
    Task<bool> RemoveUserPermissionAsync(string userId, string permissionId);
    Task<bool> HasPermissionAsync(string userId, string permissionId);

    // Session management
    Task<UserSession> CreateSessionAsync(UserSession session);
    Task<IEnumerable<UserSession>> GetUserSessionsAsync(string userId);
    Task<bool> EndSessionAsync(string sessionId);
    Task<bool> EndAllUserSessionsAsync(string userId);
    Task<UserSession?> GetSessionByIdAsync(string sessionId);
    Task<bool> UpdateSessionActivityAsync(string sessionId, DateTime lastActivity);

    // Email confirmation
    Task<bool> ConfirmEmailAsync(string userId);
    Task<EmailConfirmationToken> CreateEmailConfirmationTokenAsync(EmailConfirmationToken token);
    Task<IEnumerable<EmailConfirmationToken>> GetEmailConfirmationTokensAsync();
    Task<bool> MarkEmailConfirmationTokenAsUsedAsync(string tokenId);

    // Password reset
    Task<PasswordResetToken> CreatePasswordResetTokenAsync(PasswordResetToken token);
    Task<IEnumerable<PasswordResetToken>> GetPasswordResetTokensAsync();
    Task<bool> MarkPasswordResetTokenAsUsedAsync(string tokenId);

    // MFA management
    Task<bool> UpdateMfaSecretAsync(string userId, string secret);
    Task<bool> EnableMfaAsync(string userId);
    Task<bool> DisableMfaAsync(string userId);
    Task<bool> UpdateMfaBackupCodesAsync(string userId, string[] backupCodes);

    // Profile management
    Task<bool> UpdateProfileAsync(string userId, string firstName, string lastName, string? phoneNumber = null, string? profilePictureUrl = null);
    Task<bool> UpdateUserPreferencesAsync(string userId, string? timeZone = null, string? language = null, Dictionary<string, object>? metadata = null);

    // Query operations
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, string? tenantId = null, int skip = 0, int take = 50);
    Task<int> GetUserCountAsync(string? tenantId = null);
    Task<IEnumerable<User>> GetActiveUsersAsync(string? tenantId = null);
    Task<IEnumerable<User>> GetInactiveUsersAsync(string? tenantId = null);
    Task<IEnumerable<User>> GetLockedUsersAsync(string? tenantId = null);
    Task<IEnumerable<User>> GetUsersWithRoleAsync(string roleId);
    Task<IEnumerable<User>> GetUsersWithPermissionAsync(string permissionId);
    Task<IEnumerable<User>> GetUsersCreatedAfterAsync(DateTime date, string? tenantId = null);
    Task<IEnumerable<User>> GetUsersLastLoginBeforeAsync(DateTime date, string? tenantId = null);
}

/// <summary>
/// Repository interface for tenant management
/// </summary>
public interface ITenantRepository
{
    // Basic CRUD operations
    Task<Tenant> CreateAsync(Tenant tenant);
    Task<Tenant?> GetByIdAsync(string tenantId);
    Task<Tenant?> GetByDomainAsync(string domain);
    Task<Tenant?> GetByNameAsync(string name);
    Task<Tenant> UpdateAsync(Tenant tenant);
    Task<bool> DeleteAsync(string tenantId);
    Task<bool> ExistsAsync(string tenantId);

    // Tenant management
    Task<bool> ActivateAsync(string tenantId);
    Task<bool> DeactivateAsync(string tenantId);
    Task<bool> UpdateSettingsAsync(string tenantId, TenantSettings settings);
    Task<bool> UpdateSubscriptionAsync(string tenantId, TenantSubscription subscription);

    // Feature management
    Task<IEnumerable<TenantFeature>> GetTenantFeaturesAsync(string tenantId);
    Task<bool> EnableFeatureAsync(string tenantId, string featureName);
    Task<bool> DisableFeatureAsync(string tenantId, string featureName);
    Task<bool> HasFeatureAsync(string tenantId, string featureName);

    // Query operations
    Task<IEnumerable<Tenant>> GetAllAsync();
    Task<IEnumerable<Tenant>> GetActiveTenantsAsync();
    Task<IEnumerable<Tenant>> GetInactiveTenantsAsync();
    Task<IEnumerable<Tenant>> SearchTenantsAsync(string searchTerm, int skip = 0, int take = 50);
    Task<int> GetTenantCountAsync();
    Task<IEnumerable<Tenant>> GetTenantsCreatedAfterAsync(DateTime date);
    Task<IEnumerable<Tenant>> GetTenantsWithSubscriptionAsync(string planId);
    Task<IEnumerable<Tenant>> GetTenantsExpiringBeforeAsync(DateTime date);
}

/// <summary>
/// Repository interface for role management
/// </summary>
public interface IRoleRepository
{
    // Basic CRUD operations
    Task<Role> CreateAsync(Role role);
    Task<Role?> GetByIdAsync(string roleId);
    Task<Role?> GetByNameAsync(string name, string? tenantId = null);
    Task<Role> UpdateAsync(Role role);
    Task<bool> DeleteAsync(string roleId);
    Task<bool> ExistsAsync(string roleId);

    // Permission management
    Task<IEnumerable<Permission>> GetRolePermissionsAsync(string roleId);
    Task<bool> AddRolePermissionAsync(string roleId, string permissionId);
    Task<bool> RemoveRolePermissionAsync(string roleId, string permissionId);
    Task<bool> HasPermissionAsync(string roleId, string permissionId);

    // Hierarchy management
    Task<IEnumerable<Role>> GetChildRolesAsync(string parentRoleId);
    Task<Role?> GetParentRoleAsync(string roleId);
    Task<bool> SetParentRoleAsync(string roleId, string? parentRoleId);

    // Query operations
    Task<IEnumerable<Role>> GetByTenantIdAsync(string tenantId);
    Task<IEnumerable<Role>> GetSystemRolesAsync();
    Task<IEnumerable<Role>> GetActiveRolesAsync(string? tenantId = null);
    Task<IEnumerable<Role>> SearchRolesAsync(string searchTerm, string? tenantId = null, int skip = 0, int take = 50);
    Task<int> GetRoleCountAsync(string? tenantId = null);
    Task<IEnumerable<Role>> GetRolesWithPermissionAsync(string permissionId);
    Task<IEnumerable<Role>> GetRolesCreatedAfterAsync(DateTime date, string? tenantId = null);
}

/// <summary>
/// Repository interface for permission management
/// </summary>
public interface IPermissionRepository
{
    // Basic CRUD operations
    Task<Permission> CreateAsync(Permission permission);
    Task<Permission?> GetByIdAsync(string permissionId);
    Task<Permission?> GetByNameAsync(string name);
    Task<Permission> UpdateAsync(Permission permission);
    Task<bool> DeleteAsync(string permissionId);
    Task<bool> ExistsAsync(string permissionId);

    // User permissions
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId);
    Task<bool> AddUserPermissionAsync(string userId, string permissionId);
    Task<bool> RemoveUserPermissionAsync(string userId, string permissionId);
    Task<bool> UserHasPermissionAsync(string userId, string permissionId);

    // Query operations
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<IEnumerable<Permission>> GetByModuleAsync(string module);
    Task<IEnumerable<Permission>> GetByCategoryAsync(string category);
    Task<IEnumerable<Permission>> GetSystemPermissionsAsync();
    Task<IEnumerable<Permission>> SearchPermissionsAsync(string searchTerm, int skip = 0, int take = 50);
    Task<int> GetPermissionCountAsync();
    Task<IEnumerable<Permission>> GetPermissionsCreatedAfterAsync(DateTime date);
}

/// <summary>
/// Repository interface for session management
/// </summary>
public interface ISessionRepository
{
    // Basic CRUD operations
    Task<UserSession> CreateAsync(UserSession session);
    Task<UserSession?> GetByIdAsync(string sessionId);
    Task<UserSession> UpdateAsync(UserSession session);
    Task<bool> DeleteAsync(string sessionId);
    Task<bool> ExistsAsync(string sessionId);

    // Session management
    Task<IEnumerable<UserSession>> GetByUserIdAsync(string userId);
    Task<IEnumerable<UserSession>> GetByTenantIdAsync(string tenantId);
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync();
    Task<IEnumerable<UserSession>> GetExpiredSessionsAsync();
    Task<bool> EndSessionAsync(string sessionId);
    Task<bool> EndAllUserSessionsAsync(string userId);
    Task<bool> UpdateLastActivityAsync(string sessionId, DateTime lastActivity);

    // Query operations
    Task<int> GetActiveSessionCountAsync();
    Task<int> GetUserSessionCountAsync(string userId);
    Task<IEnumerable<UserSession>> GetSessionsCreatedAfterAsync(DateTime date);
    Task<IEnumerable<UserSession>> GetSessionsLastActiveBeforeAsync(DateTime date);
    Task<IEnumerable<UserSession>> GetSessionsByIpAddressAsync(string ipAddress);
    Task<IEnumerable<UserSession>> GetSessionsByUserAgentAsync(string userAgent);

    // Cleanup operations
    Task<int> CleanupExpiredSessionsAsync();
    Task<int> CleanupOldSessionsAsync(DateTime cutoffDate);
}

/// <summary>
/// Repository interface for security event management
/// </summary>
public interface ISecurityEventRepository
{
    // Basic CRUD operations
    Task<SecurityEvent> CreateAsync(SecurityEvent securityEvent);
    Task<SecurityEvent?> GetByIdAsync(string eventId);
    Task<bool> DeleteAsync(string eventId);
    Task<bool> ExistsAsync(string eventId);

    // Query operations
    Task<IEnumerable<SecurityEvent>> GetEventsAsync(string? userId = null, string? tenantId = null, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<SecurityEvent>> GetEventsByTypeAsync(SecurityEventType type, string? userId = null, string? tenantId = null, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<SecurityEvent>> GetEventsByUserAsync(string userId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<SecurityEvent>> GetEventsByTenantAsync(string tenantId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<SecurityEvent>> GetEventsByIpAddressAsync(string ipAddress, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<SecurityEvent>> GetRecentEventsAsync(int count = 100);

    // Analytics operations
    Task<SecurityMetrics> GetMetricsAsync(string? tenantId = null);
    Task<Dictionary<SecurityEventType, int>> GetEventCountsByTypeAsync(string? tenantId = null, DateTime? from = null, DateTime? to = null);
    Task<Dictionary<string, int>> GetEventCountsByUserAsync(string? tenantId = null, DateTime? from = null, DateTime? to = null);
    Task<Dictionary<string, int>> GetEventCountsByIpAddressAsync(string? tenantId = null, DateTime? from = null, DateTime? to = null);
    Task<Dictionary<DateTime, int>> GetEventCountsByDateAsync(string? tenantId = null, DateTime? from = null, DateTime? to = null);

    // Cleanup operations
    Task<int> CleanupOldEventsAsync(DateTime cutoffDate);
    Task<int> GetEventCountAsync(string? tenantId = null, DateTime? from = null, DateTime? to = null);
}

/// <summary>
/// Repository interface for SSO provider management
/// </summary>
public interface ISsoProviderRepository
{
    // Basic CRUD operations
    Task<SsoProvider> CreateAsync(SsoProvider provider);
    Task<SsoProvider?> GetByIdAsync(string providerId);
    Task<SsoProvider?> GetByNameAsync(string name, string tenantId);
    Task<SsoProvider> UpdateAsync(SsoProvider provider);
    Task<bool> DeleteAsync(string providerId);
    Task<bool> ExistsAsync(string providerId);

    // Query operations
    Task<IEnumerable<SsoProvider>> GetByTenantIdAsync(string tenantId);
    Task<IEnumerable<SsoProvider>> GetByTypeAsync(string type);
    Task<IEnumerable<SsoProvider>> GetActiveProvidersAsync(string? tenantId = null);
    Task<IEnumerable<SsoProvider>> GetInactiveProvidersAsync(string? tenantId = null);
    Task<int> GetProviderCountAsync(string? tenantId = null);
}

/// <summary>
/// Repository interface for API key management
/// </summary>
public interface IApiKeyRepository
{
    // Basic CRUD operations
    Task<ApiKey> CreateAsync(ApiKey apiKey);
    Task<ApiKey?> GetByIdAsync(string apiKeyId);
    Task<ApiKey?> GetByKeyHashAsync(string keyHash);
    Task<ApiKey> UpdateAsync(ApiKey apiKey);
    Task<bool> DeleteAsync(string apiKeyId);
    Task<bool> ExistsAsync(string apiKeyId);

    // Key management
    Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId);
    Task<IEnumerable<ApiKey>> GetByTenantIdAsync(string tenantId);
    Task<IEnumerable<ApiKey>> GetActiveKeysAsync(string? userId = null, string? tenantId = null);
    Task<IEnumerable<ApiKey>> GetExpiredKeysAsync();
    Task<bool> UpdateLastUsedAsync(string apiKeyId, DateTime lastUsed);

    // Query operations
    Task<int> GetKeyCountAsync(string? userId = null, string? tenantId = null);
    Task<IEnumerable<ApiKey>> GetKeysCreatedAfterAsync(DateTime date, string? userId = null, string? tenantId = null);
    Task<IEnumerable<ApiKey>> GetKeysLastUsedBeforeAsync(DateTime date);
    Task<IEnumerable<ApiKey>> GetKeysExpiringBeforeAsync(DateTime date);

    // Cleanup operations
    Task<int> CleanupExpiredKeysAsync();
    Task<int> CleanupOldKeysAsync(DateTime cutoffDate);
}