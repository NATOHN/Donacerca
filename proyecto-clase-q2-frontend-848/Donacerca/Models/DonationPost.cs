namespace Donacerca.Models;

public class DonationPost
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DonorId { get; set; } = string.Empty;
    public string DonorName { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemCondition { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public List<string> PhotoUrls { get; set; } = new();
    public string Status { get; set; } = "disponible";
    public string? SelectedReceiverId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReservedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool DeactivatedByAdmin { get; set; } = false;
}