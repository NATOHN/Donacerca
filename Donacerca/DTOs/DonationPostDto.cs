namespace Donacerca.DTOs;

public class CreateDonationPostDto
{
    public string CategoryId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemCondition { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public List<string> PhotoUrls { get; set; } = new();
}

public class UpdateDonationPostDto
{
    public string? CategoryId { get; set; }
    public string? ItemName { get; set; }
    public string? Description { get; set; }
    public string? ItemCondition { get; set; }
    public string? Zone { get; set; }
    public List<string>? PhotoUrls { get; set; }
}