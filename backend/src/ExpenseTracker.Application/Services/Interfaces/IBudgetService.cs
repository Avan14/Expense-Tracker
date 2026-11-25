using ExpenseTracker.Application.DTOs.Budgets;

namespace ExpenseTracker.Application.Services.Interfaces;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
    Task<BudgetDto> CreateBudgetAsync(Guid userId, CreateBudgetRequest request, CancellationToken cancellationToken = default);
    Task<BudgetDto> UpdateBudgetAsync(Guid userId, Guid budgetId, UpdateBudgetRequest request, CancellationToken cancellationToken = default);
    Task DeleteBudgetAsync(Guid userId, Guid budgetId, CancellationToken cancellationToken = default);
}
