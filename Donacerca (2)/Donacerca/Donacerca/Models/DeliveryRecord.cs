namespace Donacerca.Models;

public class DeliveryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PostId { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string DeliveryLocation { get; set; } = string.Empty;
    public bool ConfirmedByDonor { get; set; } = false;
    public bool ConfirmedByReceiver { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
}