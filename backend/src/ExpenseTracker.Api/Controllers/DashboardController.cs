using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Application.DTOs.Dashboard;
using ExpenseTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(IDashboardService dashboardService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview([FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var y = year ?? now.Year;
        var m = Math.Clamp(month ?? now.Month, 1, 12);

        var overview = await dashboardService.GetOverviewAsync(currentUser.UserId, y, m, cancellationToken);
        return Ok(overview);
    }
}
