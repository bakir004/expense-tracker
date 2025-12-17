// ============================================================================
// FILE: ExpenseGroupErrors.cs
// ============================================================================
// WHAT: Domain-specific error definitions for expense group-related operations.
//
// WHY: This file exists in the Domain layer to define all possible errors
//      that can occur when working with expense groups. By centralizing error
//      definitions here, we ensure consistent error handling across all
//      layers. Domain errors are business logic errors, not technical errors.
//
// WHAT IT DOES:
//      - Defines static Error objects for common expense group-related errors:
//        NotFound, InvalidName, InvalidUserId
//      - Provides standardized error codes and messages for expense group operations
//      - Used by Application layer services and validators to return
//        domain-appropriate errors
// ============================================================================

using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

public static class ExpenseGroupErrors
{
    public static Error NotFound =>
        Error.NotFound($"{nameof(ExpenseGroupErrors)}.{nameof(NotFound)}", "Expense group not found.");
    
    public static Error InvalidName =>
        Error.Validation($"{nameof(ExpenseGroupErrors)}.{nameof(InvalidName)}", "Expense group name is required and must be between 1 and 255 characters.");
    
    public static Error InvalidUserId =>
        Error.Validation($"{nameof(ExpenseGroupErrors)}.{nameof(InvalidUserId)}", "User ID is required and must be greater than 0.");
}

