namespace SampleCkWebApp.Contracts.Transactions;

public class GetTransactionsResponse
{
    public List<TransactionResponse> Transactions { get; set; } = new();
    
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Summary statistics for the returned transactions
    /// </summary>
    public TransactionSummary? Summary { get; set; }
}

public class TransactionSummary
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetChange { get; set; }
    public int IncomeCount { get; set; }
    public int ExpenseCount { get; set; }
}

