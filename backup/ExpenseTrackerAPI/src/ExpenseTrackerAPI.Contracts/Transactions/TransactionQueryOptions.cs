using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Parsed and validated options for filtering and sorting transactions (incoming from API query).
/// </summary>
public class TransactionQueryOptions
{
    public string? Subject { get; init; }
    public IReadOnlyList<int>? CategoryIds { get; init; }
    public IReadOnlyList<PaymentMethod>? PaymentMethods { get; init; }
    public TransactionType? TransactionType { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    /// <summary>One of: subject, paymentmethod, category, amount</summary>
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = true;
}
