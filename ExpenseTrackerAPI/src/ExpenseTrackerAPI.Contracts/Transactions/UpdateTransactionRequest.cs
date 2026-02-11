using System.ComponentModel.DataAnnotations;
using ExpenseTrackerAPI.Contracts.Validation;

namespace ExpenseTrackerAPI.Contracts.Transactions;

public class UpdateTransactionRequest
{
    /// <summary>
    /// Type of transaction: "EXPENSE" or "INCOME"
    /// </summary>
    [Required(ErrorMessage = "Transaction type is required.")]
    [RegularExpression("^(EXPENSE|INCOME)$", ErrorMessage = "Transaction type must be 'EXPENSE' or 'INCOME'.")]
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// The amount (always positive)
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction date in yyyy-MM-dd format (e.g. 2024-12-31).
    /// </summary>
    [Required(ErrorMessage = "Date is required.")]
    [DateFormat]
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of what/why this transaction occurred
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
    [RegularExpression("^(CASH|DEBIT_CARD|CREDIT_CARD|BANK_TRANSFER|MOBILE_PAYMENT|PAYPAL|CRYPTO|OTHER)$", ErrorMessage = "Invalid payment method.")]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Required for expenses, optional for income
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be positive when provided.")]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Optional: group related transactions together
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Transaction group ID must be positive when provided.")]
    public int? TransactionGroupId { get; set; }

    /// <summary>
    /// Optional: source of income (e.g., "ABC Corporation")
    /// </summary>
    [MaxLength(255)]
    public string? IncomeSource { get; set; }
}
