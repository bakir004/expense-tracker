using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.TransactionGroups;

/// <summary>
/// Extension methods for mapping TransactionGroup entities to response contracts.
/// </summary>
public static class TransactionGroupMappingExtensions
{
    /// <summary>
    /// Maps a TransactionGroup entity to a TransactionGroupResponse contract.
    /// </summary>
    /// <param name="transactionGroup">The transaction group entity to map.</param>
    /// <returns>A TransactionGroupResponse containing the transaction group data.</returns>
    public static TransactionGroupResponse ToResponse(this TransactionGroup transactionGroup)
    {
        return new TransactionGroupResponse(
            Id: transactionGroup.Id,
            Name: transactionGroup.Name,
            Description: transactionGroup.Description,
            UserId: transactionGroup.UserId,
            CreatedAt: transactionGroup.CreatedAt);
    }

    /// <summary>
    /// Maps a collection of TransactionGroup entities to TransactionGroupResponse contracts.
    /// </summary>
    /// <param name="transactionGroups">The transaction group entities to map.</param>
    /// <returns>A collection of TransactionGroupResponse contracts.</returns>
    public static IEnumerable<TransactionGroupResponse> ToResponses(this IEnumerable<TransactionGroup> transactionGroups)
    {
        return transactionGroups.Select(g => g.ToResponse());
    }
}
