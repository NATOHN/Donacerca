using System.ComponentModel.DataAnnotations;

namespace Donacerca.DTOs;

public class CreateDonationRequestDto
{
    [Required(ErrorMessage = "El ID de la publicación es requerido")]
    public string PostId { get; set; } = string.Empty;
}

public class SelectReceiverDto
{
    [Required(ErrorMessage = "El ID del receptor es requerido")]
    public string ReceiverId { get; set; } = string.Empty;
}