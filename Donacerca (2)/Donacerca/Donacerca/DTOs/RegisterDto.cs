namespace Donacerca.DTOs;

public class RegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;

    // El usuario elige al registrarse: "donor", "receiver", o ambos ["donor","receiver"]
    public List<string> Roles { get; set; } = new() { "receiver" };
}