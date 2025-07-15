using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BizCore.Identity.Models;

namespace BizCore.Identity.Services;

/// <summary>
/// Token service for JWT generation and validation
/// </summary>
public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(User user);
    Task<string> GenerateRefreshTokenAsync(string userId);
    Task<ClaimsPrincipal> ValidateTokenAsync(string token);
    Task<bool> RevokeTokenAsync(string token);
    Task<bool> IsTokenRevokedAsync(string token);
    Task<string> GeneratePasswordResetTokenAsync(string userId);
    Task<string> GenerateEmailConfirmationTokenAsync(string userId);
    Task<bool> ValidatePasswordResetTokenAsync(string token);
    Task<bool> ValidateEmailConfirmationTokenAsync(string token);
}

/// <summary>
/// Token service implementation
/// </summary>
public class TokenService : ITokenService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<TokenService> _logger;
    private readonly IdentityConfiguration _config;
    private readonly IDistributedCache _cache;

    public TokenService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ILogger<TokenService> logger,
        IOptions<IdentityConfiguration> config,
        IDistributedCache cache)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _logger = logger;
        _config = config.Value;
        _cache = cache;
    }

    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        try
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new("tenant_id", user.TenantId),
                new("user_id", user.Id),
                new("email", user.Email),
                new("full_name", user.FullName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add user roles
            var userRoles = await _userRepository.GetUserRolesAsync(user.Id);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
                claims.Add(new Claim("role_id", role.Id));
            }

            // Add user permissions
            var userPermissions = await GetUserPermissionsAsync(user.Id);
            foreach (var permission in userPermissions)
            {
                claims.Add(new Claim("permission", permission.Name));
                claims.Add(new Claim("permission_module", permission.Module));
            }

            // Add tenant information
            if (user.Tenant != null)
            {
                claims.Add(new Claim("tenant_name", user.Tenant.Name));
                claims.Add(new Claim("tenant_domain", user.Tenant.Domain));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.JwtSecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config.JwtIssuer,
                audience: _config.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_config.AccessTokenExpirationMinutes),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            
            // Cache token for quick validation
            await _cache.SetStringAsync(
                $"access_token:{user.Id}:{token.Id}",
                tokenString,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.AccessTokenExpirationMinutes)
                });

            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate access token for user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<string> GenerateRefreshTokenAsync(string userId)
    {
        try
        {
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var hashedToken = BCrypt.Net.BCrypt.HashPassword(refreshToken);

            // Store hashed refresh token in cache
            await _cache.SetStringAsync(
                $"refresh_token:{userId}:{refreshToken}",
                hashedToken,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_config.RefreshTokenExpirationDays)
                });

            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate refresh token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ClaimsPrincipal> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config.JwtSecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _config.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = _config.JwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // Check if token is revoked
            var jwtToken = (JwtSecurityToken)validatedToken;
            var jti = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            
            if (!string.IsNullOrEmpty(jti) && await IsTokenRevokedAsync(jti))
            {
                return null;
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return null;
        }
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var jti = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti))
            {
                await _cache.SetStringAsync(
                    $"revoked_token:{jti}",
                    "true",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = jwtToken.ValidTo
                    });
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke token");
            return false;
        }
    }

    public async Task<bool> IsTokenRevokedAsync(string jti)
    {
        try
        {
            var revokedToken = await _cache.GetStringAsync($"revoked_token:{jti}");
            return !string.IsNullOrEmpty(revokedToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if token is revoked");
            return false;
        }
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string userId)
    {
        try
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var hashedToken = BCrypt.Net.BCrypt.HashPassword(token);

            var resetToken = new PasswordResetToken
            {
                UserId = userId,
                Token = hashedToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(_config.PasswordResetTokenExpirationHours)
            };

            // Store in database
            await _userRepository.CreatePasswordResetTokenAsync(resetToken);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate password reset token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(string userId)
    {
        try
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var hashedToken = BCrypt.Net.BCrypt.HashPassword(token);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            var confirmationToken = new EmailConfirmationToken
            {
                UserId = userId,
                Token = hashedToken,
                Email = user.Email,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(_config.EmailConfirmationTokenExpirationHours)
            };

            // Store in database
            await _userRepository.CreateEmailConfirmationTokenAsync(confirmationToken);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate email confirmation token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(string token)
    {
        try
        {
            var tokens = await _userRepository.GetPasswordResetTokensAsync();
            
            foreach (var storedToken in tokens)
            {
                if (BCrypt.Net.BCrypt.Verify(token, storedToken.Token) && storedToken.IsValid)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate password reset token");
            return false;
        }
    }

    public async Task<bool> ValidateEmailConfirmationTokenAsync(string token)
    {
        try
        {
            var tokens = await _userRepository.GetEmailConfirmationTokensAsync();
            
            foreach (var storedToken in tokens)
            {
                if (BCrypt.Net.BCrypt.Verify(token, storedToken.Token) && storedToken.IsValid)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate email confirmation token");
            return false;
        }
    }

    private async Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId)
    {
        var permissions = new List<Permission>();
        
        // Get permissions from roles
        var userRoles = await _userRepository.GetUserRolesAsync(userId);
        foreach (var role in userRoles)
        {
            var rolePermissions = await _roleRepository.GetRolePermissionsAsync(role.Id);
            permissions.AddRange(rolePermissions);
        }

        // Get direct user permissions
        var directPermissions = await _permissionRepository.GetUserPermissionsAsync(userId);
        permissions.AddRange(directPermissions);

        return permissions.Distinct();
    }
}

/// <summary>
/// Password hasher service
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

/// <summary>
/// Password hasher implementation using BCrypt
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly ILogger<PasswordHasher> _logger;

    public PasswordHasher(ILogger<PasswordHasher> logger)
    {
        _logger = logger;
    }

    public string HashPassword(string password)
    {
        try
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash password");
            throw;
        }
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify password");
            return false;
        }
    }
}

/// <summary>
/// Multi-factor authentication service
/// </summary>
public interface IMfaService
{
    Task<MfaResult> GenerateMfaSecretAsync(string userId);
    Task<string> GenerateMfaTokenAsync(string userId);
    Task<bool> ValidateMfaCodeAsync(string userId, string code);
    Task<string[]> GenerateBackupCodesAsync(string userId);
    Task<bool> ValidateBackupCodeAsync(string userId, string code);
}

/// <summary>
/// MFA service implementation
/// </summary>
public class MfaService : IMfaService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<MfaService> _logger;
    private readonly IDistributedCache _cache;

    public MfaService(
        IUserRepository userRepository,
        ILogger<MfaService> logger,
        IDistributedCache cache)
    {
        _userRepository = userRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task<MfaResult> GenerateMfaSecretAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return MfaResult.Failure("User not found");
            }

            var secret = GenerateSecretKey();
            var qrCode = GenerateQrCode(user.Email, secret);

            // Store secret temporarily until confirmed
            await _cache.SetStringAsync(
                $"mfa_temp_secret:{userId}",
                secret,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

            return MfaResult.Success(secret, qrCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate MFA secret for user {UserId}", userId);
            return MfaResult.Failure("Failed to generate MFA secret");
        }
    }

    public async Task<string> GenerateMfaTokenAsync(string userId)
    {
        try
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            
            await _cache.SetStringAsync(
                $"mfa_token:{userId}:{token}",
                "true",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate MFA token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateMfaCodeAsync(string userId, string code)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.MfaSecret))
            {
                return false;
            }

            // Validate TOTP code
            var totp = new Totp(Base32Encoding.ToBytes(user.MfaSecret));
            var isValid = totp.VerifyTotp(code, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate MFA code for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string[]> GenerateBackupCodesAsync(string userId)
    {
        try
        {
            var backupCodes = new string[10];
            for (int i = 0; i < backupCodes.Length; i++)
            {
                backupCodes[i] = GenerateBackupCode();
            }

            // Hash backup codes before storing
            var hashedCodes = backupCodes.Select(code => BCrypt.Net.BCrypt.HashPassword(code)).ToArray();
            
            await _userRepository.UpdateMfaBackupCodesAsync(userId, hashedCodes);

            return backupCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate backup codes for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateBackupCodeAsync(string userId, string code)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.MfaBackupCodes == null)
            {
                return false;
            }

            foreach (var hashedCode in user.MfaBackupCodes)
            {
                if (BCrypt.Net.BCrypt.Verify(code, hashedCode))
                {
                    // Remove used backup code
                    var updatedCodes = user.MfaBackupCodes.Where(c => c != hashedCode).ToArray();
                    await _userRepository.UpdateMfaBackupCodesAsync(userId, updatedCodes);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate backup code for user {UserId}", userId);
            return false;
        }
    }

    private string GenerateSecretKey()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    private string GenerateQrCode(string email, string secret)
    {
        var authenticatorUri = $"otpauth://totp/BizCore ERP:{email}?secret={secret}&issuer=BizCore ERP";
        return authenticatorUri; // In real implementation, generate QR code image
    }

    private string GenerateBackupCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}

/// <summary>
/// Security audit service
/// </summary>
public interface ISecurityAuditService
{
    Task LogEventAsync(SecurityEvent securityEvent);
    Task<IEnumerable<SecurityEvent>> GetEventsAsync(string userId = null, string tenantId = null, DateTime? from = null, DateTime? to = null);
    Task<SecurityMetrics> GetMetricsAsync(string tenantId = null);
    Task<bool> IsUserSuspiciousAsync(string userId);
}

/// <summary>
/// Security audit service implementation
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly ISecurityEventRepository _eventRepository;
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(
        ISecurityEventRepository eventRepository,
        ILogger<SecurityAuditService> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task LogEventAsync(SecurityEvent securityEvent)
    {
        try
        {
            await _eventRepository.CreateAsync(securityEvent);
            
            // Log to application logger as well
            _logger.LogInformation("Security event: {EventType} for user {UserId} in tenant {TenantId}",
                securityEvent.Type, securityEvent.UserId, securityEvent.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event");
        }
    }

    public async Task<IEnumerable<SecurityEvent>> GetEventsAsync(string userId = null, string tenantId = null, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            return await _eventRepository.GetEventsAsync(userId, tenantId, from, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events");
            return Enumerable.Empty<SecurityEvent>();
        }
    }

    public async Task<SecurityMetrics> GetMetricsAsync(string tenantId = null)
    {
        try
        {
            return await _eventRepository.GetMetricsAsync(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security metrics");
            return new SecurityMetrics();
        }
    }

    public async Task<bool> IsUserSuspiciousAsync(string userId)
    {
        try
        {
            var events = await _eventRepository.GetEventsAsync(userId, null, DateTime.UtcNow.AddHours(-24), DateTime.UtcNow);
            
            var suspiciousEvents = events.Count(e => e.Type == SecurityEventType.LoginFailed || 
                                                   e.Type == SecurityEventType.SuspiciousActivity ||
                                                   e.Type == SecurityEventType.UnauthorizedAccess);
            
            return suspiciousEvents > 10; // Threshold for suspicious activity
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user is suspicious");
            return false;
        }
    }
}