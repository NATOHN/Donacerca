namespace Donacerca.DTOs;


// Lo que el frontend manda cuando se crea un experimento nuevo
// El UserId lo vamos a obtener del token (JWT)
public class ExperimentDto
{
    public string Title { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public bool Success { get; set; } = false;
}