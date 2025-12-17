namespace SampleCkWebApp.Expenses;

public class GetExpensesResponse
{
    public List<ExpenseResponse> Expenses { get; set; } = new();
    public int TotalCount { get; set; }
}

