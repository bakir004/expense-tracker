using ErrorOr;
using Microsoft.EntityFrameworkCore;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Shared;

namespace SampleCkWebApp.Infrastructure.TransactionGroups;

/// <summary>
/// Entity Framework Core implementation of the transaction group repository.
/// </summary>
public class TransactionGroupRepository : ITransactionGroupRepository
{
    private readonly ExpenseTrackerDbContext _context;

    public TransactionGroupRepository(ExpenseTrackerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ErrorOr<List<TransactionGroup>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var transactionGroups = await _context.TransactionGroups
                .OrderBy(tg => tg.Id)
                .ToListAsync(cancellationToken);

            return transactionGroups;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transaction groups: {ex.Message}");
        }
    }

    public async Task<ErrorOr<TransactionGroup>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var transactionGroup = await _context.TransactionGroups
                .FirstOrDefaultAsync(tg => tg.Id == id, cancellationToken);

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

    public async Task<ErrorOr<List<TransactionGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var transactionGroups = await _context.TransactionGroups
                .Where(tg => tg.UserId == userId)
                .OrderBy(tg => tg.Id)
                .ToListAsync(cancellationToken);

            return transactionGroups;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transaction groups by user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<TransactionGroup>> CreateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken)
    {
        try
        {
            transactionGroup.CreatedAt = DateTime.UtcNow;

            _context.TransactionGroups.Add(transactionGroup);
            await _context.SaveChangesAsync(cancellationToken);

            return transactionGroup;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create transaction group: {ex.Message}");
        }
    }

    public async Task<ErrorOr<TransactionGroup>> UpdateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken)
    {
        try
        {
            var existingGroup = await _context.TransactionGroups
                .FirstOrDefaultAsync(tg => tg.Id == transactionGroup.Id, cancellationToken);

            if (existingGroup == null)
            {
                return TransactionGroupErrors.NotFound;
            }

            existingGroup.Name = transactionGroup.Name;
            existingGroup.Description = transactionGroup.Description;

            await _context.SaveChangesAsync(cancellationToken);

            return existingGroup;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update transaction group: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var transactionGroup = await _context.TransactionGroups
                .FirstOrDefaultAsync(tg => tg.Id == id, cancellationToken);

            if (transactionGroup == null)
            {
                return TransactionGroupErrors.NotFound;
            }

            _context.TransactionGroups.Remove(transactionGroup);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete transaction group: {ex.Message}");
        }
    }
}
