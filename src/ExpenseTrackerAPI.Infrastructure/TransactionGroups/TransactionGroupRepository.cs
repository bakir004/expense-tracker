using ErrorOr;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using ExpenseTrackerAPI.Infrastructure.Persistence;

namespace ExpenseTrackerAPI.Infrastructure.TransactionGroups;

/// <summary>
/// EF Core implementation of the transaction group repository.
/// </summary>
public class TransactionGroupRepository : ITransactionGroupRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionGroupRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<ErrorOr<TransactionGroup>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var transactionGroup = await _context.TransactionGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

            if (transactionGroup == null)
            {
                return TransactionGroupErrors.NotFound;
            }

            return transactionGroup;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transaction group: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ErrorOr<List<TransactionGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var transactionGroups = await _context.TransactionGroups
                .AsNoTracking()
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.Name)
                .ToListAsync(cancellationToken);

            return transactionGroups;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transaction groups: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ErrorOr<TransactionGroup>> CreateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken)
    {
        try
        {
            _context.TransactionGroups.Add(transactionGroup);
            await _context.SaveChangesAsync(cancellationToken);

            return transactionGroup;
        }
        catch (DbUpdateException ex)
        {
            // Check for foreign key violation (user doesn't exist)
            if (ex.InnerException?.Message.Contains("user", StringComparison.OrdinalIgnoreCase) == true ||
                ex.InnerException?.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) == true)
            {
                return TransactionGroupErrors.UserNotFound;
            }

            return Error.Failure("Database.Error", $"Failed to create transaction group: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create transaction group: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ErrorOr<TransactionGroup>> UpdateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken)
    {
        try
        {
            _context.TransactionGroups.Update(transactionGroup);
            await _context.SaveChangesAsync(cancellationToken);

            return transactionGroup;
        }
        catch (DbUpdateConcurrencyException)
        {
            return TransactionGroupErrors.NotFound;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update transaction group: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deletedCount = await _context.TransactionGroups
                .Where(g => g.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount == 0)
            {
                return TransactionGroupErrors.NotFound;
            }

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete transaction group: {ex.Message}");
        }
    }
}
