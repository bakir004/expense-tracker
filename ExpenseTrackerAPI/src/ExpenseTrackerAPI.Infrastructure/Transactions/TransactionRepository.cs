using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ExpenseTrackerAPI.Domain.Constants;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using ExpenseTrackerAPI.Application.Transactions.Data;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Infrastructure.Shared;

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
    private readonly ExpenseTrackerDbContext _context;

    public TransactionRepository(ExpenseTrackerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ErrorOr<List<Transaction>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _context.Transactions
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Transaction>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _context.Transactions
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

    public async Task<ErrorOr<List<Transaction>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Transaction>>> GetByUserIdWithFiltersAsync(int userId, TransactionQueryOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Transactions.Where(t => t.UserId == userId);

            if (!string.IsNullOrWhiteSpace(options.Subject))
            {
                var subject = options.Subject.Trim();
                query = query.Where(t => EF.Functions.ILike(t.Subject, $"%{subject}%"));
            }

            if (options.CategoryIds is { Count: > 0 })
            {
                var ids = options.CategoryIds;
                query = query.Where(t => t.CategoryId != null && ids.Contains(t.CategoryId.Value));
            }

            if (options.PaymentMethods is { Count: > 0 })
            {
                var methods = options.PaymentMethods;
                query = query.Where(t => methods.Contains(t.PaymentMethod));
            }

            if (options.TransactionType.HasValue)
            {
                query = query.Where(t => t.TransactionType == options.TransactionType.Value);
            }

            if (options.DateFrom.HasValue)
            {
                var from = options.DateFrom.Value;
                query = query.Where(t => t.Date >= from);
            }

            if (options.DateTo.HasValue)
            {
                var to = options.DateTo.Value;
                query = query.Where(t => t.Date <= to);
            }

            var desc = options.SortDescending;

            IOrderedQueryable<Transaction> ordered = desc
                ? query.OrderByDescending(t => t.Date)
                : query.OrderBy(t => t.Date);

            ordered = options.SortBy?.ToLowerInvariant() switch
            {
                "subject" => desc ? ordered.ThenByDescending(t => t.Subject).ThenByDescending(t => t.CreatedAt) : ordered.ThenBy(t => t.Subject).ThenBy(t => t.CreatedAt),
                "paymentmethod" => desc ? ordered.ThenByDescending(t => t.PaymentMethod).ThenByDescending(t => t.CreatedAt) : ordered.ThenBy(t => t.PaymentMethod).ThenBy(t => t.CreatedAt),
                "category" => desc ? ordered.ThenByDescending(t => t.CategoryId).ThenByDescending(t => t.CreatedAt) : ordered.ThenBy(t => t.CategoryId).ThenBy(t => t.CreatedAt),
                "amount" => desc ? ordered.ThenByDescending(t => t.SignedAmount).ThenByDescending(t => t.CreatedAt) : ordered.ThenBy(t => t.SignedAmount).ThenBy(t => t.CreatedAt),
                _ => desc ? ordered.ThenByDescending(t => t.CreatedAt) : ordered.ThenBy(t => t.CreatedAt)
            };

            var transactions = await ordered.ToListAsync(cancellationToken);
            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Transaction>>> GetByUserIdAndTypeAsync(int userId, TransactionType type, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.TransactionType == type)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Transaction>>> GetByUserIdAndDateRangeAsync(int userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    // ========================================================================
    // COMPLEX OPERATIONS WITH CUMULATIVE BALANCE CALCULATIONS
    // These operations require precise cumulative balance calculations
    // and atomic updates of multiple transactions using EF Core bulk operations
    // ========================================================================

    public async Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Step 1: Get previous cumulative_delta
                var previousCumulativeDelta = await _context.Transactions
                    .Where(t => t.UserId == transaction.UserId)
                    .Where(t => t.Date < transaction.Date ||
                               (t.Date == transaction.Date && t.CreatedAt < transaction.CreatedAt))
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.CreatedAt)
                    .Select(t => t.CumulativeDelta)
                    .FirstOrDefaultAsync(cancellationToken);

                // Step 2: Calculate cumulative_delta for new transaction (default is 0m if no previous transaction)
                transaction.CumulativeDelta = previousCumulativeDelta + transaction.SignedAmount;

                // Step 3: Insert with correct cumulative_delta
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync(cancellationToken);

                // Step 4: Bulk update all subsequent transactions
                // Update transactions that come after this one in the ordering (Date > this.Date OR (Date == this.Date AND CreatedAt > this.CreatedAt))
                // This second condition is necessary since there must be an ordering even on the same date
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

    public async Task<ErrorOr<Transaction>> UpdateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var oldTransaction = await _context.Transactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == transaction.Id, cancellationToken);

                if (oldTransaction == null)
                {
                    return TransactionErrors.NotFound;
                }

                // Detach any tracked entity with the same Id to avoid tracking conflicts
                var trackedEntity = _context.ChangeTracker.Entries<Transaction>()
                    .FirstOrDefault(e => e.Entity.Id == transaction.Id);
                if (trackedEntity != null)
                    trackedEntity.State = EntityState.Detached;

                transaction.UserId = oldTransaction.UserId;
                transaction.CreatedAt = oldTransaction.CreatedAt;
                transaction.UpdatedAt = DateTime.UtcNow;

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
                    transaction.CumulativeDelta = oldTransaction.CumulativeDelta + amountDelta;
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

                if(transaction.Date < oldTransaction.Date) {
                    // If the transaction is being moved into the past, we need to add the signed amount to the cumulative delta, then update the rest
                    transaction.CumulativeDelta = previousCumulativeDelta + transaction.SignedAmount;
                } else {
                    // However, since we are first updating the new transaction and then the rest, this will update the new transaction
                    // based on stale data. We counteract this effect by not adding the signed amount.
                    transaction.CumulativeDelta = previousCumulativeDelta + correctionAmount;
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

                if(amountChanged) {
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
        try
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
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete transaction: {ex.Message}");
        }
    }

}
