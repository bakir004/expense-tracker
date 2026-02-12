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

    public static Error ExpenseMissingCategory => Error.Validation(
        code: "categoryId",
        description: "Expense transactions must have a category.");

    public static Error InvalidTransactionType => Error.Validation(
        code: "transactionType",
        description: "Invalid transaction type. Must be 'EXPENSE' or 'INCOME'.");

    public static Error InvalidSubject => Error.Validation(
        code: "subject",
        description: "Transaction subject is required and cannot be empty.");

    public static Error InvalidUserId => Error.Validation(
        code: "userId",
        description: "User ID must be a positive integer.");

    public static Error InvalidDate => Error.Validation(
        code: "date",
        description: "Transaction date is outside the allowed range.");

    public static Error SubjectTooLong => Error.Validation(
        code: "subject",
        description: "Transaction subject cannot exceed 255 characters.");

    public static Error ExpenseWithIncomeSource => Error.Validation(
        code: "incomeSource",
        description: "Expense transactions cannot have an income source.");

    public static Error IncomeWithBothCategoryAndSource => Error.Validation(
        code: "categoryId_incomeSource",
        description: "Income transactions cannot have both a category and an income source.");

    public static Error InvalidCategoryId => Error.Validation(
        code: "categoryId",
        description: "Category ID must be a positive integer when provided.");

    public static Error IncomeSourceTooLong => Error.Validation(
        code: "incomeSource",
        description: "Income source cannot exceed 100 characters.");

    public static Error Unauthorized => Error.Validation(
        code: "unauthorized",
        description: "You are not authorized to access this transaction.");

    public static Error ConcurrencyConflict => Error.Conflict(
        code: "concurrency",
        description: "Transaction was modified by another process. Please refresh and try again.");

    public static Error InvalidPageNumber => Error.Validation(
        code: "pageNumber",
        description: "Page number must be a positive integer.");

    public static Error InvalidPageSize => Error.Validation(
        code: "pageSize",
        description: "Page size must be between 1 and 100.");
}
