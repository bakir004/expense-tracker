using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Errors;

/// <summary>
/// Domain-specific errors for transaction group operations.
/// </summary>
public static class TransactionGroupErrors
{
    public static Error NotFound =>
        Error.NotFound("TransactionGroup", "Transaction group not found.");

    public static Error InvalidTransactionGroupId =>
        Error.Validation("TransactionGroupId", "Transaction group ID must be a positive integer.");

    public static Error InvalidName =>
        Error.Validation("Name", "Transaction group name is required and must be between 1 and 255 characters.");

    public static Error InvalidUserId =>
        Error.Validation("UserId", "User ID is required and must be greater than 0.");

    public static Error UserNotFound =>
        Error.Failure("UserId", "The specified user does not exist.");
}
