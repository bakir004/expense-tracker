using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.Expenses.Data;

public class GetExpensesResult
{
    public List<Expense> Expenses { get; set; } = new();
}

