// ============================================================================
// FILE: ExpenseErrors.cs
// ============================================================================
// WHAT: Domain-specific error definitions for expense-related operations.
//
// WHY: This file exists in the Domain layer to define all possible errors
//      that can occur when working with expenses. By centralizing error
//      definitions here, we ensure consistent error handling across all
//      layers. Domain errors are business logic errors, not technical errors.
//
// WHAT IT DOES:
//      - Defines static Error objects for common expense-related errors:
//        NotFound, InvalidAmount, InvalidDate, InvalidCategoryId, InvalidUserId
//      - Provides standardized error codes and messages for expense operations
//      - Used by Application layer services and validators to return
//        domain-appropriate errors
// ============================================================================

using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

public static class ExpenseErrors
{
    public static Error NotFound =>
        Error.NotFound($"{nameof(ExpenseErrors)}.{nameof(NotFound)}", "Expense not found.");
    
    public static Error InvalidAmount =>
        Error.Validation($"{nameof(ExpenseErrors)}.{nameof(InvalidAmount)}", "Expense amount must be greater than 0.");
    
    public static Error InvalidDate =>
        Error.Validation($"{nameof(ExpenseErrors)}.{nameof(InvalidDate)}", "Expense date is required.");
    
    public static Error InvalidCategoryId =>
        Error.Validation($"{nameof(ExpenseErrors)}.{nameof(InvalidCategoryId)}", "Category ID is required and must be greater than 0.");
    
    public static Error InvalidUserId =>
        Error.Validation($"{nameof(ExpenseErrors)}.{nameof(InvalidUserId)}", "User ID is required and must be greater than 0.");
}

