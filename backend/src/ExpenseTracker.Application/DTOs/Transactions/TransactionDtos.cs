using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.DTOs.Transactions;

public record TransactionDto(
    Guid Id,
    string Title,
    decimal Amount,
    TransactionType Type,
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    string CategoryIcon,
    string? Description,
    DateTime TransactionDate,
    PaymentMethod PaymentMethod,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateTransactionRequest(
    string Title,
    decimal Amount,
    TransactionType Type,
    Guid CategoryId,
    string? Description,
    DateTime TransactionDate,
    PaymentMethod PaymentMethod,
    string? Notes);

public record UpdateTransactionRequest(
    string Title,
    decimal Amount,
    TransactionType Type,
    Guid CategoryId,
    string? Description,
    DateTime TransactionDate,
    PaymentMethod PaymentMethod,
    string? Notes);

public class TransactionQueryParameters
{
    public string? Search { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? CategoryId { get; set; }
    public TransactionType? Type { get; set; }
    public string SortBy { get; set; } = "date";
    public string SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}
