using System.Security.Claims;
using Donacerca.DTOs;
using Donacerca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donacerca.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DonationRequestController : ControllerBase
{
    private readonly RequestService _requestService;

    public DonationRequestController(RequestService requestService)
    {
        _requestService = requestService;
    }

    // Receptor envía solicitud
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDonationRequestDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "Receptor";
        try
        {
            var result = await _requestService.CreateAsync(dto.PostId, userId, userName);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    // Donante ve quiénes solicitaron su publicación
    [HttpGet("post/{postId}")]
    public async Task<IActionResult> GetByPost(string postId) =>
        Ok(await _requestService.GetByPostAsync(postId));

    // Receptor ve sus solicitudes enviadas
    [HttpGet("my")]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        return Ok(await _requestService.GetByReceiverAsync(userId));
    }

    // Donante selecciona receptor
    [HttpPost("select")]
    public async Task<IActionResult> SelectReceiver([FromBody] SelectReceiverDto dto, [FromQuery] string postId)
    {
        var donorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        try
        {
            await _requestService.SelectReceiverAsync(postId, dto.ReceiverId, donorId);
            return Ok(new { message = "Receptor seleccionado correctamente" });
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}