namespace Donacerca.Models;

public class DonationStatistics
{
    public int ActivePosts { get; set; }
    public int CompletedDonations { get; set; }
    public int PendingRequests { get; set; }
    public int TotalPosts { get; set; }
    public double CompletionRate { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}