namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Request model for getting transaction net chart data.
/// This is used for generating data for a chart that shows the net amount (income - expenses) over time.
/// </summary>
public record TransactionNetChartData
{
    /// <summary>
    /// The date for this data point.
    /// </summary>
    public required String Date { get; init; }

    /// <summary>
    /// The net amount for this date (income - expenses).
    /// Positive means net income, negative means net expense.
    /// </summary>
    public decimal NetAmount { get; init; }

    /// <summary>
    /// The net expenses for this date.
    /// </summary>
    public decimal NetExpenses { get; init; }

    /// <summary>
    /// The net income for this date.
    /// </summary>
    public decimal NetIncome { get; init; }

    /// <summary>
    /// Transactions that contribute to this data point.
    /// </summary>
    public required List<TransactionResponse> Transactions { get; init; }
}
