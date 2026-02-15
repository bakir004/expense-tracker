namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Request contract for filtering and sorting transactions.
/// All filter properties are optional - when null, no filtering is applied for that property.
/// Multiple filters are combined with AND logic.
/// </summary>
public record TransactionFilterRequest
{
    /// <summary>
    /// Filter by transaction type: "EXPENSE" or "INCOME".
    /// Case-insensitive.
    /// </summary>
    public string? TransactionType { get; init; }

    /// <summary>
    /// Filter by minimum amount (inclusive).
    /// Filters on the absolute amount value.
    /// </summary>
    public decimal? MinAmount { get; init; }

    /// <summary>
    /// Filter by maximum amount (inclusive).
    /// Filters on the absolute amount value.
    /// </summary>
    public decimal? MaxAmount { get; init; }

    /// <summary>
    /// Filter by date range start (inclusive).
    /// Format: yyyy-MM-dd
    /// </summary>
    public string? DateFrom { get; init; }

    /// <summary>
    /// Filter by date range end (inclusive).
    /// Format: yyyy-MM-dd
    /// </summary>
    public string? DateTo { get; init; }

    /// <summary>
    /// Filter by subject containing this text (case-insensitive).
    /// </summary>
    public string? SubjectContains { get; init; }

    /// <summary>
    /// Filter by notes containing this text (case-insensitive).
    /// </summary>
    public string? NotesContains { get; init; }

    /// <summary>
    /// Filter by payment methods.
    /// Valid values: CASH, DEBIT_CARD, CREDIT_CARD, BANK_TRANSFER, MOBILE_PAYMENT, PAYPAL, CRYPTO, OTHER.
    /// Multiple values create an OR condition (matches any of the specified methods).
    /// </summary>
    public string[]? PaymentMethods { get; init; }

    /// <summary>
    /// Filter by category IDs.
    /// Multiple values create an OR condition (matches any of the specified categories).
    /// Use null or empty array to not filter by category.
    /// </summary>
    public int[]? CategoryIds { get; init; }

    /// <summary>
    /// Filter to include only transactions without a category.
    /// When true, only uncategorized transactions are returned.
    /// When false or null, no filtering on category presence is applied.
    /// </summary>
    public bool? Uncategorized { get; init; }

    /// <summary>
    /// Filter by transaction group IDs.
    /// Multiple values create an OR condition (matches any of the specified groups).
    /// </summary>
    public int[]? TransactionGroupIds { get; init; }

    /// <summary>
    /// Filter to include only transactions without a transaction group.
    /// When true, only ungrouped transactions are returned.
    /// When false or null, no filtering on group presence is applied.
    /// </summary>
    public bool? Ungrouped { get; init; }

    /// <summary>
    /// Field to sort by.
    /// Valid values: date, amount, subject, paymentMethod, createdAt, updatedAt.
    /// Default: date
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort direction: "asc" or "desc".
    /// Default: desc (newest first for date, highest first for amount)
    /// </summary>
    public string? SortDirection { get; init; }

    /// <summary>
    /// Page number for pagination (1-based).
    /// Default: 1
    /// </summary>
    public int? Page { get; init; }

    /// <summary>
    /// Number of items per page.
    /// Default: 20, Maximum: 50
    /// </summary>
    public int? PageSize { get; init; }
}
