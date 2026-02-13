using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Application;

/// <summary>
/// Service interface for transaction group business operations.
/// </summary>
public interface ITransactionGroupService
{
    /// <summary>
    /// Get a transaction group by ID.
    /// Verifies that the transaction group belongs to the specified user.
    /// </summary>
    /// <param name="id">The transaction group ID</param>
    /// <param name="userId">The user ID for authorization</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transaction group if found and authorized, or error</returns>
    Task<ErrorOr<TransactionGroup>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Get all transaction groups for a specific user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transaction groups belonging to the user</returns>
    Task<ErrorOr<List<TransactionGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Create a new transaction group for a user.
    /// </summary>
    /// <param name="userId">The user ID who owns the transaction group</param>
    /// <param name="name">The name of the transaction group</param>
    /// <param name="description">Optional description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created transaction group</returns>
    Task<ErrorOr<TransactionGroup>> CreateAsync(
        int userId,
        string name,
        string? description,
        CancellationToken cancellationToken);

    /// <summary>
    /// Update an existing transaction group.
    /// Verifies that the transaction group belongs to the specified user.
    /// </summary>
    /// <param name="id">The transaction group ID to update</param>
    /// <param name="userId">The user ID for authorization</param>
    /// <param name="name">The updated name</param>
    /// <param name="description">The updated description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated transaction group</returns>
    Task<ErrorOr<TransactionGroup>> UpdateAsync(
        int id,
        int userId,
        string name,
        string? description,
        CancellationToken cancellationToken);

    /// <summary>
    /// Delete a transaction group.
    /// Verifies that the transaction group belongs to the specified user.
    /// </summary>
    /// <param name="id">The transaction group ID to delete</param>
    /// <param name="userId">The user ID for authorization</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deleted result or error</returns>
    Task<ErrorOr<Deleted>> DeleteAsync(int id, int userId, CancellationToken cancellationToken);
}
