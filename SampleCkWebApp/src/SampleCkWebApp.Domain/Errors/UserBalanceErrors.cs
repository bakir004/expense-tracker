// ============================================================================
// FILE: UserBalanceErrors.cs
// ============================================================================
// WHAT: Domain-specific error definitions for user balance-related operations.
//
// WHY: This file exists in the Domain layer to define all possible errors
//      that can occur when working with user balances. By centralizing error
//      definitions here, we ensure consistent error handling across all
//      layers. Domain errors are business logic errors, not technical errors.
//
// WHAT IT DOES:
//      - Defines static Error objects for common user balance-related errors:
//        NotFound, InvalidUserId, InvalidBalance
//      - Provides standardized error codes and messages for balance operations
//      - Used by Application layer services and validators to return
//        domain-appropriate errors
// ============================================================================

using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

public static class UserBalanceErrors
{
    public static Error NotFound =>
        Error.NotFound($"{nameof(UserBalanceErrors)}.{nameof(NotFound)}", "User balance not found.");
    
    public static Error InvalidUserId =>
        Error.Validation($"{nameof(UserBalanceErrors)}.{nameof(InvalidUserId)}", "User ID is required and must be greater than 0.");
    
    public static Error InvalidBalance =>
        Error.Validation($"{nameof(UserBalanceErrors)}.{nameof(InvalidBalance)}", "Balance cannot be negative.");
}

