using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

/// <summary>
/// Domain-specific errors for transaction operations.
/// </summary>
public static class TransactionErrors
{
    public static Error NotFound => Error.NotFound(
        code: "Transaction.NotFound",
        description: "Transaction not found.");
    
    public static Error InvalidAmount => Error.Validation(
        code: "Transaction.InvalidAmount",
        description: "Transaction amount must be greater than zero.");
    
    public static Error InvalidDate => Error.Validation(
        code: "Transaction.InvalidDate",
        description: "Transaction date cannot be in the future.");
    
    public static Error ExpenseMissingCategory => Error.Validation(
        code: "Transaction.ExpenseMissingCategory",
        description: "Expense transactions must have a category.");
    
    public static Error InvalidTransactionType => Error.Validation(
        code: "Transaction.InvalidType",
        description: "Invalid transaction type. Must be 'EXPENSE' or 'INCOME'.");
}

