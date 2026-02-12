using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Transactions.Data;

public class GetTransactionsResult
{
    public List<Transaction> Transactions { get; set; } = new();
    
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetChange { get; set; }
    public int IncomeCount { get; set; }
    public int ExpenseCount { get; set; }
}

