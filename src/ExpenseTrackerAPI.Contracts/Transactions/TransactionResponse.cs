namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Response contract for transaction data.
/// </summary>
/// <param name="Id">Unique identifier for the transaction</param>
/// <param name="UserId">ID of the user who owns this transaction</param>
/// <param name="TransactionType">Type of transaction: EXPENSE or INCOME</param>
/// <param name="Amount">Absolute amount value (always positive, e.g., 50.00)</param>
/// <param name="SignedAmount">Amount with sign - negative for expenses, positive for income (e.g., -50.00 or +50.00)</param>
/// <param name="Date">Date when the transaction occurred</param>
/// <param name="Subject">Brief description of the transaction (e.g., "Grocery shopping", "Monthly salary")</param>
/// <param name="Notes">Optional detailed notes or additional information about the transaction</param>
/// <param name="PaymentMethod">Payment method used: CASH, DEBIT_CARD, CREDIT_CARD, BANK_TRANSFER, MOBILE_PAYMENT, PAYPAL, CRYPTO, or OTHER</param>
/// <param name="CumulativeDelta">Running balance at this transaction - cumulative sum of all signed amounts up to this point</param>
/// <param name="CategoryId">ID of the associated category (null if uncategorized)</param>
/// <param name="TransactionGroupId">ID of the associated transaction group (null if ungrouped)</param>
/// <param name="CreatedAt">UTC timestamp when the transaction was created</param>
/// <param name="UpdatedAt">UTC timestamp when the transaction was last updated</param>
public record TransactionResponse(
    int Id,
    int UserId,
    string TransactionType,
    decimal Amount,
    decimal SignedAmount,
    DateOnly Date,
    string Subject,
    string? Notes,
    string PaymentMethod,
    decimal CumulativeDelta,
    int? CategoryId,
    int? TransactionGroupId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
