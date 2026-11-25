using ExpenseTracker.Application.DTOs.Transactions;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Exceptions;

namespace ExpenseTracker.UnitTests;

public class TransactionServiceTests
{
    [Fact]
    public async Task GetTransactions_ShouldFilterSortPaginateAndIsolateUsers()
    {
        await using var db = TestDbContextFactory.Create();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var foodCat = new Category { Name = "Food", Type = TransactionType.Expense, IsDefault = true };
        var salaryCat = new Category { Name = "Salary", Type = TransactionType.Income, IsDefault = true };
        db.Categories.AddRange(foodCat, salaryCat);

        db.Transactions.AddRange(
            new Transaction { UserId = user1, Title = "Lunch", Amount = 20m, Type = TransactionType.Expense, CategoryId = foodCat.Id, TransactionDate = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { UserId = user1, Title = "Dinner", Amount = 50m, Type = TransactionType.Expense, CategoryId = foodCat.Id, TransactionDate = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { UserId = user1, Title = "Paycheck", Amount = 3000m, Type = TransactionType.Income, CategoryId = salaryCat.Id, TransactionDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { UserId = user2, Title = "User2 Lunch", Amount = 99m, Type = TransactionType.Expense, CategoryId = foodCat.Id, TransactionDate = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc) }
        );
        await db.SaveChangesAsync();

        var service = new TransactionService(db);

        var result = await service.GetTransactionsAsync(user1, new TransactionQueryParameters
        {
            Type = TransactionType.Expense,
            SortBy = "amount",
            SortDirection = "desc",
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Dinner", result.Items[0].Title);
        Assert.Equal("Lunch", result.Items[1].Title);
    }

    [Fact]
    public async Task CreateTransaction_WithMismatchedCategoryType_ShouldThrowBadRequest()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var salaryCat = new Category { Name = "Salary", Type = TransactionType.Income, IsDefault = true };
        db.Categories.Add(salaryCat);
        await db.SaveChangesAsync();

        var service = new TransactionService(db);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateTransactionAsync(userId, new CreateTransactionRequest(
                "Invalid Expense", 100m, TransactionType.Expense, salaryCat.Id, null, DateTime.UtcNow, PaymentMethod.Cash, null)));
    }
}
