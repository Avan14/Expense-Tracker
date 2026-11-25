using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(UserManager<ApplicationUser> userManager, IAppDbContext dbContext) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetPlatformStats(CancellationToken cancellationToken)
    {
        var totalUsers = await userManager.Users.CountAsync(cancellationToken);
        var totalTransactions = await dbContext.Transactions.CountAsync(cancellationToken);
        var totalCategories = await dbContext.Categories.CountAsync(cancellationToken);

        return Ok(new
        {
            totalUsers,
            totalTransactions,
            totalCategories,
            timestampUtc = DateTime.UtcNow
        });
    }
}
