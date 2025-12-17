using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Expenses.Data;
using SampleCkWebApp.Expenses;

namespace SampleCkWebApp.Application.Expenses.Mappings;

public static class ExpenseMappings
{
    public static GetExpensesResponse ToResponse(this GetExpensesResult result)
    {
        return new GetExpensesResponse
        {
            Expenses = result.Expenses.Select(e => e.ToResponse()).ToList(),
            TotalCount = result.Expenses.Count
        };
    }
    
    public static ExpenseResponse ToResponse(this Expense expense)
    {
        return new ExpenseResponse
        {
            Id = expense.Id,
            Amount = expense.Amount,
            Date = expense.Date,
            Description = expense.Description,
            PaymentMethod = expense.PaymentMethod,
            CategoryId = expense.CategoryId,
            UserId = expense.UserId,
            ExpenseGroupId = expense.ExpenseGroupId
        };
    }
}

