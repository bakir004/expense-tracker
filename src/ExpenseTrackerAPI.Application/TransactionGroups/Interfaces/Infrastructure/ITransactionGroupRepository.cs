using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for transaction group persistence operations.
/// Defined in Application layer, implemented in Infrastructure layer.
/// </summary>
public interface ITransactionGroupRepository
{
    /// <summary>
    /// Get a transaction group by ID.
    /// </summary>
    /// <param name="id">The transaction group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transaction group if found, or NotFound error</returns>
    Task<ErrorOr<TransactionGroup>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Get all transaction groups for a specific user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transaction groups belonging to the user</returns>
    Task<ErrorOr<List<TransactionGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Create a new transaction group.
    /// </summary>
    /// <param name="transactionGroup">The transaction group to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created transaction group with ID populated</returns>
    Task<ErrorOr<TransactionGroup>> CreateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken);

    /// <summary>
    /// Update an existing transaction group.
    /// </summary>
    /// <param name="transactionGroup">The transaction group with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated transaction group</returns>
    Task<ErrorOr<TransactionGroup>> UpdateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a transaction group by ID.
    /// </summary>
    /// <param name="id">The transaction group ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deleted result or NotFound error</returns>
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}
