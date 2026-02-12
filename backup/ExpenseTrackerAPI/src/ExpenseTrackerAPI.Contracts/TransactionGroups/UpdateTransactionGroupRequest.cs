namespace ExpenseTrackerAPI.Contracts.TransactionGroups;

public class UpdateTransactionGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

