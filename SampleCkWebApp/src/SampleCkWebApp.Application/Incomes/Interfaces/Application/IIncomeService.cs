using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Incomes.Data;

namespace SampleCkWebApp.Application.Incomes.Interfaces.Application;

public interface IIncomeService
{
    Task<ErrorOr<GetIncomesResult>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<Income>> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<List<Income>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<Income>> CreateAsync(decimal amount, string? description, string? source, PaymentMethod paymentMethod, int userId, DateTime date, CancellationToken cancellationToken);
    
    Task<ErrorOr<Income>> UpdateAsync(int id, decimal amount, string? description, string? source, PaymentMethod paymentMethod, DateTime date, CancellationToken cancellationToken);
    
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

