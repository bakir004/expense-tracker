using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Expenses.Data;
using SampleCkWebApp.Application.Expenses.Interfaces.Application;
using SampleCkWebApp.Application.Expenses.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.Expenses;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;

    public ExpenseService(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
    }
    
    public async Task<ErrorOr<GetExpensesResult>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _expenseRepository.GetAllAsync(cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }
        
        return new GetExpensesResult
        {
            Expenses = result.Value
        };
    }
    
    public async Task<ErrorOr<Expense>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _expenseRepository.GetByIdAsync(id, cancellationToken);
    }
    
    public async Task<ErrorOr<List<Expense>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _expenseRepository.GetByUserIdAsync(userId, cancellationToken);
    }
    
    public async Task<ErrorOr<Expense>> CreateAsync(decimal amount, DateTime date, string? description, PaymentMethod paymentMethod, int categoryId, int userId, int? expenseGroupId, CancellationToken cancellationToken)
    {
        var validationResult = ExpenseValidator.ValidateExpenseRequest(amount, date, categoryId, userId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        var expense = new Expense
        {
            Amount = amount,
            Date = date,
            Description = description,
            PaymentMethod = paymentMethod,
            CategoryId = categoryId,
            UserId = userId,
            ExpenseGroupId = expenseGroupId
        };
        
        return await _expenseRepository.CreateAsync(expense, cancellationToken);
    }
    
    public async Task<ErrorOr<Expense>> UpdateAsync(int id, decimal amount, DateTime date, string? description, PaymentMethod paymentMethod, int categoryId, int? expenseGroupId, CancellationToken cancellationToken)
    {
        var existingResult = await _expenseRepository.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsError)
        {
            return existingResult.Errors;
        }
        
        var validationResult = ExpenseValidator.ValidateExpenseRequest(amount, date, categoryId, existingResult.Value.UserId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        var expense = new Expense
        {
            Id = id,
            Amount = amount,
            Date = date,
            Description = description,
            PaymentMethod = paymentMethod,
            CategoryId = categoryId,
            UserId = existingResult.Value.UserId,
            ExpenseGroupId = expenseGroupId
        };
        
        return await _expenseRepository.UpdateAsync(expense, cancellationToken);
    }
    
    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return await _expenseRepository.DeleteAsync(id, cancellationToken);
    }
}

