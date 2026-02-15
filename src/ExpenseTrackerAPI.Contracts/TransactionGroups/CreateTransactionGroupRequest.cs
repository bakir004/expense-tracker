namespace ExpenseTrackerAPI.Contracts.TransactionGroups;

/// <summary>
/// Request contract for creating a new transaction group.
/// </summary>
/// <param name="Name">Name of the transaction group (required, e.g., "January Budget", "Vacation Expenses")</param>
/// <param name="Description">Optional detailed description of the group's purpose or what it will track</param>
public record CreateTransactionGroupRequest(
    string Name,
    string? Description);
