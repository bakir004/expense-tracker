namespace ExpenseTrackerAPI.Users;

/// <summary>
/// Response model for user balance information
/// </summary>
public class UserBalanceResponse
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// User's starting balance when they began tracking
    /// </summary>
    public decimal InitialBalance { get; set; }
    
    /// <summary>
    /// Cumulative sum of all transaction effects (from latest transaction)
    /// </summary>
    public decimal CumulativeDelta { get; set; }
    
    /// <summary>
    /// Current balance = InitialBalance + CumulativeDelta
    /// </summary>
    public decimal CurrentBalance { get; set; }
}

