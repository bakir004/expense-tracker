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
}
