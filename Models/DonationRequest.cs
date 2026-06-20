namespace Donacerca.Models;

public class DonationRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PostId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    // "pendiente" | "aceptada" | "rechazada" | "cancelada"
    public string Status { get; set; } = "pendiente";
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
}