using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Domain.Entities;

/// <summary>
/// Represents a financial transaction (expense or income) in the expense tracker system.
/// This is a unified model that handles both expenses and income with type-specific fields.
/// </summary>
public class Transaction
{
    // Private constructor for EF Core
    private Transaction() { }

    /// <summary>
    /// Creates a new transaction with business rule validation
    /// </summary>
    /// <param name="userId">The user who owns this transaction</param>
    /// <param name="transactionType">Type of transaction (EXPENSE or INCOME)</param>
    /// <param name="amount">The transaction amount (must be positive)</param>
    /// <param name="date">The date of the transaction</param>
    /// <param name="subject">Brief description of the transaction</param>
    /// <param name="paymentMethod">How the transaction was paid</param>
    /// <param name="notes">Optional additional notes</param>
    /// <param name="categoryId">Category ID (optional for both expenses and income)</param>
    /// <param name="transactionGroupId">Optional transaction group ID</param>

    public Transaction(
        int userId,
        TransactionType transactionType,
        decimal amount,
        DateOnly date,
        string subject,
        PaymentMethod paymentMethod,
        string? notes = null,
        int? categoryId = null,
        int? transactionGroupId = null)
    {
        ValidateBusinessRules(userId, transactionType, amount, date, subject, categoryId);

        UserId = userId;
        TransactionType = transactionType;
        Amount = amount;
        SignedAmount = transactionType == TransactionType.EXPENSE ? -amount : amount;
        Date = date;
        Subject = subject.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        PaymentMethod = paymentMethod;
        CategoryId = categoryId;
        TransactionGroupId = transactionGroupId;

        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;

        CumulativeDelta = 0;
    }

    /// <summary>
    /// Updates the cumulative delta for this transaction (called by domain services)
    /// </summary>
    public void UpdateCumulativeDelta(decimal cumulativeDelta)
    {
        CumulativeDelta = cumulativeDelta;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates business rules for transaction creation
    /// Throws ArgumentException if validation fails
    /// </summary>
    private static void ValidateBusinessRules(
        int userId,
        TransactionType transactionType,
        decimal amount,
        DateOnly date,
        string subject,
        int? categoryId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be a positive integer.", nameof(userId));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Transaction amount must be greater than zero.", nameof(amount));
        }

        var minDate = new DateOnly(1900, 1, 1);
        var maxDate = DateOnly.FromDateTime(DateTime.Now.AddYears(1));

        if (date < minDate || date > maxDate)
        {
            throw new ArgumentException($"Transaction date must be between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}.", nameof(date));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Transaction subject is required and cannot be empty.", nameof(subject));
        }

        if (subject.Trim().Length > 255)
        {
            throw new ArgumentException("Transaction subject cannot exceed 255 characters.", nameof(subject));
        }

        if (transactionType != TransactionType.EXPENSE && transactionType != TransactionType.INCOME)
        {
            throw new ArgumentException("Invalid transaction type. Must be 'EXPENSE' or 'INCOME'.", nameof(transactionType));
        }

        if (categoryId.HasValue && categoryId.Value <= 0)
        {
            throw new ArgumentException("Category ID must be a positive integer when provided.", nameof(categoryId));
        }
    }

    public int Id { get; }

    public int UserId { get; }

    /// <summary>
    /// The type of transaction: Expense or Income
    /// </summary>
    public TransactionType TransactionType { get; }

    /// <summary>
    /// The absolute amount (always positive)
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// The signed amount: negative for expenses, positive for income
    /// </summary>
    public decimal SignedAmount { get; }

    /// <summary>
    /// The date of the transaction (date only, no time component).
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Brief description of what/why this transaction occurred.
    /// Examples: "Grocery shopping", "Monthly salary", "Flight tickets"
    /// </summary>
    public string Subject { get; } = string.Empty;

    /// <summary>
    /// Optional longer description with additional details.
    /// </summary>
    public string? Notes { get; }

    public PaymentMethod PaymentMethod { get; }

    /// <summary>
    /// Running sum of all signed_amounts up to and including this transaction.
    /// Actual balance = User.InitialBalance + CumulativeDelta
    /// </summary>
    public decimal CumulativeDelta { get; private set; }

    /// <summary>
    /// Category of the transaction. Optional for both expenses and income.
    /// </summary>
    public int? CategoryId { get; }

    /// <summary>
    /// Transaction group for grouping related transactions (e.g., vacation, project, wedding).
    /// Applicable for both expenses and income.
    /// </summary>
    public int? TransactionGroupId { get; }

    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }
}
