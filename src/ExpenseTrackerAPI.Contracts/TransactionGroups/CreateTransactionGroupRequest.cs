namespace ExpenseTrackerAPI.Contracts.TransactionGroups;

/// <summary>
/// Request contract for creating a new transaction group.
/// </summary>
public record CreateTransactionGroupRequest(
    string Name,
    string? Description);
