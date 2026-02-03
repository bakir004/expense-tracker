using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Application.Transactions.Data;

namespace ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for transaction persistence operations.
/// Defined in Application layer, implemented in Infrastructure layer.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Get all transactions (admin/debug use)
    /// </summary>
    Task<ErrorOr<List<Transaction>>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get a single transaction by ID
    /// </summary>
    Task<ErrorOr<Transaction>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Get transactions for a user with optional filters and sorting (date first, then secondary sort).
    /// </summary>
    Task<ErrorOr<List<Transaction>>> GetByUserIdWithFiltersAsync(int userId, TransactionQueryOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Get all transactions for a user, ordered by date descending
    /// </summary>
    Task<ErrorOr<List<Transaction>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Get transactions for a user filtered by type
    /// </summary>
    Task<ErrorOr<List<Transaction>>> GetByUserIdAndTypeAsync(int userId, TransactionType type, CancellationToken cancellationToken);

    /// <summary>
    /// Get transactions for a user within a date range
    /// </summary>
    Task<ErrorOr<List<Transaction>>> GetByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Create a new transaction, update balance, and record in history
    /// </summary>
    Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Update a transaction, recalculate subsequent balances
    /// </summary>
    Task<ErrorOr<Transaction>> UpdateAsync(Transaction transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a transaction, recalculate subsequent balances
    /// </summary>
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

