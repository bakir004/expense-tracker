using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Strongly-typed result of parsing <see cref="CreateTransactionRequest"/>.
/// Ready to pass to the application service (no more string/enum parsing in controller).
/// </summary>
public sealed record ParsedCreateTransactionInput(
    int UserId,
    TransactionType TransactionType,
    decimal Amount,
    DateOnly Date,
    string Subject,
    string? Notes,
    PaymentMethod PaymentMethod,
    int? CategoryId,
    int? TransactionGroupId,
    string? IncomeSource);
