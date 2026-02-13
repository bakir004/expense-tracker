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
    Task<ErrorOr<Transaction>> GetByIdAsync(int id, CancellationToken cancellationToken);
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
}
