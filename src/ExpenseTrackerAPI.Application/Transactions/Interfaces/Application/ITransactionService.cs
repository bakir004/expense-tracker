using ErrorOr;
using ExpenseTrackerAPI.Contracts.Transactions;

namespace ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;

/// <summary>
/// Service interface for transaction business operations.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Gets all transactions for a specific user with pagination.
    /// </summary>
    /// <param name="userId">The user ID to get transactions for</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of transactions</returns>
    Task<ErrorOr<GetAllTransactionsResponse>> GetAllTransactionsAsync(
        int userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
