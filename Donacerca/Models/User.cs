namespace Donacerca.Models;

public class User
{
    // Representa un usuario dentro del sistema
    // Esta clase es lo que vamos a guarda en FS
    public string Id { get; set; } = string.Empty;
    
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // La contraseña siempre va "encriptada" hasheada 
    public string PasswordHash { get; set; } = string.Empty;
    
    // Por defecto un usuario nuevo sea solo user
    public string Role { get; set; } = "user";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}