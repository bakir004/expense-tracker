using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ExpenseTrackerAPI.Domain.Constants;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using ExpenseTrackerAPI.Contracts.Transactions;
using System.Linq.Expressions;

namespace ExpenseTrackerAPI.Infrastructure.Transactions;

/// <summary>
/// EF Core implementation of the transaction repository.
///
/// DESIGN:
/// - Each transaction stores cumulative_delta (running sum of signed_amounts)
/// - User.initial_balance + cumulative_delta = actual balance
/// - No separate UserBalance or UserBalanceHistory tables needed
///
/// IMPLEMENTATION:
/// - Uses EF Core for all operations (reads and writes)
/// - Supports non-sequential transaction dates by recalculating cumulative_delta
/// - Create/Update/Delete operations automatically update all affected subsequent transactions
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ErrorOr<Transaction>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (transaction == null)
            {
                return TransactionErrors.NotFound;
            }

            return transaction;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transaction: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var previousCumulativeDelta = await _context.Transactions
                        .Where(t => t.UserId == transaction.UserId)
                        .Where(t => t.Date < transaction.Date ||
                                   (t.Date == transaction.Date && t.CreatedAt < transaction.CreatedAt))
                        .OrderByDescending(t => t.Date)
                        .ThenByDescending(t => t.CreatedAt)
                        .Select(t => t.CumulativeDelta)
                        .FirstOrDefaultAsync(cancellationToken);

                    transaction.UpdateCumulativeDelta(previousCumulativeDelta + transaction.SignedAmount);

                    _context.Transactions.Add(transaction);
                    await _context.SaveChangesAsync(cancellationToken);

                    await _context.Transactions
                        .Where(t => t.UserId == transaction.UserId)
                        .Where(t => t.Date > transaction.Date ||
                                   (t.Date == transaction.Date && t.CreatedAt > transaction.CreatedAt))
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + transaction.SignedAmount)
                            .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                            cancellationToken);

                    await dbTransaction.CommitAsync(cancellationToken);
                    return transaction;
                }
                catch
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresSqlState.ForeignKeyViolation)
        {
            var constraint = pgEx.ConstraintName ?? pgEx.Message;
            if (constraint.Contains("Users", StringComparison.OrdinalIgnoreCase) || constraint.Contains("user_id", StringComparison.OrdinalIgnoreCase))
                return UserErrors.NotFound;
            if (constraint.Contains("Category", StringComparison.OrdinalIgnoreCase) || constraint.Contains("category_id", StringComparison.OrdinalIgnoreCase))
                return CategoryErrors.NotFound;
            if (constraint.Contains("TransactionGroup", StringComparison.OrdinalIgnoreCase) || constraint.Contains("transaction_group", StringComparison.OrdinalIgnoreCase))
                return TransactionGroupErrors.NotFound;
            return Error.Failure("Database.Error", "Referenced entity not found.");
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create transaction: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Transaction>> UpdateAsync(Transaction oldTransaction, Transaction transaction, CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Detach the old transaction if it's being tracked to avoid conflicts
                    var trackedEntity = _context.ChangeTracker.Entries<Transaction>()
                        .FirstOrDefault(e => e.Entity.Id == oldTransaction.Id);
                    if (trackedEntity != null)
                    {
                        _context.Entry(trackedEntity.Entity).State = EntityState.Detached;
                    }

                    bool dateChanged = oldTransaction.Date != transaction.Date;
                    bool amountChanged = oldTransaction.SignedAmount != transaction.SignedAmount;

                    if (!dateChanged && !amountChanged)
                    {
                        _context.Transactions.Update(transaction);
                        await _context.SaveChangesAsync(cancellationToken);
                        await dbTransaction.CommitAsync(cancellationToken);
                        return transaction;
                    }

                    // OPTIMIZATION: If only amount changed (date stayed same), use bulk update
                    if (!dateChanged && amountChanged)
                    {
                        decimal amountDelta = transaction.SignedAmount - oldTransaction.SignedAmount;
                        transaction.UpdateCumulativeDelta(oldTransaction.CumulativeDelta + amountDelta);
                        _context.Transactions.Update(transaction);
                        await _context.SaveChangesAsync(cancellationToken);

                        await _context.Transactions
                            .Where(t => t.UserId == transaction.UserId)
                            .Where(t => t.Date > transaction.Date ||
                                       (t.Date == transaction.Date && t.CreatedAt > transaction.CreatedAt))
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + amountDelta)
                                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                                cancellationToken);

                        await dbTransaction.CommitAsync(cancellationToken);
                        return transaction;
                    }

                    /*
                    We need to get the previous cumulative delta to continue the chain of cumulative deltae

                    If we imagine the graph of cumulative deltae, with the x-axis as time and y-axis as cumulative delta
                    and labelling the previous date as P and new date as N, it would look like this:
                             ___________
                            |
                    ________|

                    --------P----N------>
                    This movement would make the graph like this:
                                  ______
                                 |
                    _____________|

                    --------P----N------>
                    This means that for moving a transaction into the future, it would require subtracting the signed amount from
                    transactions inbetween P and N. For moving into the past, we just add the signed amount to the same interval.

                    The catch is that this sequence of steps given first calculates the cumulative delta for the new transaction,
                    and then updates the rest. But, for moving a transaction into the future, it should only calculate the
                    new transaction's cumulative delta after the others updated, so it links to the correct version of the previous
                    transaction. This can be easily achieved by just correcting the amount if the move is into the future.
                    */

                    var minDate = oldTransaction.Date < transaction.Date ? oldTransaction.Date : transaction.Date;
                    var maxDate = oldTransaction.Date > transaction.Date ? oldTransaction.Date : transaction.Date;

                    var previousCumulativeDelta = await _context.Transactions
                        .Where(t => t.UserId == transaction.UserId)
                        .Where(t => t.Date < transaction.Date || (t.Date == transaction.Date && t.CreatedAt < transaction.CreatedAt))
                        .OrderByDescending(t => t.Date)
                        .ThenByDescending(t => t.CreatedAt)
                        .Select(t => t.CumulativeDelta)
                        .FirstOrDefaultAsync(cancellationToken);

                    // The correction amount will be zero if only the date changed
                    var correctionAmount = transaction.SignedAmount - oldTransaction.SignedAmount;

                    if (transaction.Date < oldTransaction.Date)
                    {
                        // If the transaction is being moved into the past, we need to add the signed amount to the cumulative delta, then update the rest
                        transaction.UpdateCumulativeDelta(previousCumulativeDelta + transaction.SignedAmount);
                    }
                    else
                    {
                        // However, since we are first updating the new transaction and then the rest, this will update the new transaction
                        // based on stale data. We counteract this effect by not adding the signed amount.
                        transaction.UpdateCumulativeDelta(previousCumulativeDelta + correctionAmount);
                    }
                    _context.Transactions.Update(transaction);
                    await _context.SaveChangesAsync(cancellationToken);

                    // If only date changed, this will add the signed amount if moving into the past,
                    // and subtract if moving into the future
                    var deltaAmount = transaction.Date < oldTransaction.Date
                        ? transaction.SignedAmount
                        : -oldTransaction.SignedAmount;

                    await _context.Transactions
                        .Where(t => t.UserId == transaction.UserId)
                        .Where(t => t.Date > minDate && t.Date < maxDate
                            || (t.Date == minDate && t.CreatedAt > transaction.CreatedAt)
                            || (t.Date == maxDate && t.CreatedAt < transaction.CreatedAt))
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + deltaAmount)
                            .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                            cancellationToken);

                    if (amountChanged)
                    {
                        await _context.Transactions
                            .Where(t => t.UserId == transaction.UserId)
                            .Where(t => t.Date > maxDate || (t.Date == maxDate && t.CreatedAt > transaction.CreatedAt))
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + correctionAmount)
                                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                                cancellationToken);
                    }

                    await dbTransaction.CommitAsync(cancellationToken);
                    return transaction;
                }
                catch
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresSqlState.ForeignKeyViolation)
        {
            var constraint = pgEx.ConstraintName ?? pgEx.Message;
            if (constraint.Contains("Category", StringComparison.OrdinalIgnoreCase) || constraint.Contains("category_id", StringComparison.OrdinalIgnoreCase))
                return CategoryErrors.NotFound;
            if (constraint.Contains("TransactionGroup", StringComparison.OrdinalIgnoreCase) || constraint.Contains("transaction_group", StringComparison.OrdinalIgnoreCase))
                return TransactionGroupErrors.NotFound;
            return Error.Failure("Database.Error", "Referenced entity not found.");
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update transaction: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync<ErrorOr<Deleted>>(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var transaction = await _context.Transactions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

                    if (transaction == null)
                    {
                        return TransactionErrors.NotFound;
                    }

                    await _context.Transactions
                        .Where(t => t.UserId == transaction.UserId)
                        .Where(t => t.Date > transaction.Date ||
                                   (t.Date == transaction.Date && t.CreatedAt > transaction.CreatedAt))
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta - transaction.SignedAmount)
                            .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                            cancellationToken);

                    var deletedCount = await _context.Transactions
                        .Where(t => t.Id == id)
                        .ExecuteDeleteAsync(cancellationToken);

                    if (deletedCount == 0)
                    {
                        return TransactionErrors.NotFound;
                    }

                    await dbTransaction.CommitAsync(cancellationToken);
                    return Result.Deleted;
                }
                catch
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete transaction: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Transaction>>> GetByUserIdWithFilterAsync(
        int userId,
        TransactionFilter filter,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId);

            // Apply filters
            query = ApplyFilters(query, filter);

            // Apply sorting
            query = ApplySorting(query, filter);

            // Apply pagination
            var transactions = await query
                .Skip(filter.Skip)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    private static IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, TransactionFilter filter)
    {
        // Filter by transaction type
        if (filter.TransactionType.HasValue)
        {
            query = query.Where(t => t.TransactionType == filter.TransactionType.Value);
        }

        // Filter by amount range
        if (filter.MinAmount.HasValue)
        {
            query = query.Where(t => t.Amount >= filter.MinAmount.Value);
        }

        if (filter.MaxAmount.HasValue)
        {
            query = query.Where(t => t.Amount <= filter.MaxAmount.Value);
        }

        // Filter by date range
        if (filter.DateFrom.HasValue)
        {
            query = query.Where(t => t.Date >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            query = query.Where(t => t.Date <= filter.DateTo.Value);
        }

        // Filter by subject (case-insensitive contains)
        if (!string.IsNullOrWhiteSpace(filter.SubjectContains))
        {
            var searchTerm = filter.SubjectContains.ToLower();
            query = query.Where(t => t.Subject.ToLower().Contains(searchTerm));
        }

        // Filter by notes (case-insensitive contains)
        if (!string.IsNullOrWhiteSpace(filter.NotesContains))
        {
            var searchTerm = filter.NotesContains.ToLower();
            query = query.Where(t => t.Notes != null && t.Notes.ToLower().Contains(searchTerm));
        }

        // Filter by payment methods (OR logic)
        if (filter.PaymentMethods is { Count: > 0 })
        {
            query = query.Where(t => filter.PaymentMethods.Contains(t.PaymentMethod));
        }

        // Filter by category IDs (OR logic)
        if (filter.CategoryIds is { Count: > 0 })
        {
            query = query.Where(t => t.CategoryId.HasValue && filter.CategoryIds.Contains(t.CategoryId.Value));
        }

        // Filter for uncategorized transactions
        if (filter.Uncategorized)
        {
            query = query.Where(t => t.CategoryId == null);
        }

        // Filter by transaction group IDs (OR logic)
        if (filter.TransactionGroupIds is { Count: > 0 })
        {
            query = query.Where(t => t.TransactionGroupId.HasValue && filter.TransactionGroupIds.Contains(t.TransactionGroupId.Value));
        }

        // Filter for ungrouped transactions
        if (filter.Ungrouped)
        {
            query = query.Where(t => t.TransactionGroupId == null);
        }

        return query;
    }

    private static IQueryable<Transaction> ApplySorting(IQueryable<Transaction> query, TransactionFilter filter)
    {
        // Primary sort by the specified field
        query = filter.SortBy switch
        {
            TransactionSortField.Amount => filter.SortDescending
                ? query.OrderByDescending(t => t.Amount)
                : query.OrderBy(t => t.Amount),

            TransactionSortField.Subject => filter.SortDescending
                ? query.OrderByDescending(t => t.Subject)
                : query.OrderBy(t => t.Subject),

            TransactionSortField.PaymentMethod => filter.SortDescending
                ? query.OrderByDescending(t => t.PaymentMethod)
                : query.OrderBy(t => t.PaymentMethod),

            TransactionSortField.CreatedAt => filter.SortDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),

            TransactionSortField.UpdatedAt => filter.SortDescending
                ? query.OrderByDescending(t => t.UpdatedAt)
                : query.OrderBy(t => t.UpdatedAt),

            // Default: sort by Date, then CreatedAt
            _ => filter.SortDescending
                ? query.OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.Date).ThenBy(t => t.CreatedAt)
        };

        // Add secondary sort by Id for stable pagination (except for Date which already has CreatedAt)
        if (filter.SortBy != TransactionSortField.Date)
        {
            query = filter.SortDescending
                ? ((IOrderedQueryable<Transaction>)query).ThenByDescending(t => t.Id)
                : ((IOrderedQueryable<Transaction>)query).ThenBy(t => t.Id);
        }

        return query;
    }
}
