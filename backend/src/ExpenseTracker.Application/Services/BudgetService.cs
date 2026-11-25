using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Application.DTOs.Budgets;
using ExpenseTracker.Application.Services.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services;

public class BudgetService(IAppDbContext dbContext) : IBudgetService
{
    public async Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var budgets = await dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .OrderBy(b => b.Category.Name)
            .ToListAsync(cancellationToken);

        var spentByCategory = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                        && t.Type == TransactionType.Expense
                        && t.TransactionDate >= startDate
                        && t.TransactionDate < endDate)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total, cancellationToken);

        return budgets.Select(b =>
        {
            var spent = spentByCategory.GetValueOrDefault(b.CategoryId, 0m);
            var remaining = b.Amount - spent;
            var pct = b.Amount > 0 ? Math.Round((spent / b.Amount) * 100m, 2) : 0m;

            return new BudgetDto(
                b.Id,
                b.CategoryId,
                b.Category.Name,
                b.Category.Color,
                b.Category.Icon,
                b.Amount,
                spent,
                remaining,
                pct,
                b.Year,
                b.Month);
        }).ToList();
    }

    public async Task<BudgetDto> CreateBudgetAsync(Guid userId, CreateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && (c.UserId == null || c.UserId == userId), cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        if (category.Type != TransactionType.Expense)
        {
            throw new BadRequestException("Budgets can only be created for Expense categories.");
        }

        var exists = await dbContext.Budgets
            .AnyAsync(b => b.UserId == userId
                           && b.CategoryId == request.CategoryId
                           && b.Year == request.Year
                           && b.Month == request.Month, cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A budget for '{category.Name}' already exists for {request.Year}-{request.Month:D2}.");
        }

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Year = request.Year,
            Month = request.Month,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);

        var startDate = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);
        var spent = await dbContext.Transactions
            .Where(t => t.UserId == userId
                        && t.Type == TransactionType.Expense
                        && t.CategoryId == request.CategoryId
                        && t.TransactionDate >= startDate
                        && t.TransactionDate < endDate)
            .SumAsync(t => t.Amount, cancellationToken);

        var pct = budget.Amount > 0 ? Math.Round((spent / budget.Amount) * 100m, 2) : 0m;
        return new BudgetDto(budget.Id, category.Id, category.Name, category.Color, category.Icon, budget.Amount, spent, budget.Amount - spent, pct, budget.Year, budget.Month);
    }

    public async Task<BudgetDto> UpdateBudgetAsync(Guid userId, Guid budgetId, UpdateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var budget = await dbContext.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Budget not found.");

        budget.Amount = request.Amount;
        budget.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var startDate = new DateTime(budget.Year, budget.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);
        var spent = await dbContext.Transactions
            .Where(t => t.UserId == userId
                        && t.Type == TransactionType.Expense
                        && t.CategoryId == budget.CategoryId
                        && t.TransactionDate >= startDate
                        && t.TransactionDate < endDate)
            .SumAsync(t => t.Amount, cancellationToken);

        var pct = budget.Amount > 0 ? Math.Round((spent / budget.Amount) * 100m, 2) : 0m;
        return new BudgetDto(budget.Id, budget.CategoryId, budget.Category.Name, budget.Category.Color, budget.Category.Icon, budget.Amount, spent, budget.Amount - spent, pct, budget.Year, budget.Month);
    }

    public async Task DeleteBudgetAsync(Guid userId, Guid budgetId, CancellationToken cancellationToken = default)
    {
        var budget = await dbContext.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Budget not found.");

        dbContext.Budgets.Remove(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
