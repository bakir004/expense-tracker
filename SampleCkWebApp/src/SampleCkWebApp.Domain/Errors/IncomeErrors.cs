// ============================================================================
// FILE: IncomeErrors.cs
// ============================================================================
// WHAT: Domain-specific error definitions for income-related operations.
//
// WHY: This file exists in the Domain layer to define all possible errors
//      that can occur when working with income. By centralizing error
//      definitions here, we ensure consistent error handling across all
//      layers. Domain errors are business logic errors, not technical errors.
//
// WHAT IT DOES:
//      - Defines static Error objects for common income-related errors:
//        NotFound, InvalidAmount, InvalidDate, InvalidUserId
//      - Provides standardized error codes and messages for income operations
//      - Used by Application layer services and validators to return
//        domain-appropriate errors
// ============================================================================

using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

public static class IncomeErrors
{
    public static Error NotFound =>
        Error.NotFound($"{nameof(IncomeErrors)}.{nameof(NotFound)}", "Income not found.");
    
    public static Error InvalidAmount =>
        Error.Validation($"{nameof(IncomeErrors)}.{nameof(InvalidAmount)}", "Income amount must be greater than 0.");
    
    public static Error InvalidDate =>
        Error.Validation($"{nameof(IncomeErrors)}.{nameof(InvalidDate)}", "Income date is required.");
    
    public static Error InvalidUserId =>
        Error.Validation($"{nameof(IncomeErrors)}.{nameof(InvalidUserId)}", "User ID is required and must be greater than 0.");
}

