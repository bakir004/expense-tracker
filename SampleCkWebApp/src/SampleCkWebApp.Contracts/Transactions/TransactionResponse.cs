using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Contracts.Transactions;

public class TransactionResponse
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    /// <summary>
    /// Type of transaction: "EXPENSE" or "INCOME"
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;
    
    /// <summary>
    /// The absolute amount (always positive)
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// The signed amount: negative for expenses, positive for income
    /// </summary>
    public decimal SignedAmount { get; set; }
    
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Brief description of what/why this transaction occurred
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional longer description with additional details
    /// </summary>
    public string? Notes { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    // Optional fields
    public int? CategoryId { get; set; }
    public int? TransactionGroupId { get; set; }
    public string? IncomeSource { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
