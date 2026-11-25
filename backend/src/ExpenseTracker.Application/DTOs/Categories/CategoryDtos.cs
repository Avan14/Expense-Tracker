using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.DTOs.Categories;

public record CategoryDto(
    Guid Id,
    string Name,
    TransactionType Type,
    string Icon,
    string Color,
    bool IsDefault,
    int TransactionCount);

public record CreateCategoryRequest(
    string Name,
    TransactionType Type,
    string Icon = "tag",
    string Color = "#6366f1");

public record UpdateCategoryRequest(
    string Name,
    string Icon,
    string Color);

public record DeleteCategoryRequest(Guid? ReassignToCategoryId = null);
