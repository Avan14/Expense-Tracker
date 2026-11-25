using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Other;
    public string? Notes { get; set; }
}
