namespace ExpenseTrackerAPI.Domain.Entities;

/// <summary>
/// Represents the type of financial transaction.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Money going out (reduces balance)
    /// </summary>
    EXPENSE,

    /// <summary>
    /// Money coming in (increases balance)
    /// </summary>
    INCOME
}

