using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Application.TransactionGroups.Data;

namespace ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Application;

public interface ITransactionGroupService
{
    Task<ErrorOr<GetTransactionGroupsResult>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<TransactionGroup>> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<List<TransactionGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<TransactionGroup>> CreateAsync(string name, string? description, int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<TransactionGroup>> UpdateAsync(int id, string name, string? description, CancellationToken cancellationToken);
    
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

