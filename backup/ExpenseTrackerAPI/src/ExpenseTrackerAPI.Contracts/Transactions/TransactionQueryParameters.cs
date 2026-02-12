namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Query parameters for filtering and sorting transactions.
/// </summary>
public class TransactionQueryParameters
{
    /// <summary>
    /// Fuzzy search on subject field
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Filter by multiple category IDs (comma-separated in query string)
    /// </summary>
    public int[]? CategoryIds { get; set; }

    /// <summary>
    /// Filter by multiple payment method values (comma-separated in query string)
    /// Values: 0=Cash, 1=DebitCard, 2=CreditCard, 3=BankTransfer, 4=MobilePayment, 5=PayPal, 6=Crypto, 7=Other
    /// </summary>
    public int[]? PaymentMethods { get; set; }

    /// <summary>
    /// Filter by transaction type: "EXPENSE" or "INCOME"
    /// </summary>
    public string? TransactionType { get; set; }

    /// <summary>
    /// Filter by date range start (inclusive)
    /// Format: dd-MM-yyyy
    /// </summary>
    public string? DateFrom { get; set; }

    /// <summary>
    /// Filter by date range end (inclusive)
    /// Format: dd-MM-yyyy
    /// </summary>
    public string? DateTo { get; set; }

    /// <summary>
    /// Secondary sort field (date is always primary).
    /// Valid values: subject, paymentMethod, category, amount
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction: "asc" or "desc" (default: "desc")
    /// Applies to both date (primary) and secondary sort.
    /// </summary>
    public string? SortDirection { get; set; }
}
