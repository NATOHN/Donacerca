using System.Security.Claims;
using Donacerca.DTOs;
using Donacerca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donacerca.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeliveryController : ControllerBase
{
    private readonly DeliveryService _deliveryService;

    public DeliveryController(DeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryDto dto)
    {
        var donorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        try
        {
            var result = await _deliveryService.CreateDeliveryAsync(dto, donorId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("post/{postId}")]
    public async Task<IActionResult> GetByPost(string postId)
    {
        var record = await _deliveryService.GetByPostIdAsync(postId);
        if (record == null) return NotFound(new { message = "No hay entrega registrada para este post" });
        return Ok(record);
    }

    [HttpPost("{postId}/confirm-donor")]
    public async Task<IActionResult> ConfirmDonor(string postId)
    {
        var donorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        try
        {
            await _deliveryService.ConfirmByDonorAsync(postId, donorId);
            return Ok(new { message = "Entrega confirmada por el donante" });
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("{postId}/confirm-receiver")]
    public async Task<IActionResult> ConfirmReceiver(string postId)
    {
        var receiverId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        try
        {
            await _deliveryService.ConfirmByReceiverAsync(postId, receiverId);
            return Ok(new { message = "Recepción confirmada. ¡Donación completada!" });
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("history")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetHistory() =>
        Ok(await _deliveryService.GetAllCompletedAsync());
}