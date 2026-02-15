namespace ExpenseTrackerAPI.Contracts.TransactionGroups;

/// <summary>
/// Request contract for updating an existing transaction group.
/// </summary>
/// <param name="Name">Updated name of the transaction group (required, e.g., "February Budget", "Updated Project Name")</param>
/// <param name="Description">Updated description of the group - set to null to remove existing description</param>
public record UpdateTransactionGroupRequest(
    string Name,
    string? Description);
