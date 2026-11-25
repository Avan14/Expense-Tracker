using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Application.DTOs.SavingsGoals;
using ExpenseTracker.Application.Services.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services;

public class SavingsGoalService(IAppDbContext dbContext) : ISavingsGoalService
{
    public async Task<IReadOnlyList<SavingsGoalDto>> GetSavingsGoalsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var goals = await dbContext.SavingsGoals
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return goals.Select(MapToDto).ToList();
    }

    public async Task<SavingsGoalDto> CreateSavingsGoalAsync(Guid userId, CreateSavingsGoalRequest request, CancellationToken cancellationToken = default)
    {
        var goal = new SavingsGoal
        {
            UserId = userId,
            Name = request.Name.Trim(),
            TargetAmount = request.TargetAmount,
            CurrentAmount = request.CurrentAmount,
            TargetDate = request.TargetDate.HasValue ? DateTime.SpecifyKind(request.TargetDate.Value, DateTimeKind.Utc) : null,
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.SavingsGoals.Add(goal);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(goal);
    }

    public async Task<SavingsGoalDto> UpdateSavingsGoalAsync(Guid userId, Guid goalId, UpdateSavingsGoalRequest request, CancellationToken cancellationToken = default)
    {
        var goal = await dbContext.SavingsGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Savings goal not found.");

        goal.Name = request.Name.Trim();
        goal.TargetAmount = request.TargetAmount;
        goal.CurrentAmount = request.CurrentAmount;
        goal.TargetDate = request.TargetDate.HasValue ? DateTime.SpecifyKind(request.TargetDate.Value, DateTimeKind.Utc) : null;
        goal.Notes = request.Notes?.Trim();
        goal.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(goal);
    }

    public async Task<SavingsGoalDto> ContributeAsync(Guid userId, Guid goalId, ContributeSavingsGoalRequest request, CancellationToken cancellationToken = default)
    {
        var goal = await dbContext.SavingsGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Savings goal not found.");

        var updatedAmount = goal.CurrentAmount + request.Amount;
        if (updatedAmount < 0)
        {
            throw new BadRequestException("Contribution results in a negative saved amount.");
        }

        goal.CurrentAmount = updatedAmount;
        goal.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(goal);
    }

    public async Task DeleteSavingsGoalAsync(Guid userId, Guid goalId, CancellationToken cancellationToken = default)
    {
        var goal = await dbContext.SavingsGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Savings goal not found.");

        dbContext.SavingsGoals.Remove(goal);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SavingsGoalDto MapToDto(SavingsGoal goal)
    {
        var progress = goal.TargetAmount > 0
            ? Math.Min(100m, Math.Round((goal.CurrentAmount / goal.TargetAmount) * 100m, 2))
            : 0m;

        return new SavingsGoalDto(
            goal.Id,
            goal.Name,
            goal.TargetAmount,
            goal.CurrentAmount,
            progress,
            goal.TargetDate,
            goal.Notes,
            goal.IsCompleted,
            goal.CreatedAtUtc);
    }
}
