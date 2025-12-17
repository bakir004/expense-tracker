using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Incomes.Data;
using SampleCkWebApp.Incomes;

namespace SampleCkWebApp.Application.Incomes.Mappings;

public static class IncomeMappings
{
    public static GetIncomesResponse ToResponse(this GetIncomesResult result)
    {
        return new GetIncomesResponse
        {
            Incomes = result.Incomes.Select(i => i.ToResponse()).ToList(),
            TotalCount = result.Incomes.Count
        };
    }
    
    public static IncomeResponse ToResponse(this Income income)
    {
        return new IncomeResponse
        {
            Id = income.Id,
            Amount = income.Amount,
            Description = income.Description,
            Source = income.Source,
            PaymentMethod = income.PaymentMethod,
            UserId = income.UserId,
            Date = income.Date
        };
    }
}

