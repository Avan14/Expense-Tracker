using ExpenseTracker.Application.DTOs.SavingsGoals;

namespace ExpenseTracker.Application.Services.Interfaces;

public interface ISavingsGoalService
{
    Task<IReadOnlyList<SavingsGoalDto>> GetSavingsGoalsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SavingsGoalDto> CreateSavingsGoalAsync(Guid userId, CreateSavingsGoalRequest request, CancellationToken cancellationToken = default);
    Task<SavingsGoalDto> UpdateSavingsGoalAsync(Guid userId, Guid goalId, UpdateSavingsGoalRequest request, CancellationToken cancellationToken = default);
    Task<SavingsGoalDto> ContributeAsync(Guid userId, Guid goalId, ContributeSavingsGoalRequest request, CancellationToken cancellationToken = default);
    Task DeleteSavingsGoalAsync(Guid userId, Guid goalId, CancellationToken cancellationToken = default);
}
