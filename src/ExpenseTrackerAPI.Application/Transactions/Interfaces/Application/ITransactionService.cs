using ErrorOr;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;

/// <summary>
/// Service interface for transaction business operations.
/// </summary>
public interface ITransactionService
{
    Task<ErrorOr<Transaction>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken);
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
    Task<ErrorOr<Deleted>> DeleteAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Get transactions for a user with optional filters, sorting, and pagination.
    /// </summary>
    /// <param name="userId">The user ID to filter transactions by</param>
    /// <param name="filter">Filter, sort, and pagination options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated filter response with transactions and pagination metadata</returns>
    Task<ErrorOr<TransactionFilterResponse>> GetByUserIdWithFilterAsync(
        int userId,
        TransactionFilter filter,
        CancellationToken cancellationToken);
}
