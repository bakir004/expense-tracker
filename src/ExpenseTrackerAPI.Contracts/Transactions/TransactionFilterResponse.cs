namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Response contract for filtered transaction results.
/// </summary>
public record TransactionFilterResponse
{
    /// <summary>
    /// The list of transactions matching the filter criteria.
    /// </summary>
    public required IReadOnlyList<TransactionResponse> Transactions { get; init; }

    /// <summary>
    /// The total count of transactions matching the filter criteria.
    /// Useful for pagination and displaying total results.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// The current page size
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// The current page number
    /// </summary>
    public required int CurrentPage { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public required int TotalPages { get; init; }
}
