using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Application.DTOs.Auth;
using ExpenseTracker.Application.DTOs.Categories;
using ExpenseTracker.Application.DTOs.Dashboard;
using ExpenseTracker.Application.DTOs.Transactions;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.IntegrationTests;

public class ApiEndToEndTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task AuthFlow_RegisterRefreshRotationAndReuseDetection_ShouldWork()
    {
        var client = factory.CreateClient();
        var email = $"user_{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, "Password123", "Jane", "Doe", "USD"));
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));

        // Rotate refresh token
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(rotated);
        Assert.NotEqual(auth.RefreshToken, rotated.RefreshToken);

        // Reuse old refresh token should fail with 401
        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task FinanceFlow_TransactionsDashboardAndRoleAuthorization_ShouldWork()
    {
        var client = factory.CreateClient();
        var email = $"finance_{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, "Password123", "John", "Smith", "USD"));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Non-admin user cannot access admin endpoint
        var adminResp = await client.GetAsync("/api/admin/stats");
        Assert.Equal(HttpStatusCode.Forbidden, adminResp.StatusCode);

        // Fetch default categories
        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories", JsonOptions);
        Assert.NotNull(categories);
        Assert.True(categories.Count >= 15);

        var foodCat = categories.First(c => c.Name == "Food");
        var salaryCat = categories.First(c => c.Name == "Salary");

        // Create income & expense
        var now = DateTime.UtcNow;
        var incResp = await client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
            "Salary", 4000m, TransactionType.Income, salaryCat.Id, "Monthly pay", now, PaymentMethod.BankTransfer, null));
        Assert.Equal(HttpStatusCode.Created, incResp.StatusCode);

        var expResp = await client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
            "Dinner", 120m, TransactionType.Expense, foodCat.Id, "Steakhouse", now, PaymentMethod.CreditCard, null));
        Assert.Equal(HttpStatusCode.Created, expResp.StatusCode);

        // Verify dashboard overview
        var overview = await client.GetFromJsonAsync<DashboardOverviewDto>($"/api/dashboard/overview?year={now.Year}&month={now.Month}", JsonOptions);
        Assert.NotNull(overview);
        Assert.Equal(4000m, overview.TotalIncome);
        Assert.Equal(120m, overview.TotalExpenses);
        Assert.Equal(3880m, overview.SavingsAmount);
    }
}
