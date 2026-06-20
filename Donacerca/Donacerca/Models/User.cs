namespace Donacerca.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;

    // El PDF dice: donante, receptor o ambos. Usamos lista de roles.
    // Valores posibles: "donor", "receiver", "admin"
    public List<string> Roles { get; set; } = new();

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}