using System.Globalization;
using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Application.DTOs.Dashboard;
using ExpenseTracker.Application.DTOs.Transactions;
using ExpenseTracker.Application.Services.Interfaces;
using ExpenseTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services;

public class DashboardService(IAppDbContext dbContext) : IDashboardService
{
    public async Task<DashboardOverviewDto> GetOverviewAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default)
    {
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);
        var prevPeriodStart = periodStart.AddMonths(-1);
        var cashflowStart = periodStart.AddMonths(-5);

        // Current period totals
        var currentPeriodTxs = await dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.TransactionDate >= periodStart && t.TransactionDate < periodEnd)
            .ToListAsync(cancellationToken);

        var totalIncome = currentPeriodTxs.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpenses = currentPeriodTxs.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var savingsAmount = totalIncome - totalExpenses;
        var monthlySavingsRate = totalIncome > 0 ? Math.Round((savingsAmount / totalIncome) * 100m, 2) : 0m;

        // All-time cumulative balance up to periodEnd
        var allTimeIncome = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == TransactionType.Income && t.TransactionDate < periodEnd)
            .SumAsync(t => t.Amount, cancellationToken);

        var allTimeExpenses = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense && t.TransactionDate < periodEnd)
            .SumAsync(t => t.Amount, cancellationToken);

        var currentBalance = allTimeIncome - allTimeExpenses;

        // Previous month comparison
        var prevPeriodTxs = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.TransactionDate >= prevPeriodStart && t.TransactionDate < periodStart)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync(cancellationToken);

        var prevIncome = prevPeriodTxs.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var prevExpenses = prevPeriodTxs.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var prevSavings = prevIncome - prevExpenses;

        var comparison = new PeriodComparisonDto(
            prevIncome,
            prevExpenses,
            prevSavings,
            CalculateChangePercentage(totalIncome, prevIncome),
            CalculateChangePercentage(totalExpenses, prevExpenses),
            CalculateChangePercentage(savingsAmount, prevSavings));

        // Spending by category
        var spendingByCategory = currentPeriodTxs
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.Color, t.Category.Icon })
            .Select(g =>
            {
                var amt = g.Sum(t => t.Amount);
                var pct = totalExpenses > 0 ? Math.Round((amt / totalExpenses) * 100m, 2) : 0m;
                return new CategorySpendingDto(g.Key.CategoryId, g.Key.Name, g.Key.Color, g.Key.Icon, amt, pct);
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // Monthly income vs expenses (6 months ending at selected month)
        var cashflowTxs = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.TransactionDate >= cashflowStart && t.TransactionDate < periodEnd)
            .Select(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.Type, t.Amount })
            .ToListAsync(cancellationToken);

        var monthlyCashflow = Enumerable.Range(0, 6)
            .Select(offset => cashflowStart.AddMonths(offset))
            .Select(dt =>
            {
                var monthIncome = cashflowTxs
                    .Where(t => t.Year == dt.Year && t.Month == dt.Month && t.Type == TransactionType.Income)
                    .Sum(t => t.Amount);
                var monthExpenses = cashflowTxs
                    .Where(t => t.Year == dt.Year && t.Month == dt.Month && t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount);
                var label = dt.ToString("MMM yyyy", CultureInfo.InvariantCulture);

                return new MonthlyCashflowDto(dt.Year, dt.Month, label, monthIncome, monthExpenses, monthIncome - monthExpenses);
            })
            .ToList();

        // Budget utilization overview
        var budgets = await dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToListAsync(cancellationToken);

        var spentLookup = currentPeriodTxs
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var budgetUtilization = budgets
            .Select(b =>
            {
                var spent = spentLookup.GetValueOrDefault(b.CategoryId, 0m);
                var pct = b.Amount > 0 ? Math.Round((spent / b.Amount) * 100m, 2) : 0m;
                return new BudgetUtilizationDto(
                    b.Id,
                    b.CategoryId,
                    b.Category.Name,
                    b.Category.Color,
                    b.Amount,
                    spent,
                    pct,
                    spent > b.Amount);
            })
            .OrderByDescending(b => b.UtilizationPercentage)
            .ToList();

        // Recent transactions (top 5 overall for user)
        var recentTransactions = await dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Take(5)
            .Select(t => new TransactionDto(
                t.Id,
                t.Title,
                t.Amount,
                t.Type,
                t.CategoryId,
                t.Category.Name,
                t.Category.Color,
                t.Category.Icon,
                t.Description,
                t.TransactionDate,
                t.PaymentMethod,
                t.Notes,
                t.CreatedAtUtc,
                t.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new DashboardOverviewDto(
            year,
            month,
            totalIncome,
            totalExpenses,
            currentBalance,
            savingsAmount,
            monthlySavingsRate,
            comparison,
            spendingByCategory,
            monthlyCashflow,
            budgetUtilization,
            recentTransactions);
    }

    private static decimal? CalculateChangePercentage(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0m : null;
        }

        return Math.Round(((current - previous) / Math.Abs(previous)) * 100m, 2);
    }
}
