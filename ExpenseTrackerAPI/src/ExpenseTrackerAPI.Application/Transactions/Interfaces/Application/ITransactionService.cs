using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Application.Transactions.Data;

namespace ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;

/// <summary>
/// Service interface for transaction business operations.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Get all transactions with summary statistics
    /// </summary>
    Task<ErrorOr<GetTransactionsResult>> GetAllAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Get a single transaction by ID
    /// </summary>
    Task<ErrorOr<Transaction>> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get all transactions for a user with summary statistics
    /// </summary>
    Task<ErrorOr<GetTransactionsResult>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Get transactions for a user filtered by type (EXPENSE or INCOME)
    /// </summary>
    Task<ErrorOr<GetTransactionsResult>> GetByUserIdAndTypeAsync(int userId, TransactionType type, CancellationToken cancellationToken);
    
    /// <summary>
    /// Create a new transaction
    /// </summary>
    Task<ErrorOr<Transaction>> CreateAsync(
        int userId,
        TransactionType transactionType,
        decimal amount,
        DateTime date,
        string subject,
        string? notes,
        PaymentMethod paymentMethod,
        int? categoryId,
        int? transactionGroupId,
        string? incomeSource,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Update an existing transaction
    /// </summary>
    Task<ErrorOr<Transaction>> UpdateAsync(
        int id,
        TransactionType transactionType,
        decimal amount,
        DateTime date,
        string subject,
        string? notes,
        PaymentMethod paymentMethod,
        int? categoryId,
        int? transactionGroupId,
        string? incomeSource,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Delete a transaction
    /// </summary>
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

