using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Validated and parsed filter model for querying transactions.
/// This is the internal representation created from <see cref="TransactionFilterRequest"/>.
/// All values are validated and strongly-typed for safe use in queries.
/// </summary>
public record TransactionFilter
{
    /// <summary>
    /// Filter by transaction type (EXPENSE or INCOME).
    /// Null means no filtering by type.
    /// </summary>
    public TransactionType? TransactionType { get; init; }

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
    /// </summary>
    public DateOnly? DateFrom { get; init; }

    /// <summary>
    /// Filter by date range end (inclusive).
    /// </summary>
    public DateOnly? DateTo { get; init; }

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
    /// Null or empty means no filtering by payment method.
    /// </summary>
    public IReadOnlyList<PaymentMethod>? PaymentMethods { get; init; }

    /// <summary>
    /// Filter by category IDs.
    /// Null or empty means no filtering by category.
    /// </summary>
    public IReadOnlyList<int>? CategoryIds { get; init; }

    /// <summary>
    /// When true, only return transactions without a category.
    /// </summary>
    public bool Uncategorized { get; init; }

    /// <summary>
    /// Filter by transaction group IDs.
    /// Null or empty means no filtering by transaction group.
    /// </summary>
    public IReadOnlyList<int>? TransactionGroupIds { get; init; }

    /// <summary>
    /// When true, only return transactions without a transaction group.
    /// </summary>
    public bool Ungrouped { get; init; }

    /// <summary>
    /// Field to sort by.
    /// </summary>
    public TransactionSortField SortBy { get; init; } = TransactionSortField.Date;

    /// <summary>
    /// When true, sort in descending order.
    /// Default is true (newest/highest first).
    /// </summary>
    public bool SortDescending { get; init; } = true;

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Calculates the number of items to skip for pagination.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Returns true if any filter criteria are specified (excluding sorting and pagination).
    /// </summary>
    public bool HasFilters =>
        TransactionType.HasValue ||
        MinAmount.HasValue ||
        MaxAmount.HasValue ||
        DateFrom.HasValue ||
        DateTo.HasValue ||
        !string.IsNullOrWhiteSpace(SubjectContains) ||
        !string.IsNullOrWhiteSpace(NotesContains) ||
        (PaymentMethods?.Count > 0) ||
        (CategoryIds?.Count > 0) ||
        Uncategorized ||
        (TransactionGroupIds?.Count > 0) ||
        Ungrouped;
}

/// <summary>
/// Valid fields for sorting transactions.
/// </summary>
public enum TransactionSortField
{
    /// <summary>Sort by transaction date (primary), then by created time.</summary>
    Date,

    /// <summary>Sort by absolute amount.</summary>
    Amount,

    /// <summary>Sort by subject alphabetically.</summary>
    Subject,

    /// <summary>Sort (Group) by category.</summary>
    CategoryId,

    /// <summary>Sort (Group) by transaction group.</summary>
    TransactionGroupId,

    /// <summary>Sort by payment method.</summary>
    PaymentMethod,

    /// <summary>Sort by creation timestamp.</summary>
    CreatedAt,

    /// <summary>Sort by last update timestamp.</summary>
    UpdatedAt
}
