using Donacerca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donacerca.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class ReportController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string? categoryId = null,
        [FromQuery] string? zone = null,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null)
    {
        DateTime? fromDate = string.IsNullOrEmpty(from) ? null : DateTime.Parse(from);
        DateTime? toDate = string.IsNullOrEmpty(to) ? null : DateTime.Parse(to).AddDays(1);

        return Ok(await _reportService.GetDashboardStatsAsync(categoryId, zone, fromDate, toDate));
    }

    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend([FromQuery] string period = "week") =>
        Ok(await _reportService.GetTrendAsync(period));
}