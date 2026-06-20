namespace Donacerca.DTOs;

public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public string? Zone { get; set; }
    public List<string>? Roles { get; set; }
}
