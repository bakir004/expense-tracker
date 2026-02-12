using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for transaction data operations.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Gets all transactions for a specific user with pagination.
    /// </summary>
    /// <param name="userId">The user ID to get transactions for</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of transactions with total count</returns>
    Task<ErrorOr<(IEnumerable<Transaction> Transactions, int TotalCount)>> GetAllByUserIdAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by ID for a specific user.
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="userId">User ID (for security)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction if found</returns>
    Task<ErrorOr<Transaction>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="transaction">Transaction to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created transaction with ID</returns>
    Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    /// <param name="transaction">Transaction to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated transaction</returns>
    Task<ErrorOr<Transaction>> UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a transaction by ID for a specific user.
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="userId">User ID (for security)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a transaction exists for a specific user.
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="userId">User ID (for security)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if transaction exists</returns>
    Task<ErrorOr<bool>> ExistsAsync(int id, int userId, CancellationToken cancellationToken = default);
}
