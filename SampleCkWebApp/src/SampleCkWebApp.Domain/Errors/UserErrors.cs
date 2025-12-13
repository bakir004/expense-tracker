// ============================================================================
// FILE: UserErrors.cs
// ============================================================================
// WHAT: Domain-specific error definitions for user-related operations.
//
// WHY: This file exists in the Domain layer to define all possible errors
//      that can occur when working with users. By centralizing error
//      definitions here, we ensure consistent error handling across all
//      layers. Domain errors are business logic errors, not technical errors.
//
// WHAT IT DOES:
//      - Defines static Error objects for common user-related errors:
//        NotFound, DuplicateEmail, InvalidEmail, InvalidName, InvalidPassword
//      - Provides standardized error codes and messages for user operations
//      - Used by Application layer services and validators to return
//        domain-appropriate errors
// ============================================================================

using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

public static class UserErrors
{
    public static Error NotFound =>
        Error.NotFound($"{nameof(UserErrors)}.{nameof(NotFound)}", "User not found.");
    
    public static Error DuplicateEmail =>
        Error.Conflict($"{nameof(UserErrors)}.{nameof(DuplicateEmail)}", "A user with this email already exists.");
    
    public static Error InvalidEmail =>
        Error.Validation($"{nameof(UserErrors)}.{nameof(InvalidEmail)}", "The provided email is invalid.");
    
    public static Error InvalidName =>
        Error.Validation($"{nameof(UserErrors)}.{nameof(InvalidName)}", "Name must be between 1 and 100 characters.");
    
    public static Error InvalidPassword =>
        Error.Validation($"{nameof(UserErrors)}.{nameof(InvalidPassword)}", "Password is required and must be at least 6 characters long.");
}

