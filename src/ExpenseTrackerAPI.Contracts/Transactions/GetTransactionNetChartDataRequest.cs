/// <summary>
/// Request model for getting transaction net chart data.
/// This is used for generating data for a chart that shows the net amount (income - expenses) over time.
/// </summary>
public record GetTransactionNetChartDataRequest
{
    /// <summary>
    /// Filter by date range start (inclusive).
    /// </summary>
    public DateOnly? DateFrom { get; init; }

    /// <summary>
    /// Filter by date range end (inclusive).
    /// </summary>
    public DateOnly? DateTo { get; init; }
}
