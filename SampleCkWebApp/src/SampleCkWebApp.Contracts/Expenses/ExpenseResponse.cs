using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Expenses;

public class ExpenseResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public int CategoryId { get; set; }
    public int UserId { get; set; }
    public int? ExpenseGroupId { get; set; }
}

