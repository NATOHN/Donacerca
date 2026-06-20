using System.ComponentModel.DataAnnotations;

namespace Donacerca.Validations;

// Atributo reutilizable para validar que una fecha sea futura
public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is DateTime date)
            return date > DateTime.UtcNow;
        return false;
    }
}