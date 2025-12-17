using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.ExpenseGroups.Data;

namespace SampleCkWebApp.Application.ExpenseGroups.Interfaces.Application;

public interface IExpenseGroupService
{
    Task<ErrorOr<GetExpenseGroupsResult>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<ExpenseGroup>> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<List<ExpenseGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<ExpenseGroup>> CreateAsync(string name, string? description, int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<ExpenseGroup>> UpdateAsync(int id, string name, string? description, CancellationToken cancellationToken);
    
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

