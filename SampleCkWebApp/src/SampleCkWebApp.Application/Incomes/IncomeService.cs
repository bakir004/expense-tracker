using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Incomes.Data;
using SampleCkWebApp.Application.Incomes.Interfaces.Application;
using SampleCkWebApp.Application.Incomes.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.Incomes;

public class IncomeService : IIncomeService
{
    private readonly IIncomeRepository _incomeRepository;

    public IncomeService(IIncomeRepository incomeRepository)
    {
        _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
    }
    
    public async Task<ErrorOr<GetIncomesResult>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _incomeRepository.GetAllAsync(cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }
        
        return new GetIncomesResult
        {
            Incomes = result.Value
        };
    }
    
    public async Task<ErrorOr<Income>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _incomeRepository.GetByIdAsync(id, cancellationToken);
    }
    
    public async Task<ErrorOr<List<Income>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _incomeRepository.GetByUserIdAsync(userId, cancellationToken);
    }
    
    public async Task<ErrorOr<Income>> CreateAsync(decimal amount, string? description, string? source, PaymentMethod paymentMethod, int userId, DateTime date, CancellationToken cancellationToken)
    {
        var validationResult = IncomeValidator.ValidateIncomeRequest(amount, date, userId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        var income = new Income
        {
            Amount = amount,
            Description = description,
            Source = source,
            PaymentMethod = paymentMethod,
            UserId = userId,
            Date = date
        };
        
        return await _incomeRepository.CreateAsync(income, cancellationToken);
    }
    
    public async Task<ErrorOr<Income>> UpdateAsync(int id, decimal amount, string? description, string? source, PaymentMethod paymentMethod, DateTime date, CancellationToken cancellationToken)
    {
        var existingResult = await _incomeRepository.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsError)
        {
            return existingResult.Errors;
        }
        
        var validationResult = IncomeValidator.ValidateIncomeRequest(amount, date, existingResult.Value.UserId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        var income = new Income
        {
            Id = id,
            Amount = amount,
            Description = description,
            Source = source,
            PaymentMethod = paymentMethod,
            UserId = existingResult.Value.UserId,
            Date = date
        };
        
        return await _incomeRepository.UpdateAsync(income, cancellationToken);
    }
    
    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return await _incomeRepository.DeleteAsync(id, cancellationToken);
    }
}

