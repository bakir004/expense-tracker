namespace ExpenseTrackerAPI.Domain.Entities;

/// <summary>
/// Represents a user in the expense tracker system.
/// </summary>
public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// The user's starting balance when they began tracking.
    /// Actual balance = InitialBalance + his last transaction's CumulativeDelta
    /// </summary>
    public decimal InitialBalance { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
