using ErrorOr;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Application;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.TransactionGroups;

/// <summary>
/// Application service for transaction group operations.
/// Handles authorization checks to ensure users can only access their own transaction groups.
/// </summary>
public class TransactionGroupService : ITransactionGroupService
{
    private readonly ITransactionGroupRepository _transactionGroupRepository;

    public TransactionGroupService(ITransactionGroupRepository transactionGroupRepository)
    {
        _transactionGroupRepository = transactionGroupRepository ?? throw new ArgumentNullException(nameof(transactionGroupRepository));
    }

    /// <inheritdoc />
    public async Task<ErrorOr<TransactionGroup>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var result = await _transactionGroupRepository.GetByIdAsync(id, cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        var transactionGroup = result.Value;

        // Authorization check: ensure the transaction group belongs to the user
        if (transactionGroup.UserId != userId)
        {
            return TransactionGroupErrors.NotFound;
        }

        return transactionGroup;
    }

    /// <inheritdoc />
    public async Task<ErrorOr<List<TransactionGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _transactionGroupRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<TransactionGroup>> CreateAsync(
        int userId,
        string name,
        string? description,
        CancellationToken cancellationToken)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            return TransactionGroupErrors.InvalidName;
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 255)
        {
            return TransactionGroupErrors.InvalidName;
        }

        var transactionGroup = new TransactionGroup
        {
            Name = trimmedName,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        return await _transactionGroupRepository.CreateAsync(transactionGroup, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<TransactionGroup>> UpdateAsync(
        int id,
        int userId,
        string name,
        string? description,
        CancellationToken cancellationToken)
    {
        // First get the existing transaction group
        var existingResult = await _transactionGroupRepository.GetByIdAsync(id, cancellationToken);

        if (existingResult.IsError)
        {
            return existingResult.Errors;
        }

        var existingGroup = existingResult.Value;

        // Authorization check: ensure the transaction group belongs to the user
        if (existingGroup.UserId != userId)
        {
            return TransactionGroupErrors.NotFound;
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            return TransactionGroupErrors.InvalidName;
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 255)
        {
            return TransactionGroupErrors.InvalidName;
        }

        // Update the entity
        existingGroup.Name = trimmedName;
        existingGroup.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        return await _transactionGroupRepository.UpdateAsync(existingGroup, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, int userId, CancellationToken cancellationToken)
    {
        // First get the existing transaction group
        var existingResult = await _transactionGroupRepository.GetByIdAsync(id, cancellationToken);

        if (existingResult.IsError)
        {
            return existingResult.Errors;
        }

        var existingGroup = existingResult.Value;

        // Authorization check: ensure the transaction group belongs to the user
        if (existingGroup.UserId != userId)
        {
            return TransactionGroupErrors.NotFound;
        }

        return await _transactionGroupRepository.DeleteAsync(id, cancellationToken);
    }
}
