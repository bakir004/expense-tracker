using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Strongly-typed result of parsing <see cref="UpdateTransactionRequest"/>.
/// Ready to pass to the application service.
/// </summary>
public sealed record ParsedUpdateTransactionInput(
    TransactionType TransactionType,
    decimal Amount,
    DateOnly Date,
    string Subject,
    string? Notes,
    PaymentMethod PaymentMethod,
    int? CategoryId,
    int? TransactionGroupId,
    string? IncomeSource);
