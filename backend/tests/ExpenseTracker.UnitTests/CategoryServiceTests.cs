using ExpenseTracker.Application.DTOs.Categories;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Exceptions;

namespace ExpenseTracker.UnitTests;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateCategory_ShouldSucceed_AndRejectDuplicateName()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new CategoryService(db);
        var userId = Guid.NewGuid();

        var created = await service.CreateCategoryAsync(userId, new CreateCategoryRequest("Subscriptions", TransactionType.Expense, "repeat", "#ec4899"));
        Assert.Equal("Subscriptions", created.Name);
        Assert.False(created.IsDefault);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateCategoryAsync(userId, new CreateCategoryRequest("subscriptions", TransactionType.Expense, "repeat", "#ec4899")));
    }

    [Fact]
    public async Task DeleteCategory_DefaultCategory_ShouldThrowForbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var defaultCat = new Category { Name = "Food", Type = TransactionType.Expense, IsDefault = true, UserId = null };
        db.Categories.Add(defaultCat);
        await db.SaveChangesAsync();

        var service = new CategoryService(db);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.DeleteCategoryAsync(Guid.NewGuid(), defaultCat.Id));
    }

    [Fact]
    public async Task DeleteCategory_WithTransactions_RequiresReassignmentAndReassigns()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        var fallbackCat = new Category { Name = "Other expenses", Type = TransactionType.Expense, IsDefault = true, UserId = null };
        var customCat = new Category { Name = "Coffee", Type = TransactionType.Expense, IsDefault = false, UserId = userId };
        db.Categories.AddRange(fallbackCat, customCat);
        db.Transactions.Add(new Transaction
        {
            UserId = userId,
            Title = "Latte",
            Amount = 5.50m,
            Type = TransactionType.Expense,
            CategoryId = customCat.Id,
            TransactionDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CategoryService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeleteCategoryAsync(userId, customCat.Id));

        await service.DeleteCategoryAsync(userId, customCat.Id, fallbackCat.Id);

        Assert.Null(await db.Categories.FindAsync(customCat.Id));
        var tx = Assert.Single(db.Transactions.ToList());
        Assert.Equal(fallbackCat.Id, tx.CategoryId);
    }
}
