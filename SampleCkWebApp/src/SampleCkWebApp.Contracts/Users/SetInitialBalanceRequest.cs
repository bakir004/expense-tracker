namespace SampleCkWebApp.Users;

/// <summary>
/// Request model for setting a user's initial balance
/// </summary>
public class SetInitialBalanceRequest
{
    /// <summary>
    /// The initial balance to set for the user
    /// </summary>
    /// <example>1000.00</example>
    public decimal InitialBalance { get; set; }
}

