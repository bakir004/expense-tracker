using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Incomes;

public class IncomeResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Source { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; }
}

