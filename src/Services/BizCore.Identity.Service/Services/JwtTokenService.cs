using BizCore.Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BizCore.Identity.Services;

public class JwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings, ILogger<JwtTokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<TokenResponse> GenerateTokenAsync(User user, List<string> roles, List<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new("tenant_id", user.TenantId.ToString()),
            new("full_name", user.FullName),
            new("profile_picture", user.ProfilePictureUrl ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add roles as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permissions as claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token in user
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = token.ValidTo,
            TokenType = "Bearer"
        };
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }

    public bool ValidateRefreshToken(User user, string refreshToken)
    {
        return user.RefreshToken == refreshToken && 
               user.RefreshTokenExpiresAt > DateTime.UtcNow;
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}

public class TwoFactorService
{
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(ILogger<TwoFactorService> logger)
    {
        _logger = logger;
    }

    public string GenerateSecretKey()
    {
        var key = new byte[20];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        return Convert.ToBase64String(key);
    }

    public string GenerateQrCodeUrl(string email, string secretKey, string issuer = "BizCore ERP")
    {
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedSecret = Uri.EscapeDataString(secretKey);
        
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={encodedSecret}&issuer={encodedIssuer}";
    }

    public bool ValidateCode(string secretKey, string code)
    {
        try
        {
            var tfa = new Google.Authenticator.TwoFactorAuthenticator();
            return tfa.ValidateTwoFactorPIN(secretKey, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating two-factor code");
            return false;
        }
    }

    public List<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>();
        
        for (int i = 0; i < count; i++)
        {
            var code = GenerateRecoveryCode();
            codes.Add(code);
        }
        
        return codes;
    }

    private string GenerateRecoveryCode()
    {
        var bytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        
        var code = BitConverter.ToUInt32(bytes, 0) % 100000000;
        return code.ToString("D8");
    }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}