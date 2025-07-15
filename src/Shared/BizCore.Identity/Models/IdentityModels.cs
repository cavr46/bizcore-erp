namespace BizCore.Identity.Models;

/// <summary>
/// User entity with multi-tenant support
/// </summary>
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; } = false;
    public DateTime? EmailConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }
    public bool MfaEnabled { get; set; } = false;
    public string? MfaSecret { get; set; }
    public string[] MfaBackupCodes { get; set; } = Array.Empty<string>();
    public string? ProfilePictureUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; } = false;
    public string? TimeZone { get; set; }
    public string? Language { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Navigation properties
    public virtual Tenant Tenant { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
    public bool RequiresPasswordChange => DateTime.UtcNow - UpdatedAt > TimeSpan.FromDays(90);
}

/// <summary>
/// Tenant entity for multi-tenancy
/// </summary>
public class Tenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public TenantSettings Settings { get; set; } = new();
    public TenantSubscription Subscription { get; set; } = new();

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    public virtual ICollection<TenantFeature> Features { get; set; } = new List<TenantFeature>();
}

/// <summary>
/// Tenant settings
/// </summary>
public class TenantSettings
{
    public int MaxUsers { get; set; } = 100;
    public int StorageQuotaGB { get; set; } = 10;
    public string[] Features { get; set; } = Array.Empty<string>();
    public bool AllowUserRegistration { get; set; } = false;
    public bool RequireEmailConfirmation { get; set; } = true;
    public bool RequireMfa { get; set; } = false;
    public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours
    public int PasswordExpirationDays { get; set; } = 90;
    public bool EnableAuditLog { get; set; } = true;
    public bool EnableSso { get; set; } = false;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Tenant subscription information
/// </summary>
public class TenantSubscription
{
    public string PlanId { get; set; } = "free";
    public string PlanName { get; set; } = "Free";
    public decimal MonthlyPrice { get; set; } = 0;
    public DateTime? SubscriptionStart { get; set; }
    public DateTime? SubscriptionEnd { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTrialActive { get; set; } = false;
    public DateTime? TrialEnd { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
}

/// <summary>
/// Role entity with hierarchical support
/// </summary>
public class Role
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public bool IsSystem { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? ParentRoleId { get; set; }
    public int Priority { get; set; } = 0;
    public string? Color { get; set; }
    public string? Icon { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; }
    public virtual Role? ParentRole { get; set; }
    public virtual ICollection<Role> ChildRoles { get; set; } = new List<Role>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Permission entity
/// </summary>
public class Permission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsSystem { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

/// <summary>
/// User-Role relationship
/// </summary>
public class UserRole
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; }
    public virtual Role Role { get; set; }
}

/// <summary>
/// Role-Permission relationship
/// </summary>
public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public string PermissionId { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedBy { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Role Role { get; set; }
    public virtual Permission Permission { get; set; }
}

/// <summary>
/// User-Permission relationship (direct permissions)
/// </summary>
public class UserPermission
{
    public string UserId { get; set; } = string.Empty;
    public string PermissionId { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; }
    public virtual Permission Permission { get; set; }
}

/// <summary>
/// User session tracking
/// </summary>
public class UserSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpires { get; set; }
    public bool IsImpersonated { get; set; } = false;
    public string? ImpersonatedBy { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Navigation properties
    public virtual User User { get; set; }
    public virtual Tenant Tenant { get; set; }

    // Computed properties
    public bool IsExpired => EndedAt.HasValue || (DateTime.UtcNow - LastActivityAt).TotalMinutes > 480;
}

/// <summary>
/// Security audit events
/// </summary>
public class SecurityEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public SecurityEventType Type { get; set; }
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public string? Email { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Tenant? Tenant { get; set; }
}

/// <summary>
/// Security event types
/// </summary>
public enum SecurityEventType
{
    LoginSuccess,
    LoginFailed,
    Logout,
    PasswordChanged,
    PasswordReset,
    MfaEnabled,
    MfaDisabled,
    MfaCodeGenerated,
    MfaCodeValidated,
    UserCreated,
    UserUpdated,
    UserDeleted,
    UserLocked,
    UserUnlocked,
    RoleAssigned,
    RoleRemoved,
    PermissionGranted,
    PermissionRevoked,
    TenantCreated,
    TenantUpdated,
    TenantDeleted,
    SessionCreated,
    SessionEnded,
    SuspiciousActivity,
    RateLimitExceeded,
    UnauthorizedAccess,
    DataExport,
    DataImport,
    ConfigurationChanged,
    ImpersonationStarted,
    ImpersonationEnded
}

/// <summary>
/// Multi-factor authentication types
/// </summary>
public enum MfaType
{
    None,
    Totp,
    Sms,
    Email,
    App
}

/// <summary>
/// SSO provider configuration
/// </summary>
public class SsoProvider
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // SAML, OIDC, OAuth2
    public string TenantId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Configuration { get; set; } = new();

    // Navigation properties
    public virtual Tenant Tenant { get; set; }
}

/// <summary>
/// Tenant feature configuration
/// </summary>
public class TenantFeature
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime? EnabledAt { get; set; }
    public DateTime? DisabledAt { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();

    // Navigation properties
    public virtual Tenant Tenant { get; set; }
}

/// <summary>
/// Security metrics for dashboard
/// </summary>
public class SecurityMetrics
{
    public string TenantId { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int SuccessfulLogins { get; set; }
    public int SuspiciousActivities { get; set; }
    public int ActiveSessions { get; set; }
    public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, int> EventCounts { get; set; } = new();
}

/// <summary>
/// Password reset token
/// </summary>
public class PasswordResetToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public string? IpAddress { get; set; }

    // Navigation properties
    public virtual User User { get; set; }

    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;
}

/// <summary>
/// Email confirmation token
/// </summary>
public class EmailConfirmationToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; }

    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;
}

/// <summary>
/// API key for programmatic access
/// </summary>
public class ApiKey
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Navigation properties
    public virtual User User { get; set; }
    public virtual Tenant Tenant { get; set; }

    // Computed properties
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt;
    public bool IsValid => IsActive && !IsExpired;
}

/// <summary>
/// Identity configuration
/// </summary>
public class IdentityConfiguration
{
    public int MinPasswordLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 30;
    public int SessionTimeoutMinutes { get; set; } = 480;
    public int RefreshTokenExpirationDays { get; set; } = 30;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int PasswordResetTokenExpirationHours { get; set; } = 24;
    public int EmailConfirmationTokenExpirationHours { get; set; } = 24;
    public bool RequireEmailConfirmation { get; set; } = true;
    public bool EnableMfa { get; set; } = false;
    public bool EnableSso { get; set; } = false;
    public bool EnableAuditLog { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public int RateLimitRequests { get; set; } = 100;
    public int RateLimitWindowMinutes { get; set; } = 15;
    public string JwtSecretKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = "BizCore";
    public string JwtAudience { get; set; } = "BizCore";
}

/// <summary>
/// Session for application state
/// </summary>
public class Session
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(8);
    public bool IsActive { get; set; } = true;

    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => IsActive && !IsExpired;
}