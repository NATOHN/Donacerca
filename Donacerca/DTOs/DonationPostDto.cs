using System.ComponentModel.DataAnnotations;

namespace Donacerca.DTOs;

public class CreateDonationPostDto
{
    [Required(ErrorMessage = "La categoría es requerida")]
    public string CategoryId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del artículo es requerido")]
    [MinLength(3, ErrorMessage = "Mínimo 3 caracteres")]
    [MaxLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string ItemName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es requerida")]
    [MinLength(10, ErrorMessage = "La descripción debe tener al menos 10 caracteres")]
    [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "El estado del artículo es requerido")]
    // Solo acepta los tres valores que define el PDF
    [RegularExpression("^(nuevo|buen estado|uso regular)$",
        ErrorMessage = "El estado debe ser: nuevo, buen estado, o uso regular")]
    public string ItemCondition { get; set; } = string.Empty;

    [Required(ErrorMessage = "La zona de entrega es requerida")]
    [MaxLength(100)]
    public string Zone { get; set; } = string.Empty;

    // Máximo 5 fotos por publicación
    [MaxLength(5, ErrorMessage = "Máximo 5 fotos por publicación")]
    public List<string> PhotoUrls { get; set; } = new();
}

public class UpdateDonationPostDto
{
    [MinLength(3)][MaxLength(100)]
    public string? ItemName { get; set; }

    [MinLength(10)][MaxLength(500)]
    public string? Description { get; set; }

    [RegularExpression("^(nuevo|buen estado|uso regular)$",
        ErrorMessage = "Estado inválido")]
    public string? ItemCondition { get; set; }

    [MaxLength(100)]
    public string? Zone { get; set; }

    public string? CategoryId { get; set; }

    [MaxLength(5, ErrorMessage = "Máximo 5 fotos")]
    public List<string>? PhotoUrls { get; set; }
}