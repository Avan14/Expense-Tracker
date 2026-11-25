using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Application.DTOs.Categories;
using ExpenseTracker.Application.Services.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services;

public class CategoryService(IAppDbContext dbContext) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(Guid userId, TransactionType? type = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == null || c.UserId == userId);

        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }

        var categories = await query
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Type,
                c.Icon,
                c.Color,
                c.IsDefault,
                c.Transactions.Count(t => t.UserId == userId)))
            .ToListAsync(cancellationToken);

        return categories;
    }

    public async Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name.Trim();
        var exists = await dbContext.Categories
            .AnyAsync(c => (c.UserId == null || c.UserId == userId)
                           && c.Type == request.Type
                           && c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A {request.Type} category named '{trimmedName}' already exists.");
        }

        var category = new Category
        {
            Name = trimmedName,
            Type = request.Type,
            Icon = request.Icon.Trim(),
            Color = request.Color.Trim(),
            IsDefault = false,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.Type, category.Icon, category.Color, category.IsDefault, 0);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid userId, Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        if (category.IsDefault || category.UserId != userId)
        {
            throw new ForbiddenException("Default or other users' categories cannot be modified.");
        }

        var trimmedName = request.Name.Trim();
        var duplicate = await dbContext.Categories
            .AnyAsync(c => c.Id != categoryId
                           && (c.UserId == null || c.UserId == userId)
                           && c.Type == category.Type
                           && c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (duplicate)
        {
            throw new ConflictException($"A {category.Type} category named '{trimmedName}' already exists.");
        }

        category.Name = trimmedName;
        category.Icon = request.Icon.Trim();
        category.Color = request.Color.Trim();
        category.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var txCount = await dbContext.Transactions.CountAsync(t => t.CategoryId == categoryId && t.UserId == userId, cancellationToken);
        return new CategoryDto(category.Id, category.Name, category.Type, category.Icon, category.Color, category.IsDefault, txCount);
    }

    public async Task DeleteCategoryAsync(Guid userId, Guid categoryId, Guid? reassignToCategoryId = null, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        if (category.IsDefault || category.UserId != userId)
        {
            throw new ForbiddenException("Default or other users' categories cannot be deleted.");
        }

        var referencedTransactions = await dbContext.Transactions
            .Where(t => t.CategoryId == categoryId && t.UserId == userId)
            .ToListAsync(cancellationToken);

        if (referencedTransactions.Count > 0)
        {
            if (!reassignToCategoryId.HasValue)
            {
                throw new ConflictException(
                    $"Cannot delete category '{category.Name}' because it is referenced by {referencedTransactions.Count} transaction(s). Provide a fallback category to reassign them.");
            }

            if (reassignToCategoryId.Value == categoryId)
            {
                throw new BadRequestException("Fallback category cannot be the same as the category being deleted.");
            }

            var targetCategory = await dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == reassignToCategoryId.Value && (c.UserId == null || c.UserId == userId), cancellationToken)
                ?? throw new NotFoundException("Fallback category not found.");

            if (targetCategory.Type != category.Type)
            {
                throw new BadRequestException($"Fallback category must be of type {category.Type}.");
            }

            foreach (var tx in referencedTransactions)
            {
                tx.CategoryId = targetCategory.Id;
                tx.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        var referencedBudgets = await dbContext.Budgets
            .Where(b => b.CategoryId == categoryId && b.UserId == userId)
            .ToListAsync(cancellationToken);

        if (referencedBudgets.Count > 0)
        {
            dbContext.Budgets.RemoveRange(referencedBudgets);
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
