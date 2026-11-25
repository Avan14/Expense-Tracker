namespace ExpenseTracker.Application.DTOs.Budgets;

public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    string CategoryIcon,
    decimal BudgetAmount,
    decimal SpentAmount,
    decimal RemainingAmount,
    decimal UtilizationPercentage,
    int Year,
    int Month);

public record CreateBudgetRequest(
    Guid CategoryId,
    decimal Amount,
    int Year,
    int Month);

public record UpdateBudgetRequest(decimal Amount);
