using System.ComponentModel.DataAnnotations;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.Transactions;

public class CreateTransactionRequest
{
    /// <summary>
    /// Type of transaction: "EXPENSE" or "INCOME"
    /// </summary>
    [Required(ErrorMessage = "Transaction type is required.")]
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// The amount (always positive)
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction date in yyyy-MM-dd format (e.g. 2024-01-01).
    /// </summary>
    [Required(ErrorMessage = "Date is required.")]
    public DateOnly Date { get; set; }

    /// <summary>
    /// Brief description of what/why this transaction occurred.
    /// Examples: "Grocery shopping", "Monthly salary"
    /// </summary>
    [Required(ErrorMessage = "Subject is required.")]
    [MinLength(1, ErrorMessage = "Subject cannot be empty.")]
    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Optional longer description with additional details
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Payment method is required.")]
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Optional category ID
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be positive when provided.")]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Optional: group related transactions together (trips, projects, etc.)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Transaction group ID must be positive when provided.")]
    public int? TransactionGroupId { get; set; }
}
