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
    
    private readonly IWebHostEnvironment _env; // ← agregar esto

    public AuthController(AuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env; // ← agregar esto
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
    
    // Ver perfil propio
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var user = await _authService.GetById(userId);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.FullName, user.Email, user.Zone, user.Roles });
    }

// Actualizar zona o nombre
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        await _authService.UpdateProfile(userId, dto);
        return Ok(new { message = "Perfil actualizado" });
    }

// Crear primer admin (solo en desarrollo, protegido con una clave)
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

// Cerrar sesión — invalida el refresh token
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        await _authService.RevokeRefreshTokenAsync(userId);
        return Ok(new { message = "Sesión cerrada correctamente" });
    }

// Solicitar reset de contraseña
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto.Email);
        // Siempre respondemos igual por seguridad
        return Ok(new { message = "Si el email existe recibirás instrucciones para restablecer tu contraseña" });
    }

// Restablecer contraseña con el token
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok(new { message = "Contraseña restablecida correctamente" });
    }

// Ver token de reset (solo para demo/desarrollo)
    [HttpGet("reset-token/{email}")]
    public async Task<IActionResult> GetResetToken(string email)
    {
        // Usamos IWebHostEnvironment en lugar de "app"
        if (!_env.IsDevelopment())
            return NotFound();

        var token = await _authService.GetResetTokenForDemo(email);
        return Ok(token);
    }
}