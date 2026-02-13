namespace ExpenseTrackerAPI.Contracts.TransactionGroups;

/// <summary>
/// Request contract for updating an existing transaction group.
/// </summary>
public record UpdateTransactionGroupRequest(
    string Name,
    string? Description);
