using ErrorOr;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;

/// <summary>
/// Service interface for transaction business operations.
/// </summary>
public interface ITransactionService
{
    Task<ErrorOr<Transaction>> GetByIdAsync(int id, CancellationToken cancellationToken);
    /// <summary>
    /// Create a new transaction
    /// </summary>
    Task<ErrorOr<Transaction>> CreateAsync(
        int userId,
        TransactionType transactionType,
        decimal amount,
        DateOnly date,
        string subject,
        string? notes,
        PaymentMethod paymentMethod,
        int? categoryId,
        int? transactionGroupId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Update an existing transaction
    /// </summary>
    Task<ErrorOr<Transaction>> UpdateAsync(
        int id,
        int userId,
        TransactionType transactionType,
        decimal amount,
        DateOnly date,
        string subject,
        string? notes,
        PaymentMethod paymentMethod,
        int? categoryId,
        int? transactionGroupId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Delete a transaction
    /// </summary>
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}
