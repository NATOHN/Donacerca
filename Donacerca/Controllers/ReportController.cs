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
    public async Task<IActionResult> GetDashboard() =>
        Ok(await _reportService.GetDashboardStatsAsync());

    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend([FromQuery] string period = "week") =>
        Ok(await _reportService.GetTrendAsync(period));
}