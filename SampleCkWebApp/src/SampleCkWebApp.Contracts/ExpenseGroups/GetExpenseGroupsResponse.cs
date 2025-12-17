namespace SampleCkWebApp.ExpenseGroups;

public class GetExpenseGroupsResponse
{
    public List<ExpenseGroupResponse> ExpenseGroups { get; set; } = new();
    public int TotalCount { get; set; }
}

