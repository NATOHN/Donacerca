using System.Security.Claims;
using Donacerca.DTOs;
using Donacerca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donacerca.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IWebHostEnvironment _env;

    public AuthController(AuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var user = await _authService.Register(dto);
            return Ok(new
            {
                message = "Usuario registrado exitosamente",
                user.Id,
                user.FullName,
                user.Email,
                user.Zone,
                user.Roles
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.Login(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _authService.GetById(userId);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.FullName, user.Email, user.Zone, user.Roles });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        await _authService.UpdateProfile(userId, dto);

        // Si se actualizó el rol, generar nuevo token
        if (dto.Roles != null && dto.Roles.Count > 0)
        {
            var result = await _authService.GenerateNewTokenAsync(userId);
            return Ok(result);
        }

        return Ok(new { message = "Perfil actualizado" });
    }

    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin([FromQuery] string secret)
    {
        if (secret != "donacerca-admin-2026")
            return Unauthorized();
        var user = await _authService.Register(new RegisterDto
        {
            FullName = "Administrador",
            Email = "admin@donacerca.hn",
            Password = "Admin2026!",
            Zone = "Tegucigalpa",
            Roles = new() { "admin" }
        });
        return Ok(new { message = "Admin creado", user.Email });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        await _authService.RevokeRefreshTokenAsync(userId);
        return Ok(new { message = "Sesión cerrada correctamente" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto.Email);
        return Ok(new { message = "Si el email existe recibirás instrucciones para restablecer tu contraseña" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok(new { message = "Contraseña restablecida correctamente" });
    }

    [HttpGet("reset-token/{email}")]
    public async Task<IActionResult> GetResetToken(string email)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var token = await _authService.GetResetTokenForDemo(email);
        return Ok(token);
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            var result = await _authService.GoogleLoginAsync(dto.IdToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}