using BizCore.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Ardalis.SmartEnum;

namespace BizCore.Identity.Domain.Entities;

public class User : IdentityUser<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastPasswordChangedAt { get; set; }
    public bool IsSystemUser { get; set; }
    public bool RequirePasswordChange { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public List<string>? TwoFactorRecoveryCodes { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }
    public UserPreferences Preferences { get; set; } = new();
    
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    
    private readonly List<UserPermission> _userPermissions = new();
    public IReadOnlyCollection<UserPermission> UserPermissions => _userPermissions.AsReadOnly();
    
    private readonly List<UserLoginHistory> _loginHistory = new();
    public IReadOnlyCollection<UserLoginHistory> LoginHistory => _loginHistory.AsReadOnly();

    public User()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        SecurityStamp = Guid.NewGuid().ToString();
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsActive => Status == UserStatus.Active;
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    public void AssignRole(Guid roleId, Guid assignedBy)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId))
            return;

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = Id,
            RoleId = roleId,
            AssignedBy = assignedBy,
            AssignedAt = DateTime.UtcNow
        };
        
        _userRoles.Add(userRole);
    }

    public void RemoveRole(Guid roleId)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole != null)
        {
            _userRoles.Remove(userRole);
        }
    }

    public void GrantPermission(string permission, Guid grantedBy)
    {
        if (_userPermissions.Any(up => up.Permission == permission))
            return;

        var userPermission = new UserPermission
        {
            Id = Guid.NewGuid(),
            UserId = Id,
            Permission = permission,
            GrantedBy = grantedBy,
            GrantedAt = DateTime.UtcNow
        };
        
        _userPermissions.Add(userPermission);
    }

    public void RevokePermission(string permission)
    {
        var userPermission = _userPermissions.FirstOrDefault(up => up.Permission == permission);
        if (userPermission != null)
        {
            _userPermissions.Remove(userPermission);
        }
    }

    public void RecordLogin(string ipAddress, string userAgent)
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        
        var loginHistory = new UserLoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = Id,
            LoginAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuccessful = true
        };
        
        _loginHistory.Add(loginHistory);
        
        // Keep only last 100 login records
        if (_loginHistory.Count > 100)
        {
            var oldestRecords = _loginHistory.OrderBy(lh => lh.LoginAt).Take(_loginHistory.Count - 100);
            foreach (var record in oldestRecords)
            {
                _loginHistory.Remove(record);
            }
        }
    }

    public void RecordFailedLogin(string ipAddress, string userAgent)
    {
        FailedLoginAttempts++;
        
        var loginHistory = new UserLoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = Id,
            LoginAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuccessful = false
        };
        
        _loginHistory.Add(loginHistory);
        
        // Lock account after 5 failed attempts
        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(30);
        }
    }

    public void Unlock()
    {
        LockedUntil = null;
        FailedLoginAttempts = 0;
    }

    public void SetTwoFactorSecret(string secret)
    {
        TwoFactorSecret = secret;
        IsTwoFactorEnabled = true;
    }

    public void DisableTwoFactor()
    {
        TwoFactorSecret = null;
        IsTwoFactorEnabled = false;
        TwoFactorRecoveryCodes = null;
    }

    public void SetRefreshToken(string token, DateTime expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
    }

    public void UpdatePreferences(UserPreferences preferences)
    {
        Preferences = preferences;
    }

    public void Activate() => Status = UserStatus.Active;
    public void Deactivate() => Status = UserStatus.Inactive;
    public void Suspend() => Status = UserStatus.Suspended;
}

public class Role : IdentityRole<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public Role()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void GrantPermission(string permission, Guid grantedBy)
    {
        if (_rolePermissions.Any(rp => rp.Permission == permission))
            return;

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = Id,
            Permission = permission,
            GrantedBy = grantedBy,
            GrantedAt = DateTime.UtcNow
        };
        
        _rolePermissions.Add(rolePermission);
    }

    public void RevokePermission(string permission)
    {
        var rolePermission = _rolePermissions.FirstOrDefault(rp => rp.Permission == permission);
        if (rolePermission != null)
        {
            _rolePermissions.Remove(rolePermission);
        }
    }
}

public class UserRole : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
    
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public class UserPermission : Entity<Guid>
{
    public Guid UserId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public Guid GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }
    
    public User User { get; set; } = null!;
}

public class RolePermission : Entity<Guid>
{
    public Guid RoleId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public Guid GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }
    
    public Role Role { get; set; } = null!;
}

public class UserLoginHistory : Entity<Guid>
{
    public Guid UserId { get; set; }
    public DateTime LoginAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    
    public User User { get; set; } = null!;
}

public class UserPreferences : ValueObject
{
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC";
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string NumberFormat { get; set; } = "en-US";
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public Dictionary<string, object> CustomSettings { get; set; } = new();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Theme;
        yield return Language;
        yield return TimeZone;
        yield return DateFormat;
        yield return NumberFormat;
        yield return EmailNotifications;
        yield return PushNotifications;
        yield return SmsNotifications;
        yield return CustomSettings;
    }
}

public class UserStatus : SmartEnum<UserStatus>
{
    public static readonly UserStatus Active = new(1, nameof(Active));
    public static readonly UserStatus Inactive = new(2, nameof(Inactive));
    public static readonly UserStatus Suspended = new(3, nameof(Suspended));
    public static readonly UserStatus PendingActivation = new(4, nameof(PendingActivation));

    private UserStatus(int value, string name) : base(name, value) { }
}

// Domain Events
public record UserCreatedDomainEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    string FullName) : INotification;

public record UserLoginSuccessfulDomainEvent(
    Guid UserId,
    Guid TenantId,
    DateTime LoginAt,
    string IpAddress) : INotification;

public record UserLoginFailedDomainEvent(
    Guid UserId,
    Guid TenantId,
    DateTime AttemptAt,
    string IpAddress,
    string Reason) : INotification;

public record UserLockedDomainEvent(
    Guid UserId,
    Guid TenantId,
    DateTime LockedUntil,
    string Reason) : INotification;

public record UserRoleAssignedDomainEvent(
    Guid UserId,
    Guid RoleId,
    Guid TenantId,
    Guid AssignedBy) : INotification;