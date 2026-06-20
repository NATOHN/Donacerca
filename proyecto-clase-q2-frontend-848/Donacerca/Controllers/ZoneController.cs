using Microsoft.AspNetCore.Mvc;
using Donacerca.Models;

namespace Donacerca.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ZoneController : ControllerBase
{
    private static readonly List<Zone> Zones = new()
    {
        new Zone
        {
            Id = "zona-1",
            Name = "Centro",
            Address = "1 Calle, 1 Avenida, Centro Histórico",
            Reference = "Frente al Parque Central San Pedro Sula"
        },
        new Zone
        {
            Id = "zona-2",
            Name = "Megaplaza",
            Address = "Bulevar del Sur, Colonia Trejo",
            Reference = "Entrada principal del Megaplaza"
        },
        new Zone
        {
            Id = "zona-3",
            Name = "Chamelecón",
            Address = "Bulevar Chamelecón, entrada principal",
            Reference = "Frente al mercado de Chamelecón"
        },
        new Zone
        {
            Id = "zona-4",
            Name = "Rivera Hernández",
            Address = "Colonia Rivera Hernández, calle principal",
            Reference = "Frente al parque de Rivera Hernández"
        },
        new Zone
        {
            Id = "zona-5",
            Name = "Planeta",
            Address = "Bulevar Morazán, Colonia Planeta",
            Reference = "Frente al Centro Comercial Planeta"
        }
    };

    // Devuelve todas las zonas disponibles
    [HttpGet]
    public IActionResult GetAll() => Ok(Zones);

    // Devuelve una zona por ID
    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var zone = Zones.FirstOrDefault(z => z.Id == id);
        return zone == null ? NotFound() : Ok(zone);
    }
}