namespace ExpenseTrackerAPI.Contracts.TransactionGroups;

public class GetTransactionGroupsResponse
{
    public List<TransactionGroupResponse> TransactionGroups { get; set; } = new();
    public int TotalCount { get; set; }
}

