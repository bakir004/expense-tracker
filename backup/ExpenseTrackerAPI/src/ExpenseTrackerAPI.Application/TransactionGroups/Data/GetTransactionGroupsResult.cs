using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.TransactionGroups.Data;

public class GetTransactionGroupsResult
{
    public List<TransactionGroup> TransactionGroups { get; set; } = new();
}

