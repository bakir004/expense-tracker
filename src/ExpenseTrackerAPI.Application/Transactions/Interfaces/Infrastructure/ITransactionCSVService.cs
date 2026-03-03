using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for transaction CSV operations.
/// Defined in Application layer, implemented in Infrastructure layer.
/// </summary>
public interface ITransactionCSVService
{
    /// <summary>
    /// Export transactions to CSV format.
    /// </summary>
    /// <param name="transactions">The list of transactions to export.</param>
    /// <returns>A CSV string representing the transactions.</returns>
    Task<ErrorOr<Stream>> ExportTransactionsToCSVAsync(List<Transaction> transactions, CancellationToken cancellationToken);
}
