namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Response model for transaction net chart data.
/// This is used for generating data for a chart that shows the net amount (income - expenses) over time.
/// </summary>
public record TransactionNetChartDataResponse
{
    /// <summary>
    /// The list of data points for the chart, each representing a date and its corresponding net amount.
    /// </summary>
    public required List<TransactionNetChartData> ChartData { get; init; }
}
