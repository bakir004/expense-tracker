namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Response contract for transaction data.
/// </summary>
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
