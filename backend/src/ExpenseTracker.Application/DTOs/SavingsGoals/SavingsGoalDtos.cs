namespace ExpenseTracker.Application.DTOs.SavingsGoals;

public record SavingsGoalDto(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal ProgressPercentage,
    DateTime? TargetDate,
    string? Notes,
    bool IsCompleted,
    DateTime CreatedAtUtc);

public record CreateSavingsGoalRequest(
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount = 0,
    DateTime? TargetDate = null,
    string? Notes = null);

public record UpdateSavingsGoalRequest(
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    DateTime? TargetDate,
    string? Notes);

public record ContributeSavingsGoalRequest(decimal Amount);
