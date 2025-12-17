using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Incomes;

public class UpdateIncomeRequest
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Source { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime Date { get; set; }
}

