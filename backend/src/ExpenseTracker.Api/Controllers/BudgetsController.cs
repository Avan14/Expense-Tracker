using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Application.DTOs.Budgets;
using ExpenseTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController(IBudgetService budgetService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BudgetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BudgetDto>>> GetAll([FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var y = year ?? now.Year;
        var m = month ?? now.Month;
        var budgets = await budgetService.GetBudgetsAsync(currentUser.UserId, y, m, cancellationToken);
        return Ok(budgets);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<BudgetDto>> Create([FromBody] CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var budget = await budgetService.CreateBudgetAsync(currentUser.UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { year = budget.Year, month = budget.Month }, budget);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BudgetDto>> Update(Guid id, [FromBody] UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var budget = await budgetService.UpdateBudgetAsync(currentUser.UserId, id, request, cancellationToken);
        return Ok(budget);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await budgetService.DeleteBudgetAsync(currentUser.UserId, id, cancellationToken);
        return NoContent();
    }
}
