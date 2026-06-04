namespace Donacerca.DTOs;

public class CreateDeliveryDto
{
    public string PostId { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string DeliveryLocation { get; set; } = string.Empty;
}