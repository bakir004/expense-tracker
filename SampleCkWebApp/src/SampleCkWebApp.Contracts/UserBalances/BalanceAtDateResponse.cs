namespace SampleCkWebApp.UserBalances;

/// <summary>
/// Response model for balance at a specific date
/// </summary>
public class BalanceAtDateResponse
{
    /// <summary>
    /// User ID
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Target date for the balance calculation
    /// </summary>
    public DateTime TargetDate { get; set; }
    
    /// <summary>
    /// Calculated balance at the target date (initial balance + income up to date - expenses up to date)
    /// </summary>
    public decimal Balance { get; set; }
}

