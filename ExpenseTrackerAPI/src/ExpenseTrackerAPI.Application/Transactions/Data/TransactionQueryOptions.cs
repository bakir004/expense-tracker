using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Transactions.Data;

/// <summary>
/// Parsed and validated options for filtering and sorting transactions (used by Application/Infrastructure).
/// </summary>
public class TransactionQueryOptions
{
    public string? Subject { get; init; }
    public IReadOnlyList<int>? CategoryIds { get; init; }
    public IReadOnlyList<PaymentMethod>? PaymentMethods { get; init; }
    public TransactionType? TransactionType { get; init; }
    public DateTime? DateFromUtc { get; init; }
    public DateTime? DateToUtc { get; init; }
    public string? SortBy { get; init; }  // "subject" | "paymentMethod" | "category" | "amount"
    public bool SortDescending { get; init; } = true;  // date and secondary sort direction
}
