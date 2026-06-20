using System.ComponentModel.DataAnnotations;

namespace Donacerca.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "El nombre completo es requerido")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "La zona no puede superar 100 caracteres")]
    public string Zone { get; set; } = string.Empty;

    // Debe tener al menos un rol válido
    [MinLength(1, ErrorMessage = "Debe seleccionar al menos un rol")]
    public List<string> Roles { get; set; } = new() { "receiver" };
}