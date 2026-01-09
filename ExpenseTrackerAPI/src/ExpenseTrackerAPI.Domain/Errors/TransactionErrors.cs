using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Errors;

/// <summary>
/// Domain-specific errors for transaction operations.
/// </summary>
public static class TransactionErrors
{
    public static Error NotFound => Error.NotFound(
        code: "transaction",
        description: "Transaction not found.");
    
    public static Error InvalidAmount => Error.Validation(
        code: "amount",
        description: "Transaction amount must be greater than zero.");
    
    public static Error InvalidDate => Error.Validation(
        code: "date",
        description: "Transaction date cannot be in the future.");
    
    public static Error ExpenseMissingCategory => Error.Validation(
        code: "categoryId",
        description: "Expense transactions must have a category.");
    
    public static Error InvalidTransactionType => Error.Validation(
        code: "transactionType",
        description: "Invalid transaction type. Must be 'EXPENSE' or 'INCOME'.");
    
    public static Error InvalidSubject => Error.Validation(
        code: "subject",
        description: "Transaction subject is required and cannot be empty.");
}

