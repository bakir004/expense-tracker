using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.ExpenseGroups.Data;
using SampleCkWebApp.Application.ExpenseGroups.Interfaces.Application;
using SampleCkWebApp.Application.ExpenseGroups.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.ExpenseGroups;

public class ExpenseGroupService : IExpenseGroupService
{
    private readonly IExpenseGroupRepository _expenseGroupRepository;

    public ExpenseGroupService(IExpenseGroupRepository expenseGroupRepository)
    {
        _expenseGroupRepository = expenseGroupRepository ?? throw new ArgumentNullException(nameof(expenseGroupRepository));
    }
    
    public async Task<ErrorOr<GetExpenseGroupsResult>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _expenseGroupRepository.GetAllAsync(cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }
        
        return new GetExpenseGroupsResult
        {
            ExpenseGroups = result.Value
        };
    }
    
    public async Task<ErrorOr<ExpenseGroup>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _expenseGroupRepository.GetByIdAsync(id, cancellationToken);
    }
    
    public async Task<ErrorOr<List<ExpenseGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _expenseGroupRepository.GetByUserIdAsync(userId, cancellationToken);
    }
    
    public async Task<ErrorOr<ExpenseGroup>> CreateAsync(string name, string? description, int userId, CancellationToken cancellationToken)
    {
        var validationResult = ExpenseGroupValidator.ValidateExpenseGroupRequest(name, description, userId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        var expenseGroup = new ExpenseGroup
        {
            Name = name,
            Description = description,
            UserId = userId
        };
        
        return await _expenseGroupRepository.CreateAsync(expenseGroup, cancellationToken);
    }
    
    public async Task<ErrorOr<ExpenseGroup>> UpdateAsync(int id, string name, string? description, CancellationToken cancellationToken)
    {
        var validationResult = ExpenseGroupValidator.ValidateExpenseGroupRequest(name, description, 1); // userId not needed for update validation
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        var existingResult = await _expenseGroupRepository.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsError)
        {
            return existingResult.Errors;
        }
        
        var expenseGroup = new ExpenseGroup
        {
            Id = id,
            Name = name,
            Description = description,
            UserId = existingResult.Value.UserId
        };
        
        return await _expenseGroupRepository.UpdateAsync(expenseGroup, cancellationToken);
    }
    
    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return await _expenseGroupRepository.DeleteAsync(id, cancellationToken);
    }
}

