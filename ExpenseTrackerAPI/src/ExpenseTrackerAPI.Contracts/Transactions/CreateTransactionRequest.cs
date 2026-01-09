using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.Transactions;

public class CreateTransactionRequest
{
    /// <summary>
    /// Type of transaction: "EXPENSE" or "INCOME"
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;
    
    /// <summary>
    /// The amount (always positive)
    /// </summary>
    public decimal Amount { get; set; }
    
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Brief description of what/why this transaction occurred.
    /// Examples: "Grocery shopping", "Monthly salary"
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional longer description with additional details
    /// </summary>
    public string? Notes { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    /// <summary>
    /// Required for expenses, optional for income
    /// </summary>
    public int? CategoryId { get; set; }
    
    /// <summary>
    /// Optional: group related transactions together (trips, projects, etc.)
    /// </summary>
    public int? TransactionGroupId { get; set; }
    
    /// <summary>
    /// Optional: source of income (e.g., "ABC Corporation")
    /// </summary>
    public string? IncomeSource { get; set; }
    
    /// <summary>
    /// The user ID (typically from auth context, but included here for flexibility)
    /// </summary>
    public int UserId { get; set; }
}
