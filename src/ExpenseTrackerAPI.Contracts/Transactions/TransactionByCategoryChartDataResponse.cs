namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Response model for transactions by category chart data.
/// This is used for generating data for a chart that shows the net amount (income - expenses) over time.
/// </summary>
public record TransactionByCategoryChartDataResponse
{
    /// <summary>
    /// The list of data points for the chart, each representing a date and its corresponding net amount.
    /// </summary>
    public required List<TransactionByCategoryChartData> ChartData { get; init; }
}
