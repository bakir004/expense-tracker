using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Errors;

/// <summary>
/// Domain-specific errors for transaction operations.
/// </summary>
public static class TransactionErrors
{
    public static Error NotFound =>
        Error.NotFound("Transaction", "Transaction not found.");

    public static Error InvalidTransactionId =>
        Error.Validation("TransactionId", "Transaction ID must be a positive integer.");

    public static Error InvalidAmount =>
        Error.Validation("Amount", "Transaction amount must be greater than zero.");

    public static Error ExpenseMissingCategory =>
        Error.Validation("CategoryId", "Expense transactions must have a category.");

    public static Error InvalidTransactionType =>
        Error.Validation("TransactionType", "Invalid transaction type. Must be 'EXPENSE' or 'INCOME'.");

    public static Error TransactionTypeRequired =>
        Error.Validation("TransactionType", "Transaction type is required.");

    public static Error InvalidPaymentMethod =>
        Error.Validation("PaymentMethod", "Invalid payment method.");

    public static Error PaymentMethodRequired =>
        Error.Validation("PaymentMethod", "Payment method is required.");

    public static Error InvalidSubject =>
        Error.Validation("Subject", "Transaction subject is required and cannot be empty.");

    public static Error InvalidUserId =>
        Error.Validation("UserId", "User ID must be a positive integer.");

    public static Error InvalidDate =>
        Error.Validation("Date", "Transaction date is outside the allowed range.");

    public static Error SubjectTooLong =>
        Error.Validation("Subject", "Transaction subject cannot exceed 255 characters.");

    public static Error InvalidCategoryId =>
        Error.Validation("CategoryId", "Category ID must be a positive integer when provided.");

    public static Error Unauthorized =>
        Error.Validation("Unauthorized", "You are not authorized to access this transaction.");

    public static Error ConcurrencyConflict =>
        Error.Conflict("Concurrency", "Transaction was modified by another process. Please refresh and try again.");

    public static Error InvalidPageNumber =>
        Error.Validation("PageNumber", "Page number must be a positive integer.");

    public static Error InvalidPageSize =>
        Error.Validation("PageSize", "Page size must be between 1 and 100.");

    public static Error ValidationError(string message) =>
        Error.Validation("Validation", message);

    public static Error UnexpectedError(string message) =>
        Error.Failure("Unexpected", message);
}
