using System.Security.Claims;
using Donacerca.DTOs;
using Donacerca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donacerca.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DonationPostController : ControllerBase
{
    private readonly DonationService _donationService;

    public DonationPostController(DonationService donationService)
    {
        _donationService = donationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailable([FromQuery] string? categoryId, [FromQuery] string? zone) =>
        Ok(await _donationService.GetAvailableAsync(categoryId, zone));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var post = await _donationService.GetByIdAsync(id);
        return post == null ? NotFound() : Ok(post);
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        return Ok(await _donationService.GetByDonorAsync(userId));
    }

    [HttpGet("receiver-stats")]
    [Authorize]
    public async Task<IActionResult> GetReceiverStats()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        return Ok(await _donationService.GetReceiverStatsAsync(userId));
    }

    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll() =>
        Ok(await _donationService.GetAllActiveAsync());

    [HttpGet("moderation")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetForModeration() =>
        Ok(await _donationService.GetAllForModerationAsync());

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateDonationPostDto dto)
    {
        var userId   = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "Usuario";
        return Ok(await _donationService.CreateAsync(dto, userId, userName));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDonationPostDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        try
        {
            var result = await _donationService.UpdateAsync(id, dto, userId);
            return result == null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex)   { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Deactivate(string id)
    {
        var userId  = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var isAdmin = User.IsInRole("admin");
        try
        {
            await _donationService.DeactivateAsync(id, userId, isAdmin);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/reactivate")]
    [Authorize]
    public async Task<IActionResult> Reactivate(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        try
        {
            await _donationService.ReactivateAsync(id, userId);
            return Ok(new { message = "Publicación reactivada" });
        }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex)   { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
    }
}