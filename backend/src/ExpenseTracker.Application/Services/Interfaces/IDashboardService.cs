using ExpenseTracker.Application.DTOs.Dashboard;

namespace ExpenseTracker.Application.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
}
