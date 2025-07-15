using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace BizCore.Identity;

/// <summary>
/// Advanced identity service for BizCore ERP
/// Handles authentication, authorization, and multi-tenant user management
/// </summary>
public interface IIdentityService
{
    // Authentication
    Task<AuthResult> AuthenticateAsync(string email, string password, string tenantId = null);
    Task<AuthResult> AuthenticateWithTokenAsync(string token);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string token);
    Task<bool> RevokeTokenAsync(string token);
    Task<AuthResult> ImpersonateUserAsync(string adminUserId, string targetUserId);
    Task<bool> EndImpersonationAsync(string sessionId);

    // User Management
    Task<UserResult> CreateUserAsync(CreateUserRequest request);
    Task<UserResult> UpdateUserAsync(string userId, UpdateUserRequest request);
    Task<UserResult> GetUserAsync(string userId);
    Task<UserResult> GetUserByEmailAsync(string email, string tenantId = null);
    Task<bool> DeleteUserAsync(string userId);
    Task<bool> ActivateUserAsync(string userId);
    Task<bool> DeactivateUserAsync(string userId);
    Task<bool> LockUserAsync(string userId, TimeSpan? lockoutDuration = null);
    Task<bool> UnlockUserAsync(string userId);

    // Password Management
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string userId, string newPassword);
    Task<string> GeneratePasswordResetTokenAsync(string email, string tenantId = null);
    Task<bool> ValidatePasswordResetTokenAsync(string token);
    Task<bool> ResetPasswordWithTokenAsync(string token, string newPassword);

    // Multi-Factor Authentication
    Task<MfaResult> EnableMfaAsync(string userId, MfaType type);
    Task<MfaResult> DisableMfaAsync(string userId);
    Task<MfaResult> GenerateMfaCodeAsync(string userId);
    Task<bool> ValidateMfaCodeAsync(string userId, string code);
    Task<MfaResult> GenerateBackupCodesAsync(string userId);

    // Role Management
    Task<RoleResult> CreateRoleAsync(string name, string description, string tenantId = null);
    Task<RoleResult> UpdateRoleAsync(string roleId, string name, string description);
    Task<bool> DeleteRoleAsync(string roleId);
    Task<IEnumerable<RoleResult>> GetRolesAsync(string tenantId = null);
    Task<bool> AssignRoleAsync(string userId, string roleId);
    Task<bool> RemoveRoleAsync(string userId, string roleId);
    Task<IEnumerable<string>> GetUserRolesAsync(string userId);

    // Permission Management
    Task<PermissionResult> CreatePermissionAsync(string name, string description, string module);
    Task<IEnumerable<PermissionResult>> GetPermissionsAsync(string module = null);
    Task<bool> AssignPermissionToRoleAsync(string roleId, string permissionId);
    Task<bool> RemovePermissionFromRoleAsync(string roleId, string permissionId);
    Task<bool> HasPermissionAsync(string userId, string permission);
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);

    // Tenant Management
    Task<TenantResult> CreateTenantAsync(CreateTenantRequest request);
    Task<TenantResult> UpdateTenantAsync(string tenantId, UpdateTenantRequest request);
    Task<TenantResult> GetTenantAsync(string tenantId);
    Task<TenantResult> GetTenantByDomainAsync(string domain);
    Task<bool> DeleteTenantAsync(string tenantId);
    Task<bool> ActivateTenantAsync(string tenantId);
    Task<bool> DeactivateTenantAsync(string tenantId);
    Task<IEnumerable<TenantResult>> GetTenantsAsync();

    // Session Management
    Task<SessionResult> CreateSessionAsync(string userId, string tenantId, string ipAddress, string userAgent);
    Task<bool> EndSessionAsync(string sessionId);
    Task<IEnumerable<SessionResult>> GetUserSessionsAsync(string userId);
    Task<bool> EndAllUserSessionsAsync(string userId);
    Task<SessionResult> GetSessionAsync(string sessionId);

    // Audit & Security
    Task<bool> LogSecurityEventAsync(SecurityEvent securityEvent);
    Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(string userId = null, string tenantId = null, DateTime? from = null, DateTime? to = null);
    Task<bool> IsUserSuspiciousAsync(string userId);
    Task<SecurityMetrics> GetSecurityMetricsAsync(string tenantId = null);

    // Single Sign-On
    Task<SsoResult> CreateSsoProviderAsync(string tenantId, SsoProvider provider);
    Task<SsoResult> AuthenticateWithSsoAsync(string provider, string token, string tenantId);
    Task<string> GenerateSsoUrlAsync(string provider, string tenantId, string returnUrl);
}

/// <summary>
/// Advanced identity service implementation
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMfaService _mfaService;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<IdentityService> _logger;
    private readonly IdentityConfiguration _config;

    public IdentityService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ITenantRepository tenantRepository,
        ISessionRepository sessionRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IMfaService mfaService,
        ISecurityAuditService auditService,
        ILogger<IdentityService> logger,
        IOptions<IdentityConfiguration> config)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _tenantRepository = tenantRepository;
        _sessionRepository = sessionRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _mfaService = mfaService;
        _auditService = auditService;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<AuthResult> AuthenticateAsync(string email, string password, string tenantId = null)
    {
        try
        {
            // Rate limiting check
            if (await IsRateLimitedAsync(email))
            {
                await _auditService.LogEventAsync(new SecurityEvent
                {
                    Type = SecurityEventType.RateLimitExceeded,
                    Email = email,
                    TenantId = tenantId,
                    IpAddress = GetCurrentIpAddress(),
                    Timestamp = DateTime.UtcNow
                });

                return AuthResult.Failure("Rate limit exceeded. Please try again later.");
            }

            // Get user by email
            var user = await _userRepository.GetByEmailAsync(email, tenantId);
            if (user == null)
            {
                await _auditService.LogEventAsync(new SecurityEvent
                {
                    Type = SecurityEventType.LoginFailed,
                    Email = email,
                    TenantId = tenantId,
                    Reason = "User not found",
                    IpAddress = GetCurrentIpAddress(),
                    Timestamp = DateTime.UtcNow
                });

                return AuthResult.Failure("Invalid credentials.");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                await _auditService.LogEventAsync(new SecurityEvent
                {
                    Type = SecurityEventType.LoginFailed,
                    UserId = user.Id,
                    Email = email,
                    TenantId = tenantId,
                    Reason = "User inactive",
                    IpAddress = GetCurrentIpAddress(),
                    Timestamp = DateTime.UtcNow
                });

                return AuthResult.Failure("Account is inactive.");
            }

            // Check if user is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                await _auditService.LogEventAsync(new SecurityEvent
                {
                    Type = SecurityEventType.LoginFailed,
                    UserId = user.Id,
                    Email = email,
                    TenantId = tenantId,
                    Reason = "Account locked",
                    IpAddress = GetCurrentIpAddress(),
                    Timestamp = DateTime.UtcNow
                });

                return AuthResult.Failure("Account is locked.");
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                await IncrementFailedLoginAttemptsAsync(user.Id);
                
                await _auditService.LogEventAsync(new SecurityEvent
                {
                    Type = SecurityEventType.LoginFailed,
                    UserId = user.Id,
                    Email = email,
                    TenantId = tenantId,
                    Reason = "Invalid password",
                    IpAddress = GetCurrentIpAddress(),
                    Timestamp = DateTime.UtcNow
                });

                return AuthResult.Failure("Invalid credentials.");
            }

            // Check if MFA is required
            if (user.MfaEnabled)
            {
                var mfaToken = await _mfaService.GenerateMfaTokenAsync(user.Id);
                return AuthResult.RequiresMfa(mfaToken);
            }

            // Generate tokens
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            // Create session
            var session = await CreateSessionAsync(user.Id, user.TenantId, GetCurrentIpAddress(), GetCurrentUserAgent());

            // Reset failed login attempts
            await _userRepository.ResetFailedLoginAttemptsAsync(user.Id);

            // Update last login
            await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);

            await _auditService.LogEventAsync(new SecurityEvent
            {
                Type = SecurityEventType.LoginSuccess,
                UserId = user.Id,
                Email = email,
                TenantId = tenantId,
                IpAddress = GetCurrentIpAddress(),
                Timestamp = DateTime.UtcNow
            });

            return AuthResult.Success(accessToken, refreshToken, session.Value.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for email {Email}", email);
            return AuthResult.Failure("Authentication failed.");
        }
    }

    public async Task<AuthResult> AuthenticateWithTokenAsync(string token)
    {
        try
        {
            var principal = await _tokenService.ValidateTokenAsync(token);
            if (principal == null)
            {
                return AuthResult.Failure("Invalid token.");
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return AuthResult.Failure("Invalid token claims.");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return AuthResult.Failure("User not found or inactive.");
            }

            return AuthResult.Success(token, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token authentication failed");
            return AuthResult.Failure("Token authentication failed.");
        }
    }

    public async Task<UserResult> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            // Validate tenant
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId);
            if (tenant == null || !tenant.IsActive)
            {
                return UserResult.Failure("Invalid or inactive tenant.");
            }

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, request.TenantId);
            if (existingUser != null)
            {
                return UserResult.Failure("User with this email already exists.");
            }

            // Validate password
            var passwordValidation = ValidatePassword(request.Password);
            if (!passwordValidation.IsValid)
            {
                return UserResult.Failure(passwordValidation.ErrorMessage);
            }

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email.ToLowerInvariant(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                TenantId = request.TenantId,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                MfaEnabled = request.RequireMfa
            };

            await _userRepository.CreateAsync(user);

            // Assign default roles
            if (request.RoleIds?.Any() == true)
            {
                foreach (var roleId in request.RoleIds)
                {
                    await AssignRoleAsync(user.Id, roleId);
                }
            }

            await _auditService.LogEventAsync(new SecurityEvent
            {
                Type = SecurityEventType.UserCreated,
                UserId = user.Id,
                Email = user.Email,
                TenantId = user.TenantId,
                IpAddress = GetCurrentIpAddress(),
                Timestamp = DateTime.UtcNow
            });

            return UserResult.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {Email}", request.Email);
            return UserResult.Failure("Failed to create user.");
        }
    }

    public async Task<TenantResult> CreateTenantAsync(CreateTenantRequest request)
    {
        try
        {
            // Validate unique constraints
            var existingTenant = await _tenantRepository.GetByDomainAsync(request.Domain);
            if (existingTenant != null)
            {
                return TenantResult.Failure("Domain already exists.");
            }

            // Create tenant
            var tenant = new Tenant
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Domain = request.Domain.ToLowerInvariant(),
                DatabaseName = $"BizCore_{request.Domain}_{Guid.NewGuid():N}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Settings = new TenantSettings
                {
                    MaxUsers = request.MaxUsers ?? 100,
                    StorageQuotaGB = request.StorageQuotaGB ?? 10,
                    Features = request.Features ?? new string[0]
                }
            };

            await _tenantRepository.CreateAsync(tenant);

            // Create tenant database
            await CreateTenantDatabaseAsync(tenant);

            // Create default admin user
            var adminUser = await CreateDefaultAdminUserAsync(tenant, request.AdminEmail, request.AdminPassword);

            await _auditService.LogEventAsync(new SecurityEvent
            {
                Type = SecurityEventType.TenantCreated,
                TenantId = tenant.Id,
                IpAddress = GetCurrentIpAddress(),
                Timestamp = DateTime.UtcNow
            });

            return TenantResult.Success(tenant, adminUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tenant {Domain}", request.Domain);
            return TenantResult.Failure("Failed to create tenant.");
        }
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            // Get user roles
            var userRoles = await _userRepository.GetUserRolesAsync(userId);
            
            // Check if any role has the permission
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
            _logger.LogError(ex, "Failed to check permission {Permission} for user {UserId}", permission, userId);
            return false;
        }
    }

    // Private helper methods
    private async Task<bool> IsRateLimitedAsync(string email)
    {
        // Implementation for rate limiting
        // This would check Redis or in-memory cache for failed attempts
        return false;
    }

    private async Task IncrementFailedLoginAttemptsAsync(string userId)
    {
        await _userRepository.IncrementFailedLoginAttemptsAsync(userId);
        
        var user = await _userRepository.GetByIdAsync(userId);
        if (user.FailedLoginAttempts >= _config.MaxFailedLoginAttempts)
        {
            await _userRepository.LockUserAsync(userId, DateTime.UtcNow.AddMinutes(_config.LockoutDurationMinutes));
        }
    }

    private PasswordValidationResult ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password is required." };
        }

        if (password.Length < _config.MinPasswordLength)
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = $"Password must be at least {_config.MinPasswordLength} characters long." };
        }

        if (_config.RequireUppercase && !password.Any(char.IsUpper))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password must contain at least one uppercase letter." };
        }

        if (_config.RequireLowercase && !password.Any(char.IsLower))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password must contain at least one lowercase letter." };
        }

        if (_config.RequireDigit && !password.Any(char.IsDigit))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password must contain at least one number." };
        }

        if (_config.RequireSpecialCharacter && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password must contain at least one special character." };
        }

        return new PasswordValidationResult { IsValid = true };
    }

    private async Task CreateTenantDatabaseAsync(Tenant tenant)
    {
        // Implementation for creating tenant-specific database
        // This would create a new database or schema for the tenant
    }

    private async Task<User> CreateDefaultAdminUserAsync(Tenant tenant, string adminEmail, string adminPassword)
    {
        var adminUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = adminEmail.ToLowerInvariant(),
            FirstName = "Admin",
            LastName = "User",
            TenantId = tenant.Id,
            PasswordHash = _passwordHasher.HashPassword(adminPassword),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(adminUser);
        
        // Assign admin role
        var adminRole = await _roleRepository.GetByNameAsync("Admin", tenant.Id);
        if (adminRole != null)
        {
            await AssignRoleAsync(adminUser.Id, adminRole.Id);
        }

        return adminUser;
    }

    private string GetCurrentIpAddress()
    {
        // Implementation to get current IP address from HTTP context
        return "127.0.0.1";
    }

    private string GetCurrentUserAgent()
    {
        // Implementation to get current user agent from HTTP context
        return "Unknown";
    }

    // Implement other interface methods...
    public async Task<AuthResult> AuthenticateWithTokenAsync(string token) => throw new NotImplementedException();
    public async Task<AuthResult> RefreshTokenAsync(string refreshToken) => throw new NotImplementedException();
    public async Task<bool> ValidateTokenAsync(string token) => throw new NotImplementedException();
    public async Task<bool> RevokeTokenAsync(string token) => throw new NotImplementedException();
    public async Task<AuthResult> ImpersonateUserAsync(string adminUserId, string targetUserId) => throw new NotImplementedException();
    public async Task<bool> EndImpersonationAsync(string sessionId) => throw new NotImplementedException();
    public async Task<UserResult> UpdateUserAsync(string userId, UpdateUserRequest request) => throw new NotImplementedException();
    public async Task<UserResult> GetUserAsync(string userId) => throw new NotImplementedException();
    public async Task<UserResult> GetUserByEmailAsync(string email, string tenantId = null) => throw new NotImplementedException();
    public async Task<bool> DeleteUserAsync(string userId) => throw new NotImplementedException();
    public async Task<bool> ActivateUserAsync(string userId) => throw new NotImplementedException();
    public async Task<bool> DeactivateUserAsync(string userId) => throw new NotImplementedException();
    public async Task<bool> LockUserAsync(string userId, TimeSpan? lockoutDuration = null) => throw new NotImplementedException();
    public async Task<bool> UnlockUserAsync(string userId) => throw new NotImplementedException();
    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword) => throw new NotImplementedException();
    public async Task<bool> ResetPasswordAsync(string userId, string newPassword) => throw new NotImplementedException();
    public async Task<string> GeneratePasswordResetTokenAsync(string email, string tenantId = null) => throw new NotImplementedException();
    public async Task<bool> ValidatePasswordResetTokenAsync(string token) => throw new NotImplementedException();
    public async Task<bool> ResetPasswordWithTokenAsync(string token, string newPassword) => throw new NotImplementedException();
    public async Task<MfaResult> EnableMfaAsync(string userId, MfaType type) => throw new NotImplementedException();
    public async Task<MfaResult> DisableMfaAsync(string userId) => throw new NotImplementedException();
    public async Task<MfaResult> GenerateMfaCodeAsync(string userId) => throw new NotImplementedException();
    public async Task<bool> ValidateMfaCodeAsync(string userId, string code) => throw new NotImplementedException();
    public async Task<MfaResult> GenerateBackupCodesAsync(string userId) => throw new NotImplementedException();
    public async Task<RoleResult> CreateRoleAsync(string name, string description, string tenantId = null) => throw new NotImplementedException();
    public async Task<RoleResult> UpdateRoleAsync(string roleId, string name, string description) => throw new NotImplementedException();
    public async Task<bool> DeleteRoleAsync(string roleId) => throw new NotImplementedException();
    public async Task<IEnumerable<RoleResult>> GetRolesAsync(string tenantId = null) => throw new NotImplementedException();
    public async Task<bool> AssignRoleAsync(string userId, string roleId) => throw new NotImplementedException();
    public async Task<bool> RemoveRoleAsync(string userId, string roleId) => throw new NotImplementedException();
    public async Task<IEnumerable<string>> GetUserRolesAsync(string userId) => throw new NotImplementedException();
    public async Task<PermissionResult> CreatePermissionAsync(string name, string description, string module) => throw new NotImplementedException();
    public async Task<IEnumerable<PermissionResult>> GetPermissionsAsync(string module = null) => throw new NotImplementedException();
    public async Task<bool> AssignPermissionToRoleAsync(string roleId, string permissionId) => throw new NotImplementedException();
    public async Task<bool> RemovePermissionFromRoleAsync(string roleId, string permissionId) => throw new NotImplementedException();
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId) => throw new NotImplementedException();
    public async Task<TenantResult> UpdateTenantAsync(string tenantId, UpdateTenantRequest request) => throw new NotImplementedException();
    public async Task<TenantResult> GetTenantAsync(string tenantId) => throw new NotImplementedException();
    public async Task<TenantResult> GetTenantByDomainAsync(string domain) => throw new NotImplementedException();
    public async Task<bool> DeleteTenantAsync(string tenantId) => throw new NotImplementedException();
    public async Task<bool> ActivateTenantAsync(string tenantId) => throw new NotImplementedException();
    public async Task<bool> DeactivateTenantAsync(string tenantId) => throw new NotImplementedException();
    public async Task<IEnumerable<TenantResult>> GetTenantsAsync() => throw new NotImplementedException();
    public async Task<SessionResult> CreateSessionAsync(string userId, string tenantId, string ipAddress, string userAgent) => throw new NotImplementedException();
    public async Task<bool> EndSessionAsync(string sessionId) => throw new NotImplementedException();
    public async Task<IEnumerable<SessionResult>> GetUserSessionsAsync(string userId) => throw new NotImplementedException();
    public async Task<bool> EndAllUserSessionsAsync(string userId) => throw new NotImplementedException();
    public async Task<SessionResult> GetSessionAsync(string sessionId) => throw new NotImplementedException();
    public async Task<bool> LogSecurityEventAsync(SecurityEvent securityEvent) => throw new NotImplementedException();
    public async Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(string userId = null, string tenantId = null, DateTime? from = null, DateTime? to = null) => throw new NotImplementedException();
    public async Task<bool> IsUserSuspiciousAsync(string userId) => throw new NotImplementedException();
    public async Task<SecurityMetrics> GetSecurityMetricsAsync(string tenantId = null) => throw new NotImplementedException();
    public async Task<SsoResult> CreateSsoProviderAsync(string tenantId, SsoProvider provider) => throw new NotImplementedException();
    public async Task<SsoResult> AuthenticateWithSsoAsync(string provider, string token, string tenantId) => throw new NotImplementedException();
    public async Task<string> GenerateSsoUrlAsync(string provider, string tenantId, string returnUrl) => throw new NotImplementedException();
}

// Result classes and DTOs
public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string SessionId { get; set; }
    public string ErrorMessage { get; set; }
    public bool RequiresMfa { get; set; }
    public string MfaToken { get; set; }

    public static AuthResult Success(string accessToken, string refreshToken, string sessionId) =>
        new() { IsSuccess = true, AccessToken = accessToken, RefreshToken = refreshToken, SessionId = sessionId };

    public static AuthResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static AuthResult RequiresMfa(string mfaToken) =>
        new() { IsSuccess = false, RequiresMfa = true, MfaToken = mfaToken };
}

public class UserResult
{
    public bool IsSuccess { get; set; }
    public User User { get; set; }
    public string ErrorMessage { get; set; }

    public static UserResult Success(User user) =>
        new() { IsSuccess = true, User = user };

    public static UserResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

public class TenantResult
{
    public bool IsSuccess { get; set; }
    public Tenant Tenant { get; set; }
    public User AdminUser { get; set; }
    public string ErrorMessage { get; set; }

    public static TenantResult Success(Tenant tenant, User adminUser = null) =>
        new() { IsSuccess = true, Tenant = tenant, AdminUser = adminUser };

    public static TenantResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

public class RoleResult
{
    public bool IsSuccess { get; set; }
    public Role Role { get; set; }
    public string ErrorMessage { get; set; }

    public static RoleResult Success(Role role) =>
        new() { IsSuccess = true, Role = role };

    public static RoleResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

public class PermissionResult
{
    public bool IsSuccess { get; set; }
    public Permission Permission { get; set; }
    public string ErrorMessage { get; set; }

    public static PermissionResult Success(Permission permission) =>
        new() { IsSuccess = true, Permission = permission };

    public static PermissionResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

public class SessionResult
{
    public bool IsSuccess { get; set; }
    public Session Session { get; set; }
    public string ErrorMessage { get; set; }

    public static SessionResult Success(Session session) =>
        new() { IsSuccess = true, Session = session };

    public static SessionResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

public class MfaResult
{
    public bool IsSuccess { get; set; }
    public string Secret { get; set; }
    public string QrCode { get; set; }
    public string[] BackupCodes { get; set; }
    public string ErrorMessage { get; set; }

    public static MfaResult Success(string secret = null, string qrCode = null, string[] backupCodes = null) =>
        new() { IsSuccess = true, Secret = secret, QrCode = qrCode, BackupCodes = backupCodes };

    public static MfaResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

public class SsoResult
{
    public bool IsSuccess { get; set; }
    public string AuthUrl { get; set; }
    public string ErrorMessage { get; set; }

    public static SsoResult Success(string authUrl) =>
        new() { IsSuccess = true, AuthUrl = authUrl };

    public static SsoResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
}

// Request DTOs
public class CreateUserRequest
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Password { get; set; }
    public string TenantId { get; set; }
    public string[] RoleIds { get; set; }
    public bool RequireMfa { get; set; }
}

public class UpdateUserRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public bool? IsActive { get; set; }
    public string[] RoleIds { get; set; }
}

public class CreateTenantRequest
{
    public string Name { get; set; }
    public string Domain { get; set; }
    public string AdminEmail { get; set; }
    public string AdminPassword { get; set; }
    public int? MaxUsers { get; set; }
    public int? StorageQuotaGB { get; set; }
    public string[] Features { get; set; }
}