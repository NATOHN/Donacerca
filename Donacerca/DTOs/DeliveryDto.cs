using System.ComponentModel.DataAnnotations;
using Donacerca.Validations;

namespace Donacerca.DTOs;

public class CreateDeliveryDto
{
    [Required(ErrorMessage = "El ID de la publicación es requerido")]
    public string PostId { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de entrega es requerida")]
    [FutureDate(ErrorMessage = "La fecha de entrega debe ser en el futuro")]
    public DateTime DeliveryDate { get; set; }

    [Required(ErrorMessage = "El punto de encuentro es requerido")]
    [MinLength(5, ErrorMessage = "Describe mejor el punto de encuentro")]
    [MaxLength(200)]
    public string DeliveryLocation { get; set; } = string.Empty;
}