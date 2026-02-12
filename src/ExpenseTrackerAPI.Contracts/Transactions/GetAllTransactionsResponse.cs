namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Response contract for paginated transaction lists.
/// </summary>
public record GetAllTransactionsResponse(
    IEnumerable<TransactionResponse> Transactions,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasNextPage,
    bool HasPreviousPage);
