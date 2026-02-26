namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Request model for getting transaction net chart data.
/// This is used for generating data for a chart that shows the net amount (income - expenses) over time.
/// </summary>
public record TransactionByCategoryChartData
{
    /// <summary>
    /// The category id for this data point.
    /// </summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// The net expenses for this category.
    /// </summary>
    public decimal NetExpenses { get; init; }

    /// <summary>
    /// Transactions that contribute to this data point.
    /// </summary>
    public required List<TransactionResponse> Transactions { get; init; }
}
