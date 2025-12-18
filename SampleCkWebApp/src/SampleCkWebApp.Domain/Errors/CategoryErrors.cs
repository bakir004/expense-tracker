// ============================================================================
// FILE: CategoryErrors.cs
// ============================================================================
// WHAT: Domain-specific error definitions for category-related operations.
//
// WHY: This file exists in the Domain layer to define all possible errors
//      that can occur when working with categories. By centralizing error
//      definitions here, we ensure consistent error handling across all
//      layers. Domain errors are business logic errors, not technical errors.
//
// WHAT IT DOES:
//      - Defines static Error objects for common category-related errors:
//        NotFound, DuplicateName, InvalidName
//      - Provides standardized error codes and messages for category operations
//      - Used by Application layer services and validators to return
//        domain-appropriate errors
// ============================================================================

using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

public static class CategoryErrors
{
    public static Error NotFound =>
        Error.NotFound("category", "Category not found.");
    
    public static Error DuplicateName =>
        Error.Conflict("name", "A category with this name already exists.");
    
    public static Error InvalidName =>
        Error.Validation("name", "Category name is required and must be between 1 and 255 characters.");
}

