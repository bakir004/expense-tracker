using ErrorOr;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerAPI.Infrastructure.Transactions;

/// <summary>
/// EF Core implementation of transaction repository.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ErrorOr<(IEnumerable<Transaction> Transactions, int TotalCount)>> GetAllByUserIdAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var transactions = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return (transactions, totalCount);
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Transaction.GetAllByUserId.Cancelled", "Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return Error.Failure("Transaction.GetAllByUserId.DatabaseError", $"Database error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Transaction>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

            if (transaction is null)
                return TransactionErrors.NotFound;

            return transaction;
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Transaction.GetById.Cancelled", "Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return Error.Failure("Transaction.GetById.DatabaseError", $"Database error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return transaction;
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Transaction.Create.Cancelled", "Operation was cancelled.");
        }
        catch (DbUpdateException ex)
        {
            return Error.Failure("Transaction.Create.DatabaseError", $"Failed to create transaction: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Error.Failure("Transaction.Create.UnexpectedError", $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Transaction>> UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return transaction;
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Transaction.Update.Cancelled", "Operation was cancelled.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return TransactionErrors.ConcurrencyConflict;
        }
        catch (DbUpdateException ex)
        {
            return Error.Failure("Transaction.Update.DatabaseError", $"Failed to update transaction: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Error.Failure("Transaction.Update.UnexpectedError", $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Success>> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

            if (transaction is null)
                return TransactionErrors.NotFound;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Transaction.Delete.Cancelled", "Operation was cancelled.");
        }
        catch (DbUpdateException ex)
        {
            return Error.Failure("Transaction.Delete.DatabaseError", $"Failed to delete transaction: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Error.Failure("Transaction.Delete.UnexpectedError", $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<bool>> ExistsAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _context.Transactions
                .AsNoTracking()
                .AnyAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

            return exists;
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Transaction.Exists.Cancelled", "Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return Error.Failure("Transaction.Exists.DatabaseError", $"Database error: {ex.Message}");
        }
    }
}
