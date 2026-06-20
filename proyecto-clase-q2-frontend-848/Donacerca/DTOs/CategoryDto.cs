using System.ComponentModel.DataAnnotations;

namespace Donacerca.DTOs;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "El nombre de la categoría es requerido")]
    [MinLength(2, ErrorMessage = "Mínimo 2 caracteres")]
    [MaxLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "La descripción no puede superar 200 caracteres")]
    public string Description { get; set; } = string.Empty;
}

public class UpdateCategoryDto
{
    [MinLength(2, ErrorMessage = "Mínimo 2 caracteres")]
    [MaxLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}