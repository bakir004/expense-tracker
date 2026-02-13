namespace ExpenseTrackerAPI.Contracts.TransactionGroups;

/// <summary>
/// Response contract for transaction group data.
/// </summary>
public record TransactionGroupResponse(
    int Id,
    string Name,
    string? Description,
    int UserId,
    DateTime CreatedAt);
