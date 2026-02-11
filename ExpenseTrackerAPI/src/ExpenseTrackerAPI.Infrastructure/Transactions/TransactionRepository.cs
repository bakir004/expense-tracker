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

                var minDate = oldTransaction.Date < transaction.Date ? oldTransaction.Date : transaction.Date;
                var maxDate = oldTransaction.Date > transaction.Date ? oldTransaction.Date : transaction.Date;

                if(!amountChanged)
                {
                    var oldCumulativeDelta = await _context.Transactions
                        .Where(t => t.UserId == transaction.UserId)
                        .Where(t => t.Date < minDate)
                        .OrderByDescending(t => t.Date)
                        .ThenByDescending(t => t.CreatedAt)
                        .Select(t => t.CumulativeDelta)
                        .FirstOrDefaultAsync(cancellationToken);

                    transaction.CumulativeDelta = oldCumulativeDelta + transaction.SignedAmount;
                    _context.Transactions.Update(transaction);
                    await _context.SaveChangesAsync(cancellationToken);

                    if(transaction.Date < oldTransaction.Date) {
                        await _context.Transactions
                            .Where(t => t.UserId == transaction.UserId)
                            .Where(t => t.Date >= minDate && t.Date <= maxDate && t.CreatedAt > transaction.CreatedAt)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + transaction.SignedAmount)
                                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                                cancellationToken);
                    } else if(transaction.Date > oldTransaction.Date) {
                        await _context.Transactions
                            .Where(t => t.UserId == transaction.UserId)
                            .Where(t => t.Date >= minDate && t.Date <= maxDate && t.CreatedAt < transaction.CreatedAt)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta - transaction.SignedAmount)
                                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                                cancellationToken);
                    }

                    await dbTransaction.CommitAsync(cancellationToken);

                    return transaction;
                }

                // DATE CHANGED: Optimized two-phase update
                // Phase 1: Transactions in range [minDate, maxDate] get the new transaction amount added
                // Phase 2: Transactions after maxDate get net delta = (newAmount - oldAmount)



                // Get cumulative_delta before minDate to calculate the transaction's new cumulative_delta
                // OPTIMIZATION: If date moved forwards (newDate > oldDate), minDate = oldDate
                // We can infer previousCumulativeDelta from oldTransaction without a query
                decimal previousCumulativeDelta;
                if (transaction.Date > oldTransaction.Date)
                {
                    // Date moved forwards: minDate = oldDate
                    // oldTransaction.CumulativeDelta = previousCumulativeDelta + oldTransaction.SignedAmount
                    // Therefore: previousCumulativeDelta = oldTransaction.CumulativeDelta - oldTransaction.SignedAmount
                    previousCumulativeDelta = oldTransaction.CumulativeDelta - oldTransaction.SignedAmount;
                }
                else
                {
                    // Date moved backwards: minDate = newDate, need to query for cumulative_delta before newDate
                    previousCumulativeDelta = await _context.Transactions
                        .Where(t => t.UserId == transaction.UserId)
                        .Where(t => t.Date < minDate)
                        .OrderByDescending(t => t.Date)
                        .ThenByDescending(t => t.CreatedAt)
                        .Select(t => t.CumulativeDelta)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                // Calculate the transaction's cumulative_delta at its new position
                transaction.CumulativeDelta = previousCumulativeDelta + transaction.SignedAmount;
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync(cancellationToken);

                // Phase 1: Add the new transaction amount to all other transactions in range [minDate, maxDate]
                // These transactions now have the moved transaction before them in the order
                await _context.Transactions
                    .Where(t => t.UserId == transaction.UserId)
                    .Where(t => t.Date >= minDate && t.Date <= maxDate && t.Id != transaction.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + transaction.SignedAmount)
                        .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                // Phase 2: Transactions after maxDate need to:
                // - Remove the old amount (transaction was at oldDate, now it's gone from there)
                // - Add the new amount (transaction is now at newDate, affecting them)
                // Net effect: (newAmount - oldAmount)
                decimal netDelta = transaction.SignedAmount - oldTransaction.SignedAmount;

                if (netDelta != 0)
                {
                    await _context.Transactions
                        .Where(t => t.UserId == transaction.UserId && t.Date > maxDate)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + netDelta)
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
                // Single query: Delete transaction AND update all subsequent transactions in one statement
                // Uses CTE (Common Table Expression) to capture deleted row data, then update subsequent rows
                var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"WITH deleted AS (
                        DELETE FROM ""Transaction""
                        WHERE id = {id}
                        RETURNING user_id, date, created_at, signed_amount
                      )
                      UPDATE ""Transaction"" t
                      SET cumulative_delta = t.cumulative_delta - d.signed_amount,
                          updated_at = CURRENT_TIMESTAMP
                      FROM deleted d
                      WHERE t.user_id = d.user_id
                        AND (t.date > d.date OR (t.date = d.date AND t.created_at > d.created_at))"
                    );

                // If no rows were deleted, transaction didn't exist
                if (rowsAffected == 0)
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
