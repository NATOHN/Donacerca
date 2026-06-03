namespace Donacerca.DTOs;

public class RegisterDto
{
    // Lo que el frontend o el usuario desde una interfaz va enviar 
    // Cuando se quiera registrar
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}