using BizCore.Identity.Domain.Entities;
using BizCore.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BizCore.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly JwtTokenService _tokenService;
    private readonly TwoFactorService _twoFactorService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<Role> roleManager,
        JwtTokenService tokenService,
        TwoFactorService twoFactorService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _twoFactorService = twoFactorService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TenantId = request.TenantId,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        // Assign default role
        await _userManager.AddToRoleAsync(user, "User");
        
        _logger.LogInformation("User {Email} registered successfully", request.Email);
        
        return Ok(new { Message = "User registered successfully", UserId = user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        
        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
            return Unauthorized(new { Message = "Invalid credentials" });
        }

        if (user.IsLocked)
        {
            _logger.LogWarning("Login attempt for locked user: {Email}", request.Email);
            return Unauthorized(new { Message = "Account is locked" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        
        if (!result.Succeeded)
        {
            user.RecordFailedLogin(GetIpAddress(), GetUserAgent());
            await _userManager.UpdateAsync(user);
            
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            return Unauthorized(new { Message = "Invalid credentials" });
        }

        if (user.IsTwoFactorEnabled && !request.SkipTwoFactor)
        {
            return Ok(new { RequiresTwoFactor = true, UserId = user.Id });
        }

        return await GenerateTokenResponse(user);
    }

    [HttpPost("login-2fa")]
    public async Task<IActionResult> LoginWithTwoFactor([FromBody] TwoFactorLoginRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        
        if (user == null)
        {
            return Unauthorized(new { Message = "Invalid request" });
        }

        bool isValidCode = false;
        
        if (!string.IsNullOrEmpty(request.Code))
        {
            isValidCode = _twoFactorService.ValidateCode(user.TwoFactorSecret!, request.Code);
        }
        else if (!string.IsNullOrEmpty(request.RecoveryCode))
        {
            isValidCode = user.TwoFactorRecoveryCodes?.Contains(request.RecoveryCode) == true;
            
            if (isValidCode)
            {
                // Remove used recovery code
                user.TwoFactorRecoveryCodes!.Remove(request.RecoveryCode);
                await _userManager.UpdateAsync(user);
            }
        }

        if (!isValidCode)
        {
            _logger.LogWarning("Invalid 2FA code for user: {Email}", user.Email);
            return Unauthorized(new { Message = "Invalid code" });
        }

        return await GenerateTokenResponse(user);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        
        if (principal == null)
        {
            return Unauthorized(new { Message = "Invalid token" });
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null || !_tokenService.ValidateRefreshToken(user, request.RefreshToken))
        {
            return Unauthorized(new { Message = "Invalid refresh token" });
        }

        return await GenerateTokenResponse(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user != null)
        {
            user.ClearRefreshToken();
            await _userManager.UpdateAsync(user);
        }

        return Ok(new { Message = "Logged out successfully" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        
        if (user == null)
        {
            // Don't reveal that the user doesn't exist
            return Ok(new { Message = "If the email exists, a password reset link has been sent" });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // TODO: Send email with reset link
        _logger.LogInformation("Password reset token generated for user: {Email}", request.Email);
        
        return Ok(new { Message = "If the email exists, a password reset link has been sent" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        
        if (user == null)
        {
            return BadRequest(new { Message = "Invalid request" });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Password reset successful for user: {Email}", request.Email);
        
        return Ok(new { Message = "Password reset successfully" });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        user.LastPasswordChangedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        
        _logger.LogInformation("Password changed for user: {Email}", user.Email);
        
        return Ok(new { Message = "Password changed successfully" });
    }

    [HttpPost("setup-2fa")]
    [Authorize]
    public async Task<IActionResult> SetupTwoFactor()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            return Unauthorized();
        }

        var secret = _twoFactorService.GenerateSecretKey();
        var qrCodeUrl = _twoFactorService.GenerateQrCodeUrl(user.Email!, secret);
        
        // Store secret temporarily (will be confirmed later)
        user.TwoFactorSecret = secret;
        await _userManager.UpdateAsync(user);
        
        return Ok(new { 
            SecretKey = secret,
            QrCodeUrl = qrCodeUrl,
            Message = "Scan the QR code with your authenticator app and verify with a code"
        });
    }

    [HttpPost("verify-2fa")]
    [Authorize]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            return Unauthorized();
        }

        var isValid = _twoFactorService.ValidateCode(user.TwoFactorSecret!, request.Code);
        
        if (!isValid)
        {
            return BadRequest(new { Message = "Invalid code" });
        }

        user.IsTwoFactorEnabled = true;
        user.TwoFactorRecoveryCodes = _twoFactorService.GenerateRecoveryCodes();
        await _userManager.UpdateAsync(user);
        
        return Ok(new { 
            Message = "Two-factor authentication enabled successfully",
            RecoveryCodes = user.TwoFactorRecoveryCodes
        });
    }

    [HttpPost("disable-2fa")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            return Unauthorized();
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        
        if (!isValidPassword)
        {
            return BadRequest(new { Message = "Invalid password" });
        }

        user.DisableTwoFactor();
        await _userManager.UpdateAsync(user);
        
        return Ok(new { Message = "Two-factor authentication disabled" });
    }

    private async Task<IActionResult> GenerateTokenResponse(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user);
        
        var tokenResponse = await _tokenService.GenerateTokenAsync(user, roles.ToList(), permissions);
        
        user.RecordLogin(GetIpAddress(), GetUserAgent());
        await _userManager.UpdateAsync(user);
        
        return Ok(tokenResponse);
    }

    private async Task<List<string>> GetUserPermissionsAsync(User user)
    {
        var permissions = new List<string>();
        
        // Direct user permissions
        permissions.AddRange(user.UserPermissions.Select(up => up.Permission));
        
        // Role permissions
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                permissions.AddRange(role.RolePermissions.Select(rp => rp.Permission));
            }
        }
        
        return permissions.Distinct().ToList();
    }

    private string GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string GetUserAgent()
    {
        return HttpContext.Request.Headers.UserAgent.ToString();
    }
}

// Request DTOs
public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    Guid TenantId,
    string? PhoneNumber = null);

public record LoginRequest(
    string Email,
    string Password,
    bool SkipTwoFactor = false);

public record TwoFactorLoginRequest(
    Guid UserId,
    string? Code = null,
    string? RecoveryCode = null);

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record VerifyTwoFactorRequest(string Code);

public record DisableTwoFactorRequest(string Password);