using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Application.DTOs.Common;
using ExpenseTracker.Application.DTOs.Transactions;
using ExpenseTracker.Application.Services.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services;

public class TransactionService(IAppDbContext dbContext) : ITransactionService
{
    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionQueryParameters query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var txQuery = dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            txQuery = txQuery.Where(t =>
                t.Title.ToLower().Contains(term) ||
                (t.Description != null && t.Description.ToLower().Contains(term)) ||
                (t.Notes != null && t.Notes.ToLower().Contains(term)) ||
                t.Category.Name.ToLower().Contains(term));
        }

        if (query.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(query.StartDate.Value.Date, DateTimeKind.Utc);
            txQuery = txQuery.Where(t => t.TransactionDate >= startUtc);
        }

        if (query.EndDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(query.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            txQuery = txQuery.Where(t => t.TransactionDate <= endUtc);
        }

        if (query.CategoryId.HasValue)
        {
            txQuery = txQuery.Where(t => t.CategoryId == query.CategoryId.Value);
        }

        if (query.Type.HasValue)
        {
            txQuery = txQuery.Where(t => t.Type == query.Type.Value);
        }

        var isAsc = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        txQuery = query.SortBy?.ToLowerInvariant() switch
        {
            "amount" => isAsc
                ? txQuery.OrderBy(t => t.Amount).ThenByDescending(t => t.TransactionDate)
                : txQuery.OrderByDescending(t => t.Amount).ThenByDescending(t => t.TransactionDate),
            _ => isAsc
                ? txQuery.OrderBy(t => t.TransactionDate).ThenBy(t => t.CreatedAtUtc)
                : txQuery.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedAtUtc)
        };

        var totalCount = await txQuery.CountAsync(cancellationToken);
        var items = await txQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new PagedResult<TransactionDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TransactionDto> GetTransactionByIdAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default)
    {
        var tx = await dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        return MapToDto(tx, tx.Category);
    }

    public async Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var category = await ValidateCategoryAsync(userId, request.CategoryId, request.Type, cancellationToken);

        var tx = new Transaction
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Amount = request.Amount,
            Type = request.Type,
            CategoryId = category.Id,
            Description = request.Description?.Trim(),
            TransactionDate = DateTime.SpecifyKind(request.TransactionDate, DateTimeKind.Utc),
            PaymentMethod = request.PaymentMethod,
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Transactions.Add(tx);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(tx, category);
    }

    public async Task<TransactionDto> UpdateTransactionAsync(Guid userId, Guid transactionId, UpdateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var tx = await dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        var category = await ValidateCategoryAsync(userId, request.CategoryId, request.Type, cancellationToken);

        tx.Title = request.Title.Trim();
        tx.Amount = request.Amount;
        tx.Type = request.Type;
        tx.CategoryId = category.Id;
        tx.Description = request.Description?.Trim();
        tx.TransactionDate = DateTime.SpecifyKind(request.TransactionDate, DateTimeKind.Utc);
        tx.PaymentMethod = request.PaymentMethod;
        tx.Notes = request.Notes?.Trim();
        tx.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(tx, category);
    }

    public async Task DeleteTransactionAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default)
    {
        var tx = await dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        dbContext.Transactions.Remove(tx);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> ValidateCategoryAsync(Guid userId, Guid categoryId, Domain.Enums.TransactionType txType, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId && (c.UserId == null || c.UserId == userId), cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        if (category.Type != txType)
        {
            throw new BadRequestException($"Category '{category.Name}' is for {category.Type} transactions, not {txType}.");
        }

        return category;
    }

    private static TransactionDto MapToDto(Transaction tx, Category category) =>
        new(
            tx.Id,
            tx.Title,
            tx.Amount,
            tx.Type,
            tx.CategoryId,
            category.Name,
            category.Color,
            category.Icon,
            tx.Description,
            tx.TransactionDate,
            tx.PaymentMethod,
            tx.Notes,
            tx.CreatedAtUtc,
            tx.UpdatedAtUtc);
}
