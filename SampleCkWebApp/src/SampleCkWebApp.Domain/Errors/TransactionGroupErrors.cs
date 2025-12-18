using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

/// <summary>
/// Domain-specific errors for transaction group operations.
/// </summary>
public static class TransactionGroupErrors
{
    public static Error NotFound =>
        Error.NotFound($"{nameof(TransactionGroupErrors)}.{nameof(NotFound)}", "Transaction group not found.");
    
    public static Error InvalidName =>
        Error.Validation($"{nameof(TransactionGroupErrors)}.{nameof(InvalidName)}", "Transaction group name is required and must be between 1 and 255 characters.");
    
    public static Error InvalidUserId =>
        Error.Validation($"{nameof(TransactionGroupErrors)}.{nameof(InvalidUserId)}", "User ID is required and must be greater than 0.");
}

