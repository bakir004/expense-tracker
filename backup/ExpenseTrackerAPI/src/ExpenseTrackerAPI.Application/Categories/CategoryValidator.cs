// ============================================================================
// FILE: CategoryValidator.cs
// ============================================================================
// WHAT: Static validation class for category-related input validation.
//
// WHY: This validator exists in the Application layer to centralize validation
//      logic for category operations. Validation is an application concern because
//      it enforces business rules about what constitutes valid category data.
//      Separating validation into its own class keeps the service clean and
//      makes validation logic reusable and testable independently.
//
// WHAT IT DOES:
//      - Validates category creation and update requests (name, description, icon)
//      - Checks name length (1-255 characters)
//      - Validates icon length if provided (max 100 characters)
//      - Returns ErrorOr<Success> with appropriate domain errors if validation fails
//      - Used by CategoryService before processing category operations
// ============================================================================

using ErrorOr;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Categories;

/// <summary>
/// Validator for category-related operations.
/// Contains validation logic for category creation and updates.
/// </summary>
public static class CategoryValidator
{
    /// <summary>
    /// Validates category creation/update request parameters
    /// </summary>
    public static ErrorOr<Success> ValidateCategoryRequest(string name, string? description, string? icon)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
        {
            return CategoryErrors.InvalidName;
        }

        if (icon != null && icon.Length > 100)
        {
            return Error.Validation("Category.Icon", "Icon must be 100 characters or less.");
        }

        return Result.Success;
    }
}

