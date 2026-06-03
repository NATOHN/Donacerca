namespace Donacerca.Models;

// Represente un experimento o prueba que el usuario realizo
public class Experiment
{
    public string Id { get; set; } = string.Empty;
    
    // Titulo o nombre al experimento
    public string Title { get; set; } = string.Empty;
    
    // Resultado o resumen del experimento
    public string Result { get; set; } = string.Empty;
    
    // Guardamos el id del usuario que creo el experimento o prueba
    public string UserId { get; set; } = string.Empty;
    
    // Resultado, funciono o no
    public bool Success { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}