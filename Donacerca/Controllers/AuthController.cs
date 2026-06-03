using Microsoft.AspNetCore.Mvc;
using Donacerca.DTOs;
using Donacerca.Services;


namespace Donacerca.Controllers;

// ApiController activa validaciones automáticas y manejo de errores de modelo
    [ApiController]
    // La ruta se construye automáticamente: api/Auth
    // [controller] toma el nombre de la clase sin la palabra "Controller"
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Guardamos el servicio en una variable privada de solo lectura
        // Solo lectura porque no debería cambiar después de que se inyecta
        private readonly AuthService _authService;

        // El constructor recibe el AuthService gracias a la inyección de dependencias
        // .NET lo resuelve automáticamente porque lo registramos en Program.cs
        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // HttpPost indica que este endpoint responde a peticiones POST
        // La ruta completa queda: POST /api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            // FromBody le dice a .NET que lea los datos del cuerpo de la petición
            // El frontend manda un JSON y .NET lo convierte automáticamente al DTO
            [FromBody] RegisterDto dto)
        {
            try
            {
                var user = await _authService.Register(dto);

                // Ok() devuelve un 200 con el objeto que le pasemos
                // Solo devolvemos los campos necesarios, nunca el hash de la contraseña
                return Ok(new { user.Id, user.FullName, user.Email, user.Role });
            }
            catch (Exception ex)
            {
                // BadRequest devuelve un 400 indicando que algo falló por parte del cliente
                // Por ejemplo, email duplicado o datos inválidos
                return BadRequest(new { message = ex.Message });
            }
        }

        // La ruta completa queda: POST /api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            // Igual que en register, FromBody convierte el JSON entrante al DTO
            [FromBody] LoginDto dto)
        {
            try
            {
                // Si las credenciales son correctas, recibimos el token JWT generado
                var token = await _authService.Login(dto);

                // Devolvemos el token al frontend para que lo guarde
                // El frontend debe mandarlo en cada petición protegida como Bearer token
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                // Credenciales inválidas u otro error — devolvemos 400
                return BadRequest(new { message = ex.Message });
            }
        }
    }