using System.ComponentModel.DataAnnotations;

namespace Donacerca.DTOs;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
    public string NewPassword { get; set; } = string.Empty;
}

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class GoogleLoginDto
{
    [Required(ErrorMessage = "El token de Google es requerido")]
    public string IdToken { get; set; } = string.Empty;
}