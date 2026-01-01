using ErrorOr;
using Microsoft.EntityFrameworkCore;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Transactions.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Shared;

namespace SampleCkWebApp.Infrastructure.Transactions;

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
                .ThenByDescending(t => t.Id)
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
                .ThenByDescending(t => t.Id)
                .ToListAsync(cancellationToken);

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
                .ThenByDescending(t => t.Id)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Transaction>>> GetByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    // ========================================================================
    // COMPLEX OPERATIONS USING RAW SQL
    // These operations require precise cumulative balance calculations
    // and atomic updates of multiple transactions
    // ========================================================================

    public async Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                transaction.CreatedAt = DateTime.UtcNow;
                transaction.UpdatedAt = DateTime.UtcNow;
                
                // Step 1: Get previous cumulative_delta (1 query - optimized to select only the value)
                var previousCumulativeDelta = await _context.Transactions
                    .Where(t => t.UserId == transaction.UserId)
                    .Where(t => t.Date < transaction.Date || 
                               (t.Date == transaction.Date && t.Id < transaction.Id))
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.Id)
                    .Select(t => t.CumulativeDelta)
                    .FirstOrDefaultAsync(cancellationToken);
                
                // Step 2: Calculate cumulative_delta for new transaction (default is 0m if no previous transaction)
                transaction.CumulativeDelta = previousCumulativeDelta + transaction.SignedAmount;
                
                // Step 3: Insert with correct cumulative_delta (1 query)
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync(cancellationToken);
                
                // Step 4: Bulk update all subsequent transactions (1 query - handles 0 to 10,000+ rows efficiently!)
                await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""Transaction""
                      SET cumulative_delta = cumulative_delta + {0},
                          updated_at = CURRENT_TIMESTAMP
                      WHERE user_id = {1}
                        AND (date > {2} OR (date = {2} AND id > {3}))",
                    transaction.SignedAmount,
                    transaction.UserId,
                    transaction.Date,
                    transaction.Id,
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
                // Step 1: Get the old transaction to compare changes
                var oldTransaction = await _context.Transactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == transaction.Id, cancellationToken);
                
                if (oldTransaction == null)
                {
                    return TransactionErrors.NotFound;
                }
                
                // Step 2: Check if date or signed_amount changed (affects cumulative calculations)
                bool dateChanged = oldTransaction.Date != transaction.Date;
                bool amountChanged = oldTransaction.SignedAmount != transaction.SignedAmount;
                
                if (!dateChanged && !amountChanged)
                {
                    // Simple update - no cumulative recalculation needed
                    transaction.UpdatedAt = DateTime.UtcNow;
                    _context.Transactions.Update(transaction);
                    await _context.SaveChangesAsync(cancellationToken);
                    await dbTransaction.CommitAsync(cancellationToken);
                    return transaction;
                }
                
                // OPTIMIZATION: If only amount changed (date stayed same), use bulk update
                if (!dateChanged && amountChanged)
                {
                    decimal amountDelta = transaction.SignedAmount - oldTransaction.SignedAmount;
                    
                    // Get previous cumulative_delta to calculate new value for this transaction
                    var previousCumulativeDelta = await _context.Transactions
                        .Where(t => t.UserId == transaction.UserId)
                        .Where(t => t.Date < transaction.Date || 
                                   (t.Date == transaction.Date && t.Id < transaction.Id))
                        .OrderByDescending(t => t.Date)
                        .ThenByDescending(t => t.Id)
                        .Select(t => t.CumulativeDelta)
                        .FirstOrDefaultAsync(cancellationToken);
                    
                    // Update this transaction's cumulative_delta (default is 0m if no previous transaction)
                    transaction.CumulativeDelta = previousCumulativeDelta + transaction.SignedAmount;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    _context.Transactions.Update(transaction);
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    // Bulk update all subsequent transactions (1 query for any number of rows!)
                    await _context.Database.ExecuteSqlRawAsync(
                        @"UPDATE ""Transaction""
                          SET cumulative_delta = cumulative_delta + {0},
                              updated_at = CURRENT_TIMESTAMP
                          WHERE user_id = {1}
                            AND (date > {2} OR (date = {2} AND id > {3}))",
                        amountDelta,
                        transaction.UserId,
                        transaction.Date,
                        transaction.Id,
                        cancellationToken);
                    
                    await dbTransaction.CommitAsync(cancellationToken);
                    return transaction;
                }
                
                // DATE CHANGED: Need full recalculation from earliest affected point
                transaction.UpdatedAt = DateTime.UtcNow;
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync(cancellationToken);
                
                // Find the earliest date that needs recalculation
                var earliestDate = oldTransaction.Date < transaction.Date ? oldTransaction.Date : transaction.Date;
                
                // Get all transactions from the earliest affected point, ordered chronologically
                var transactionsToRecalculate = await _context.Transactions
                    .Where(t => t.UserId == transaction.UserId)
                    .Where(t => t.Date > earliestDate || 
                               (t.Date == earliestDate))
                    .OrderBy(t => t.Date)
                    .ThenBy(t => t.Id)
                    .ToListAsync(cancellationToken);
                
                // Get the cumulative_delta before the earliest affected transaction
                var previousCumulativeDeltaForRecalc = await _context.Transactions
                    .Where(t => t.UserId == transaction.UserId)
                    .Where(t => t.Date < earliestDate)
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.Id)
                    .Select(t => t.CumulativeDelta)
                    .FirstOrDefaultAsync(cancellationToken);
                
                decimal runningCumulativeDelta = previousCumulativeDeltaForRecalc; // Default is 0m if no previous transaction
                
                // Recalculate cumulative_delta for all affected transactions
                foreach (var tx in transactionsToRecalculate)
                {
                    runningCumulativeDelta += tx.SignedAmount;
                    tx.CumulativeDelta = runningCumulativeDelta;
                    tx.UpdatedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync(cancellationToken);
                await dbTransaction.CommitAsync(cancellationToken);
                
                return transaction;
            }
            catch
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                throw;
            }
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
                // Step 1: Get the transaction to delete
                var transactionToDelete = await _context.Transactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
                
                if (transactionToDelete == null)
                {
                    return TransactionErrors.NotFound;
                }
                
                // Step 2: Bulk update all subsequent transactions (1 query - subtract deleted amount)
                await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""Transaction""
                      SET cumulative_delta = cumulative_delta - {0},
                          updated_at = CURRENT_TIMESTAMP
                      WHERE user_id = {1}
                        AND (date > {2} OR (date = {2} AND id > {3}))",
                    transactionToDelete.SignedAmount,
                    transactionToDelete.UserId,
                    transactionToDelete.Date,
                    transactionToDelete.Id,
                    cancellationToken);
                
                // Step 3: Delete the transaction
                var entityToDelete = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
                
                if (entityToDelete != null)
                {
                    _context.Transactions.Remove(entityToDelete);
                    await _context.SaveChangesAsync(cancellationToken);
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
