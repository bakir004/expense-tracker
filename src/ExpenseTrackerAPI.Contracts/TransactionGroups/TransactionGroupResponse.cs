namespace ExpenseTrackerAPI.Contracts.TransactionGroups;

/// <summary>
/// Response contract for transaction group data.
/// </summary>
/// <param name="Id">Unique identifier for the transaction group</param>
/// <param name="Name">Name of the transaction group (e.g., "January Budget", "Vacation Expenses")</param>
/// <param name="Description">Optional detailed description of the group's purpose or scope</param>
/// <param name="UserId">ID of the user who owns this transaction group</param>
/// <param name="CreatedAt">UTC timestamp when the transaction group was created</param>
public record TransactionGroupResponse(
    int Id,
    string Name,
    string? Description,
    int UserId,
    DateTime CreatedAt);
