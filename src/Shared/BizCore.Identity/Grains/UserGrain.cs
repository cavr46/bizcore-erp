using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using BizCore.Identity.Models;
using BizCore.Identity.Services;
using BizCore.Identity.Repositories;

namespace BizCore.Identity.Grains;

/// <summary>
/// User grain for distributed user management
/// </summary>
public interface IUserGrain : IGrainWithStringKey
{
    Task<User?> GetUserAsync();
    Task<bool> SetUserAsync(User user);
    Task<bool> UpdatePasswordAsync(string passwordHash);
    Task<bool> LockUserAsync(DateTime lockoutEnd);
    Task<bool> UnlockUserAsync();
    Task<bool> UpdateLastLoginAsync(DateTime lastLogin);
    Task<int> IncrementFailedLoginAttemptsAsync();
    Task<bool> ResetFailedLoginAttemptsAsync();
    Task<bool> ConfirmEmailAsync();
    Task<bool> EnableMfaAsync(string secret);
    Task<bool> DisableMfaAsync();
    Task<bool> UpdateMfaBackupCodesAsync(string[] backupCodes);
    Task<bool> UpdatePreferencesAsync(string? timeZone, string? language, Dictionary<string, object>? metadata);
    Task<bool> DeactivateAsync();
    Task<bool> ActivateAsync();
    Task<UserSecurityInfo> GetSecurityInfoAsync();
    Task<bool> HasPermissionAsync(string permission);
    Task<string[]> GetPermissionsAsync();
    Task<string[]> GetRolesAsync();
    Task<bool> AddRoleAsync(string roleId);
    Task<bool> RemoveRoleAsync(string roleId);
    Task<UserSession[]> GetActiveSessionsAsync();
    Task<bool> EndAllSessionsAsync();
    Task<bool> ValidateAsync();
}

[Reentrant]
public class UserGrain : Grain, IUserGrain
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<UserGrain> _logger;
    private readonly IPersistentState<UserState> _userState;

    private User? _user;
    private DateTime _lastAccessed;
    private bool _isLoaded;

    public UserGrain(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ISessionRepository sessionRepository,
        ISecurityAuditService auditService,
        ILogger<UserGrain> logger,
        [PersistentState("user", "userStore")] IPersistentState<UserState> userState)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _sessionRepository = sessionRepository;
        _auditService = auditService;
        _logger = logger;
        _userState = userState;
    }

    public override async Task OnActivateAsync()
    {
        await LoadUserAsync();
        _lastAccessed = DateTime.UtcNow;
        
        // Set up deactivation timer
        RegisterTimer(CheckDeactivation, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
    }

    public async Task<User?> GetUserAsync()
    {
        await EnsureLoadedAsync();
        _lastAccessed = DateTime.UtcNow;
        return _user;
    }

    public async Task<bool> SetUserAsync(User user)
    {
        try
        {
            _user = user;
            _userState.State.User = user;
            _userState.State.LastModified = DateTime.UtcNow;
            await _userState.WriteStateAsync();
            
            // Also update in repository
            await _userRepository.UpdateAsync(user);
            
            await LogSecurityEventAsync(SecurityEventType.UserUpdated, "User data updated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user {UserId}", this.GetPrimaryKeyString());
            return false;
        }
    }

    public async Task<bool> UpdatePasswordAsync(string passwordHash)
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.PasswordHash = passwordHash;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UpdatePasswordAsync(_user.Id, passwordHash);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.PasswordChanged, "Password updated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update password for user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> LockUserAsync(DateTime lockoutEnd)
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.LockoutEnd = lockoutEnd;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.LockUserAsync(_user.Id, lockoutEnd);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.UserLocked, $"User locked until {lockoutEnd}");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lock user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> UnlockUserAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.LockoutEnd = null;
            _user.FailedLoginAttempts = 0;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UnlockUserAsync(_user.Id);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.UserUnlocked, "User unlocked");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> UpdateLastLoginAsync(DateTime lastLogin)
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.LastLoginAt = lastLogin;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UpdateLastLoginAsync(_user.Id, lastLogin);
            await UpdateStateAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update last login for user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<int> IncrementFailedLoginAttemptsAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return 0;

        try
        {
            _user.FailedLoginAttempts++;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.IncrementFailedLoginAttemptsAsync(_user.Id);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.LoginFailed, $"Failed login attempt #{_user.FailedLoginAttempts}");
            
            return _user.FailedLoginAttempts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment failed login attempts for user {UserId}", _user.Id);
            return 0;
        }
    }

    public async Task<bool> ResetFailedLoginAttemptsAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.FailedLoginAttempts = 0;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.ResetFailedLoginAttemptsAsync(_user.Id);
            await UpdateStateAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset failed login attempts for user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> ConfirmEmailAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.EmailConfirmed = true;
            _user.EmailConfirmedAt = DateTime.UtcNow;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.ConfirmEmailAsync(_user.Id);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.UserUpdated, "Email confirmed");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm email for user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> EnableMfaAsync(string secret)
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.MfaEnabled = true;
            _user.MfaSecret = secret;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UpdateMfaSecretAsync(_user.Id, secret);
            await _userRepository.EnableMfaAsync(_user.Id);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.MfaEnabled, "MFA enabled");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable MFA for user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> DisableMfaAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.MfaEnabled = false;
            _user.MfaSecret = null;
            _user.MfaBackupCodes = Array.Empty<string>();
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.DisableMfaAsync(_user.Id);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.MfaDisabled, "MFA disabled");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable MFA for user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> UpdateMfaBackupCodesAsync(string[] backupCodes)
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.MfaBackupCodes = backupCodes;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UpdateMfaBackupCodesAsync(_user.Id, backupCodes);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.MfaCodeGenerated, "MFA backup codes updated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update MFA backup codes for user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> UpdatePreferencesAsync(string? timeZone, string? language, Dictionary<string, object>? metadata)
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            if (timeZone != null) _user.TimeZone = timeZone;
            if (language != null) _user.Language = language;
            if (metadata != null) _user.Metadata = metadata;
            
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UpdateUserPreferencesAsync(_user.Id, timeZone, language, metadata);
            await UpdateStateAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update preferences for user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> DeactivateAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.IsActive = false;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UpdateAsync(_user);
            await UpdateStateAsync();
            
            // End all active sessions
            await EndAllSessionsAsync();
            
            await LogSecurityEventAsync(SecurityEventType.UserDeleted, "User deactivated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> ActivateAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            _user.IsActive = true;
            _user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UpdateAsync(_user);
            await UpdateStateAsync();
            
            await LogSecurityEventAsync(SecurityEventType.UserUpdated, "User activated");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<UserSecurityInfo> GetSecurityInfoAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return new UserSecurityInfo();

        try
        {
            var sessions = await _sessionRepository.GetByUserIdAsync(_user.Id);
            var activeSessions = sessions.Where(s => s.IsActive && !s.IsExpired).ToArray();
            
            return new UserSecurityInfo
            {
                UserId = _user.Id,
                IsActive = _user.IsActive,
                IsLocked = _user.IsLocked,
                MfaEnabled = _user.MfaEnabled,
                EmailConfirmed = _user.EmailConfirmed,
                LastLoginAt = _user.LastLoginAt,
                FailedLoginAttempts = _user.FailedLoginAttempts,
                ActiveSessionCount = activeSessions.Length,
                RequiresPasswordChange = _user.RequiresPasswordChange
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security info for user {UserId}", _user.Id);
            return new UserSecurityInfo();
        }
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            // Check direct user permissions
            var userPermissions = await _permissionRepository.GetUserPermissionsAsync(_user.Id);
            if (userPermissions.Any(p => p.Name == permission))
            {
                return true;
            }

            // Check role permissions
            var userRoles = await _userRepository.GetUserRolesAsync(_user.Id);
            foreach (var role in userRoles)
            {
                var rolePermissions = await _roleRepository.GetRolePermissionsAsync(role.Id);
                if (rolePermissions.Any(p => p.Name == permission))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permission {Permission} for user {UserId}", permission, _user.Id);
            return false;
        }
    }

    public async Task<string[]> GetPermissionsAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return Array.Empty<string>();

        try
        {
            var permissions = new List<string>();
            
            // Get direct user permissions
            var userPermissions = await _permissionRepository.GetUserPermissionsAsync(_user.Id);
            permissions.AddRange(userPermissions.Select(p => p.Name));

            // Get role permissions
            var userRoles = await _userRepository.GetUserRolesAsync(_user.Id);
            foreach (var role in userRoles)
            {
                var rolePermissions = await _roleRepository.GetRolePermissionsAsync(role.Id);
                permissions.AddRange(rolePermissions.Select(p => p.Name));
            }

            return permissions.Distinct().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for user {UserId}", _user.Id);
            return Array.Empty<string>();
        }
    }

    public async Task<string[]> GetRolesAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return Array.Empty<string>();

        try
        {
            var userRoles = await _userRepository.GetUserRolesAsync(_user.Id);
            return userRoles.Select(r => r.Name).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get roles for user {UserId}", _user.Id);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> AddRoleAsync(string roleId)
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            await _userRepository.AddUserRoleAsync(_user.Id, roleId);
            await LogSecurityEventAsync(SecurityEventType.RoleAssigned, $"Role {roleId} assigned");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add role {RoleId} to user {UserId}", roleId, _user.Id);
            return false;
        }
    }

    public async Task<bool> RemoveRoleAsync(string roleId)
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            await _userRepository.RemoveUserRoleAsync(_user.Id, roleId);
            await LogSecurityEventAsync(SecurityEventType.RoleRemoved, $"Role {roleId} removed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove role {RoleId} from user {UserId}", roleId, _user.Id);
            return false;
        }
    }

    public async Task<UserSession[]> GetActiveSessionsAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return Array.Empty<UserSession>();

        try
        {
            var sessions = await _sessionRepository.GetByUserIdAsync(_user.Id);
            return sessions.Where(s => s.IsActive && !s.IsExpired).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active sessions for user {UserId}", _user.Id);
            return Array.Empty<UserSession>();
        }
    }

    public async Task<bool> EndAllSessionsAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        try
        {
            await _sessionRepository.EndAllUserSessionsAsync(_user.Id);
            await LogSecurityEventAsync(SecurityEventType.SessionEnded, "All sessions ended");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end all sessions for user {UserId}", _user.Id);
            return false;
        }
    }

    public async Task<bool> ValidateAsync()
    {
        await EnsureLoadedAsync();
        
        if (_user == null) return false;

        // Check if user is still valid
        if (!_user.IsActive || _user.IsLocked)
        {
            return false;
        }

        // Check if tenant is still active
        var tenantGrain = GrainFactory.GetGrain<ITenantGrain>(_user.TenantId);
        var tenantValid = await tenantGrain.ValidateAsync();
        
        return tenantValid;
    }

    private async Task LoadUserAsync()
    {
        if (_isLoaded) return;

        try
        {
            var userId = this.GetPrimaryKeyString();
            
            // Try to load from grain state first
            if (_userState.State.User != null)
            {
                _user = _userState.State.User;
            }
            else
            {
                // Load from repository
                _user = await _userRepository.GetByIdAsync(userId);
                if (_user != null)
                {
                    _userState.State.User = _user;
                    _userState.State.LastModified = DateTime.UtcNow;
                    await _userState.WriteStateAsync();
                }
            }

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user {UserId}", this.GetPrimaryKeyString());
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (!_isLoaded)
        {
            await LoadUserAsync();
        }
    }

    private async Task UpdateStateAsync()
    {
        if (_user != null)
        {
            _userState.State.User = _user;
            _userState.State.LastModified = DateTime.UtcNow;
            await _userState.WriteStateAsync();
        }
    }

    private async Task LogSecurityEventAsync(SecurityEventType eventType, string reason)
    {
        if (_user != null)
        {
            await _auditService.LogEventAsync(new SecurityEvent
            {
                Type = eventType,
                UserId = _user.Id,
                TenantId = _user.TenantId,
                Email = _user.Email,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private async Task CheckDeactivation(object _)
    {
        if (DateTime.UtcNow - _lastAccessed > TimeSpan.FromMinutes(30))
        {
            this.DeactivateOnIdle();
        }
    }
}

/// <summary>
/// User grain state for persistence
/// </summary>
[GenerateSerializer]
public class UserState
{
    [Id(0)]
    public User? User { get; set; }
    
    [Id(1)]
    public DateTime LastModified { get; set; }
}

/// <summary>
/// User security information
/// </summary>
[GenerateSerializer]
public class UserSecurityInfo
{
    [Id(0)]
    public string UserId { get; set; } = string.Empty;
    
    [Id(1)]
    public bool IsActive { get; set; }
    
    [Id(2)]
    public bool IsLocked { get; set; }
    
    [Id(3)]
    public bool MfaEnabled { get; set; }
    
    [Id(4)]
    public bool EmailConfirmed { get; set; }
    
    [Id(5)]
    public DateTime? LastLoginAt { get; set; }
    
    [Id(6)]
    public int FailedLoginAttempts { get; set; }
    
    [Id(7)]
    public int ActiveSessionCount { get; set; }
    
    [Id(8)]
    public bool RequiresPasswordChange { get; set; }
}