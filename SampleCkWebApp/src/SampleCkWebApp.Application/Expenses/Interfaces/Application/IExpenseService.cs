using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Expenses.Data;

namespace SampleCkWebApp.Application.Expenses.Interfaces.Application;

public interface IExpenseService
{
    Task<ErrorOr<GetExpensesResult>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<Expense>> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<List<Expense>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<Expense>> CreateAsync(decimal amount, DateTime date, string? description, PaymentMethod paymentMethod, int categoryId, int userId, int? expenseGroupId, CancellationToken cancellationToken);
    
    Task<ErrorOr<Expense>> UpdateAsync(int id, decimal amount, DateTime date, string? description, PaymentMethod paymentMethod, int categoryId, int? expenseGroupId, CancellationToken cancellationToken);
    
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

