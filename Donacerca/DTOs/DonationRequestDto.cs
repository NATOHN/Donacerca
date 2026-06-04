namespace Donacerca.DTOs;

public class CreateDonationRequestDto
{
    public string PostId { get; set; } = string.Empty;
}

public class SelectReceiverDto
{
    public string ReceiverId { get; set; } = string.Empty;
}