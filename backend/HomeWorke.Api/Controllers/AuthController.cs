using System.Security.Claims;
using HomeWorke.Api.Models.DTOs;
using HomeWorke.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeWorke.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { error = "Invalid email or password." });

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var employeeId = GetEmployeeId();
        var success = await _authService.ChangePasswordAsync(employeeId, request);
        if (!success)
            return BadRequest(new { error = "Current password is incorrect." });

        return Ok(new { message = "Password changed successfully." });
    }

    /// <summary>
    /// Request a password reset token. In production this would send an email.
    /// Here it returns a token the admin can provide to the user.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var token = await _authService.GenerateResetTokenAsync(request.Email);
        if (token == null)
            // Don't reveal whether the email exists (security best practice)
            return Ok(new { message = "If the email exists, a reset token has been generated.", resetToken = (string?)null });

        return Ok(new { message = "Reset token generated.", resetToken = token });
    }

    /// <summary>Reset password using a reset token.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var success = await _authService.ResetPasswordAsync(request.Email, request.ResetToken, request.NewPassword);
        if (!success)
            return BadRequest(new { error = "Invalid email or reset token." });

        return Ok(new { message = "Password has been reset. You can now log in." });
    }

    private int GetEmployeeId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
