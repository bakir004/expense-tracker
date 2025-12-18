namespace SampleCkWebApp.Domain.Entities;

/// <summary>
/// Represents a financial transaction (expense or income) in the expense tracker system.
/// This is a unified model that handles both expenses and income with type-specific fields.
/// </summary>
public class Transaction
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    /// <summary>
    /// The type of transaction: Expense or Income
    /// </summary>
    public TransactionType TransactionType { get; set; }
    
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
    /// Brief description of what/why this transaction occurred.
    /// Examples: "Grocery shopping", "Monthly salary", "Flight tickets"
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional longer description with additional details.
    /// </summary>
    public string? Notes { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    /// <summary>
    /// Running sum of all signed_amounts up to and including this transaction.
    /// Actual balance = User.InitialBalance + CumulativeDelta
    /// </summary>
    public decimal CumulativeDelta { get; set; }
    
    /// <summary>
    /// The actual balance after this transaction (computed: InitialBalance + CumulativeDelta).
    /// This is NOT stored in the database - it's computed when mapping to response.
    /// </summary>
    public decimal? BalanceAfter { get; set; }
    
    /// <summary>
    /// Sequence number for deterministic ordering of transactions.
    /// </summary>
    public long Seq { get; set; }
    
    // ============================================================
    // Optional fields
    // ============================================================
    
    /// <summary>
    /// Category of the transaction. Required for expenses, optional for income.
    /// </summary>
    public int? CategoryId { get; set; }
    
    /// <summary>
    /// Transaction group for grouping related transactions (e.g., vacation, project, wedding).
    /// Applicable for both expenses and income.
    /// </summary>
    public int? TransactionGroupId { get; set; }
    
    /// <summary>
    /// Source of income (e.g., "ABC Corporation", "Freelance Client").
    /// Only applicable for income.
    /// </summary>
    public string? IncomeSource { get; set; }
    
    // ============================================================
    // Timestamps
    // ============================================================
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}
