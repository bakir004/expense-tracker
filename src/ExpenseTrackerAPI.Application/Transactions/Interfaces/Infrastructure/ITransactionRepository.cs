using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Contracts.Transactions;

namespace ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for transaction persistence operations.
/// Defined in Application layer, implemented in Infrastructure layer.
/// </summary>
public interface ITransactionRepository
{
    Task<ErrorOr<Transaction>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken);
    /// <summary>
    /// Create a new transaction, update balance, and record in history
    /// </summary>
    Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Update a transaction, recalculate subsequent balances
    /// </summary>
    Task<ErrorOr<Transaction>> UpdateAsync(Transaction oldTransaction, Transaction transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a transaction, recalculate subsequent balances
    /// </summary>
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Get transactions for a user with optional filters, sorting, and pagination.
    /// </summary>
    /// <param name="userId">The user ID to filter transactions by</param>
    /// <param name="filter">Filter, sort, and pagination options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching transactions</returns>
    Task<ErrorOr<List<Transaction>>> GetByUserIdWithFilterAsync(
        int userId,
        TransactionFilter filter,
        CancellationToken cancellationToken);
}
