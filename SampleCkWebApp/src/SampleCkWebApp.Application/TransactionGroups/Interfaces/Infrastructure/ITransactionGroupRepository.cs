using ErrorOr;
using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.TransactionGroups.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for transaction group persistence operations.
/// This interface is defined in the Application layer but implemented in the Infrastructure layer.
/// </summary>
public interface ITransactionGroupRepository
{
    Task<ErrorOr<List<TransactionGroup>>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<TransactionGroup>> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<List<TransactionGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<TransactionGroup>> CreateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken);
    
    Task<ErrorOr<TransactionGroup>> UpdateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken);
    
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

