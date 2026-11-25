using ExpenseTracker.Application.DTOs.Transactions;

namespace ExpenseTracker.Application.DTOs.Dashboard;

public record DashboardOverviewDto(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal CurrentBalance,
    decimal SavingsAmount,
    decimal MonthlySavingsRate,
    PeriodComparisonDto ComparisonWithPreviousMonth,
    IReadOnlyList<CategorySpendingDto> SpendingByCategory,
    IReadOnlyList<MonthlyCashflowDto> MonthlyCashflow,
    IReadOnlyList<BudgetUtilizationDto> BudgetUtilization,
    IReadOnlyList<TransactionDto> RecentTransactions);

public record PeriodComparisonDto(
    decimal PreviousMonthIncome,
    decimal PreviousMonthExpenses,
    decimal PreviousMonthSavings,
    decimal? IncomeChangePercentage,
    decimal? ExpenseChangePercentage,
    decimal? SavingsChangePercentage);

public record CategorySpendingDto(
    Guid CategoryId,
    string CategoryName,
    string Color,
    string Icon,
    decimal Amount,
    decimal Percentage);

public record MonthlyCashflowDto(
    int Year,
    int Month,
    string Label,
    decimal Income,
    decimal Expenses,
    decimal Savings);

public record BudgetUtilizationDto(
    Guid BudgetId,
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    decimal BudgetAmount,
    decimal SpentAmount,
    decimal UtilizationPercentage,
    bool IsOverBudget);
