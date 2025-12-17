using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.Incomes.Data;

public class GetIncomesResult
{
    public List<Income> Incomes { get; set; } = new();
}

