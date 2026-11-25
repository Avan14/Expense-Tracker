using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.UnitTests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetOverview_ShouldCalculateIncomeExpensesSavingsRateAndBudgetUtilization()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        var salaryCat = new Category { Name = "Salary", Type = TransactionType.Income, IsDefault = true, Color = "#10b981", Icon = "briefcase" };
        var foodCat = new Category { Name = "Food", Type = TransactionType.Expense, IsDefault = true, Color = "#f97316", Icon = "utensils" };
        db.Categories.AddRange(salaryCat, foodCat);

        // Current month: Nov 2026
        db.Transactions.AddRange(
            new Transaction { UserId = userId, Title = "Salary", Amount = 5000m, Type = TransactionType.Income, CategoryId = salaryCat.Id, TransactionDate = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { UserId = userId, Title = "Groceries", Amount = 1000m, Type = TransactionType.Expense, CategoryId = foodCat.Id, TransactionDate = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc) },
            // Previous month: Oct 2026
            new Transaction { UserId = userId, Title = "Salary Oct", Amount = 4000m, Type = TransactionType.Income, CategoryId = salaryCat.Id, TransactionDate = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { UserId = userId, Title = "Food Oct", Amount = 800m, Type = TransactionType.Expense, CategoryId = foodCat.Id, TransactionDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc) }
        );

        db.Budgets.Add(new Budget { UserId = userId, CategoryId = foodCat.Id, Amount = 1200m, Year = 2026, Month = 11 });
        await db.SaveChangesAsync();

        var service = new DashboardService(db);
        var overview = await service.GetOverviewAsync(userId, 2026, 11);

        Assert.Equal(5000m, overview.TotalIncome);
        Assert.Equal(1000m, overview.TotalExpenses);
        Assert.Equal(4000m, overview.SavingsAmount);
        Assert.Equal(80m, overview.MonthlySavingsRate);
        Assert.Equal(7200m, overview.CurrentBalance); // (5000+4000) - (1000+800)
        Assert.Equal(25m, overview.ComparisonWithPreviousMonth.IncomeChangePercentage);
        Assert.Single(overview.SpendingByCategory);
        Assert.Single(overview.BudgetUtilization);
        Assert.Equal(83.33m, overview.BudgetUtilization[0].UtilizationPercentage);
        Assert.Equal(6, overview.MonthlyCashflow.Count);
    }
}
