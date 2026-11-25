using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Infrastructure.Persistence;

public static class DataSeeder
{
    private static readonly (string Name, TransactionType Type, string Icon, string Color)[] DefaultCategories =
    [
        // Income
        ("Salary", TransactionType.Income, "briefcase", "#10b981"),
        ("Freelancing", TransactionType.Income, "laptop", "#06b6d4"),
        ("Investments", TransactionType.Income, "trending-up", "#3b82f6"),
        ("Business", TransactionType.Income, "building-2", "#8b5cf6"),
        ("Other income", TransactionType.Income, "plus-circle", "#14b8a6"),

        // Expenses
        ("Food", TransactionType.Expense, "utensils", "#f97316"),
        ("Transportation", TransactionType.Expense, "car", "#eab308"),
        ("Shopping", TransactionType.Expense, "shopping-bag", "#ec4899"),
        ("Housing", TransactionType.Expense, "home", "#6366f1"),
        ("Healthcare", TransactionType.Expense, "heart-pulse", "#ef4444"),
        ("Education", TransactionType.Expense, "graduation-cap", "#8b5cf6"),
        ("Entertainment", TransactionType.Expense, "film", "#a855f7"),
        ("Utilities", TransactionType.Expense, "zap", "#0ea5e9"),
        ("Travel", TransactionType.Expense, "plane", "#14b8a6"),
        ("Other expenses", TransactionType.Expense, "receipt", "#64748b")
    ];

    public static async Task SeedAsync(IServiceProvider serviceProvider, bool seedDemoData = false)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        foreach (var roleName in new[] { "User", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        var existingDefaults = await dbContext.Categories
            .Where(c => c.IsDefault && c.UserId == null)
            .Select(c => new { c.Name, c.Type })
            .ToListAsync();

        var newCategories = DefaultCategories
            .Where(dc => !existingDefaults.Any(e => e.Name == dc.Name && e.Type == dc.Type))
            .Select(dc => new Category
            {
                Name = dc.Name,
                Type = dc.Type,
                Icon = dc.Icon,
                Color = dc.Color,
                IsDefault = true,
                UserId = null,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (newCategories.Count > 0)
        {
            dbContext.Categories.AddRange(newCategories);
            await dbContext.SaveChangesAsync();
        }

        if (seedDemoData && !await userManager.Users.AnyAsync())
        {
            await SeedDemoUserAsync(dbContext, userManager);
        }
    }

    private static async Task SeedDemoUserAsync(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        var demoUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "demo@expensetracker.dev",
            Email = "demo@expensetracker.dev",
            FirstName = "Alex",
            LastName = "Morgan",
            CurrencyCode = "USD",
            MonthlySavingsTarget = 1500m,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(demoUser, "Demo@1234");
        if (!result.Succeeded) return;

        await userManager.AddToRoleAsync(demoUser, "User");

        var categories = await dbContext.Categories.Where(c => c.IsDefault).ToListAsync();
        Guid CatId(string name, TransactionType type) =>
            categories.First(c => c.Name == name && c.Type == type).Id;

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 12, 0, 0, DateTimeKind.Utc);
        var prevMonthStart = currentMonthStart.AddMonths(-1);

        var transactions = new List<Transaction>
        {
            new() { UserId = demoUser.Id, Title = "Monthly Salary", Amount = 5800m, Type = TransactionType.Income, CategoryId = CatId("Salary", TransactionType.Income), TransactionDate = currentMonthStart.AddDays(1), PaymentMethod = PaymentMethod.BankTransfer, Description = "Primary employment paycheck" },
            new() { UserId = demoUser.Id, Title = "UI/UX Freelance Project", Amount = 950m, Type = TransactionType.Income, CategoryId = CatId("Freelancing", TransactionType.Income), TransactionDate = currentMonthStart.AddDays(6), PaymentMethod = PaymentMethod.BankTransfer, Description = "Design system consultation" },
            new() { UserId = demoUser.Id, Title = "Apartment Rent", Amount = 1650m, Type = TransactionType.Expense, CategoryId = CatId("Housing", TransactionType.Expense), TransactionDate = currentMonthStart.AddDays(2), PaymentMethod = PaymentMethod.BankTransfer },
            new() { UserId = demoUser.Id, Title = "Whole Foods Groceries", Amount = 245.50m, Type = TransactionType.Expense, CategoryId = CatId("Food", TransactionType.Expense), TransactionDate = currentMonthStart.AddDays(4), PaymentMethod = PaymentMethod.CreditCard },
            new() { UserId = demoUser.Id, Title = "Electric & Internet", Amount = 165.00m, Type = TransactionType.Expense, CategoryId = CatId("Utilities", TransactionType.Expense), TransactionDate = currentMonthStart.AddDays(5), PaymentMethod = PaymentMethod.DebitCard },
            new() { UserId = demoUser.Id, Title = "Metro Pass & Fuel", Amount = 120.00m, Type = TransactionType.Expense, CategoryId = CatId("Transportation", TransactionType.Expense), TransactionDate = currentMonthStart.AddDays(7), PaymentMethod = PaymentMethod.DebitCard },
            new() { UserId = demoUser.Id, Title = "Streaming & Cinema", Amount = 48.00m, Type = TransactionType.Expense, CategoryId = CatId("Entertainment", TransactionType.Expense), TransactionDate = currentMonthStart.AddDays(9), PaymentMethod = PaymentMethod.CreditCard },

            // Previous month
            new() { UserId = demoUser.Id, Title = "Monthly Salary", Amount = 5800m, Type = TransactionType.Income, CategoryId = CatId("Salary", TransactionType.Income), TransactionDate = prevMonthStart.AddDays(1), PaymentMethod = PaymentMethod.BankTransfer },
            new() { UserId = demoUser.Id, Title = "Apartment Rent", Amount = 1650m, Type = TransactionType.Expense, CategoryId = CatId("Housing", TransactionType.Expense), TransactionDate = prevMonthStart.AddDays(2), PaymentMethod = PaymentMethod.BankTransfer },
            new() { UserId = demoUser.Id, Title = "Groceries & Dining", Amount = 410m, Type = TransactionType.Expense, CategoryId = CatId("Food", TransactionType.Expense), TransactionDate = prevMonthStart.AddDays(8), PaymentMethod = PaymentMethod.CreditCard },
            new() { UserId = demoUser.Id, Title = "Online Shopping", Amount = 320m, Type = TransactionType.Expense, CategoryId = CatId("Shopping", TransactionType.Expense), TransactionDate = prevMonthStart.AddDays(14), PaymentMethod = PaymentMethod.CreditCard }
        };

        dbContext.Transactions.AddRange(transactions);

        dbContext.Budgets.AddRange(
            new Budget { UserId = demoUser.Id, CategoryId = CatId("Food", TransactionType.Expense), Amount = 600m, Year = now.Year, Month = now.Month },
            new Budget { UserId = demoUser.Id, CategoryId = CatId("Housing", TransactionType.Expense), Amount = 1700m, Year = now.Year, Month = now.Month },
            new Budget { UserId = demoUser.Id, CategoryId = CatId("Entertainment", TransactionType.Expense), Amount = 150m, Year = now.Year, Month = now.Month }
        );

        dbContext.SavingsGoals.AddRange(
            new SavingsGoal { UserId = demoUser.Id, Name = "Emergency Fund", TargetAmount = 10000m, CurrentAmount = 6400m, TargetDate = now.AddMonths(8) },
            new SavingsGoal { UserId = demoUser.Id, Name = "Japan Vacation", TargetAmount = 4000m, CurrentAmount = 1500m, TargetDate = now.AddMonths(10) }
        );

        await dbContext.SaveChangesAsync();
    }
}
