using ExpenseTracker.Application.DTOs.Common;
using ExpenseTracker.Application.DTOs.Transactions;

namespace ExpenseTracker.Application.Services.Interfaces;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionQueryParameters query, CancellationToken cancellationToken = default);
    Task<TransactionDto> GetTransactionByIdAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default);
    Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken = default);
    Task<TransactionDto> UpdateTransactionAsync(Guid userId, Guid transactionId, UpdateTransactionRequest request, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default);
}
