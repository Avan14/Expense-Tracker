using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Application.DTOs.SavingsGoals;
using ExpenseTracker.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/savings-goals")]
[Authorize]
public class SavingsGoalsController(ISavingsGoalService savingsGoalService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SavingsGoalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SavingsGoalDto>>> GetAll(CancellationToken cancellationToken)
    {
        var goals = await savingsGoalService.GetSavingsGoalsAsync(currentUser.UserId, cancellationToken);
        return Ok(goals);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SavingsGoalDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SavingsGoalDto>> Create([FromBody] CreateSavingsGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await savingsGoalService.CreateSavingsGoalAsync(currentUser.UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = goal.Id }, goal);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SavingsGoalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SavingsGoalDto>> Update(Guid id, [FromBody] UpdateSavingsGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await savingsGoalService.UpdateSavingsGoalAsync(currentUser.UserId, id, request, cancellationToken);
        return Ok(goal);
    }

    [HttpPost("{id:guid}/contribute")]
    [ProducesResponseType(typeof(SavingsGoalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SavingsGoalDto>> Contribute(Guid id, [FromBody] ContributeSavingsGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await savingsGoalService.ContributeAsync(currentUser.UserId, id, request, cancellationToken);
        return Ok(goal);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await savingsGoalService.DeleteSavingsGoalAsync(currentUser.UserId, id, cancellationToken);
        return NoContent();
    }
}
